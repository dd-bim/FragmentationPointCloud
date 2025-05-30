using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using JetBrains.Annotations;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.Exceptions;
using RD = Revit.Data;

namespace Revit.Green3DScan.SimulatePointCloud;

[Transaction(TransactionMode.Manual)]
[UsedImplicitly]
public class Bim2FaceObjects : IExternalCommand
{
    private const string RevitObjects = "RevitObjects";
    private const string BIMFacesFileName = "1_BimFaces.csv";
    private const string BIMPlanesFileName = "1_BimPlanes.csv";

    private static readonly Options GeometryOptions = new()
    {
        ComputeReferences = true,
        IncludeNonVisibleObjects = true,
        DetailLevel = ViewDetailLevel.Fine,
        View = null
    };

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // Initialization
        if (!ExternalCommandHelper.GetProjectPath(commandData, out string projectPath, out var document, out var uiDocument))
        {
            TaskDialog.Show("Message", "The project file has not been saved yet.");
            return Result.Failed;
        }
        ExternalCommandHelper.InitLogger(projectPath);
        var settings = SettingsJson.ReadSettingsJson(Constants.pathSettings);

        Log.Information("start Revit2FaceObjects");
        Log.Information("BBox_Buffer: {BBox_Buffer}", settings.BBox_Buffer.ToStringInvariant());

        // Execution

        // Select building components
        IList<Reference> pickedObjects;
        try
        {
            pickedObjects = uiDocument.Selection.PickObjects(ObjectType.Element, "TEST Select building components whose faces are to be output.");
            if (pickedObjects.Count < 1)
                throw new Exception();
        }
        catch
        {
            TaskDialog.Show("Message", "No building elements selected.");
            return Result.Failed;
        }

        // Set transformation
        var transform = Helper.GetTransformation(document, settings);

        // Extract PlanarFaces and ReferencePlanes from the selected building components
        int totalFailedFaces = 0;
        List<RD.PlanarFace> faces = [];
        Dictionary<string, RD.ReferencePlane> refPlanes = [];
        List<RD.Id> notAnalysedFaces = [];
        int solids = 0;

        foreach (var reference in pickedObjects)
        {
            var element = document.GetElement(reference.ElementId);
            GeometryElement geometryElement;
            if (element is null 
                || !element.IsValidObject
                || (geometryElement = element.get_Geometry(GeometryOptions)) is null 
                || !geometryElement.IsElementGeometry)
            {
                Log.Information("skipped building component");
                continue;
            }
            // Are the selected building components Solids or GeometryInstances?
            foreach (var geometryObject in geometryElement)
            {
                // collect faces from Solids and GeometryInstances in a FaceArray
                if (geometryObject is not (Solid or GeometryInstance))
                {
                    var type = geometryObject.GetType();
                    Log.Information("GeometryObject is not solid, or GeometryInstance!");
                    continue;
                }

                switch (geometryObject)
                {
                    case Solid obj:
                        totalFailedFaces += ProcessFaceArrays(settings, document, reference, transform, crs,
                            ref faces, ref refPlanes, ref notAnalysedFaces,
                            obj.Faces);
                        solids++;
                        break;
                    case GeometryInstance obj:
                        var instanceGeometry = obj.SymbolGeometry;
                        foreach (var instanceGeomObj in instanceGeometry)
                        {
                            var solid = instanceGeomObj as Solid;
                            if (solid == null || solid.Faces.Size <= 0) continue;
                            FaceArray faceArray = new();
                            foreach (Face item in solid.Faces)
                            {
                                faceArray.Append(item);
                            }
                            totalFailedFaces += ProcessFaceArrays(settings, document, reference, transform, crs,
                                ref faces, ref refPlanes, ref notAnalysedFaces,
                                faceArray);
                        }
                        solids++; // TODO: check if this is correct
                        break;
                }
            }


        }

        try
        {
            Log.Information("number of faces: {count}", faces.Count);
            
            D.PlanarFace.WriteCsv(Path.Combine(projectPath, BIMFacesFileName), faces);
            D.ReferencePlane.WriteCsv(Path.Combine(projectPath, BIMPlanesFileName), refPlanes.Values);
            
            // write OBJ
            D.PlanarFace.WriteObj(Path.Combine(projectPath, RevitObjects), refPlanes, faces);

            Log.Information("skipped faces: {_totalFailedFaces}", totalFailedFaces);
            foreach (var item in notAnalysedFaces)
            {
                Log.Information("Id: {id}", item);
            }
            ;
            TaskDialog.Show("Message", FormattableString.Invariant(
                $"{faces.Count} faces write to csv file! {solids} building components were used. {totalFailedFaces} faces skipped."));
            return Result.Succeeded;
        }
        #region catch
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            TaskDialog.Show("Message", "Error 1: Command canceled.");
            return Result.Failed;
        }
        catch (ForbiddenForDynamicUpdateException)
        {
            TaskDialog.Show("Message", "Error 2");
            return Result.Failed;
        }
        catch (Exception ex)
        {
            message += "Error message: " + ex.ToString();
            TaskDialog.Show("Message", message);
            return Result.Failed;
        }
        #endregion catch
    }

    private static int ProcessFaceArrays(
        SettingsJson settings,
        Document document,
        Reference reference,
        Transform transform,
        ref List<RD.PlanarFace> faces,
        ref Dictionary<string, RD.ReferencePlane> refPlanes,
        ref List<RD.Id> notAnalysedFaces,
        FaceArray faceArray)
    {
        int totalFailedFaces = 0;
        var element = document.GetElement(reference.ElementId);
        string createId = element.CreatedPhaseId.Value.ToString();
        string demolishedId = element.DemolishedPhaseId.Value.ToString();
        var location = element.Location;

        // distinction between planar and triangulated faces 
        foreach (Face face in faceArray)
        {
            if (face.Reference == null)
            {
                // skipped faces
                totalFailedFaces += 1;
                continue;
            }
            var mesh = face.Triangulate();
            if (mesh == null || mesh.NumTriangles < 1)
            {
                // skipped faces
                totalFailedFaces += 1;
                continue;
            }
            string convertRepresentation = face.Reference.ConvertToStableRepresentation(document);
            var id = new RD.Id(createId, demolishedId, element.UniqueId, convertRepresentation, 0);

            if (face is PlanarFace planar)
            {
                var plane = Plane.CreateByOriginAndBasis(
                      transform.OfPoint(planar.Origin) * Constants.feet2Meter,
                      transform.OfVector(planar.XVector),
                      transform.OfVector(planar.YVector));
                mesh.

                var normal = transform.OfVector(mesh.GetNormal(0));
                var xaxis = transform.OfVector(planarFace.XVector);
                var position = transform.OfPoint(face.Origin) * Constants.feet2Meter;
                var rings = Face2LinearRings(planarFace, transform);

                totalFailedFaces += CreatePlanarFace(settings, position, normal, xaxis, id, rings, 
                    ref refPlanes, ref notAnalysedFaces, ref faces);
            }
            else if (!settings.OnlyPlanarFaces)
            {
                for (int i = 0; i < mesh.NumTriangles; i++)
                {
                    var triangle = mesh.get_Triangle(i);
                    var a = triangle.get_Vertex(0);
                    var b = triangle.get_Vertex(1);
                    var c = triangle.get_Vertex(2);

                    if (location is LocationPoint locPoint)
                    {
                        var origin = locPoint.Point;
                        double angle = locPoint.Rotation;
                        double easting = origin.X;
                        double northing = origin.Y;
                        double elevation = origin.Z;

                        var rotation = Transform.CreateRotation(XYZ.BasisZ, angle);
                        XYZ vectorTranslation = new(easting, northing, elevation);
                        var tTranslation = Transform.CreateTranslation(vectorTranslation);
                        var transformation = tTranslation.Multiply(rotation);

                        a = transformation.OfPoint(a);
                        b = transformation.OfPoint(b);
                        c = transformation.OfPoint(c);
                    }

                    a = transform.OfPoint(a) * Constants.feet2Meter;
                    b = transform.OfPoint(b) * Constants.feet2Meter;
                    c = transform.OfPoint(c) * Constants.feet2Meter;

                    var d = a - b;
                    var e = a - c;
                    var normal = d.CrossProduct(e);

                    var va = a.ToVector();
                    var vb = b.ToVector();
                    var vc = c.ToVector();
                    var rings = new XYZ[] { new([a, b, c, a]) };
                    int partId = i + 1;

                    // faceId
                    string convertRepresentation = face.Reference.ConvertToStableRepresentation(document);
                    D.Id id = new(createId, demolishedId, element.UniqueId, convertRepresentation, partId);

                    CreatePlanarFace(settings, crs, va, normal, id, rings, 
                        ref refPlanes, ref notAnalysedFaces, ref faces);
                }
            }
        }

        return totalFailedFaces;
    }

    private static int CreatePlanarFace(SettingsJson settings, XYZ position, XYZ normal, XYZ xAxis, RD.Id id,
        XYZ[][] rings, ref Dictionary<string, RD.ReferencePlane> refPlanes, ref List<RD.Id> notAnalysedFaces, ref List<RD.PlanarFace> faces)
    {
        var plane = new D3.Plane(position, normal.ToDirection());
        var refPlane = new D.ReferencePlane(crs, plane, 2); // 2 decimal places
        refPlanes.Add(refPlane.Id, refPlane);

        if (!D.PlanarFace.Create(id, refPlane, rings, out var planarFaceIO, out double maxPlaneDist)
            || !(maxPlaneDist <= settings.MaxPlaneDist_Meter))
        {
            Log.Information("maxPlaneDist: {maxPlaneDist}", maxPlaneDist);
            faces.Add(planarFaceIO);
            return 0;
        }
        else
        {
            notAnalysedFaces.Add(id);
            Log.Information("Conversion of id {id} failed", id);
            return 1;
        }
    }


}
