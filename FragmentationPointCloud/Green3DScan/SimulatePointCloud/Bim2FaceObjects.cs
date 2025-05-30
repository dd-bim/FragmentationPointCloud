using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using JetBrains.Annotations;

using Serilog;

using System;
using System.Collections.Generic;
using System.IO;

using RD = Revit.Data;

namespace Revit.Green3DScan.SimulatePointCloud;

//checked
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
                        totalFailedFaces += ProcessFaceArrays(settings, document, reference, transform,
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
                            totalFailedFaces += ProcessFaceArrays(settings, document, reference, transform,
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

            RD.PlanarFace.WriteCsv(Path.Combine(projectPath, BIMFacesFileName), faces);
            RD.ReferencePlane.WriteCsv(Path.Combine(projectPath, BIMPlanesFileName), refPlanes.Values);

            // write OBJ
            RD.PlanarFace.WriteObj(Path.Combine(projectPath, RevitObjects), refPlanes, faces);

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

            if (mesh.NumberOfNormals == 1)
            {
                Plane plane;
                if (face is PlanarFace planar)
                {
                    plane = Plane.CreateByOriginAndBasis(
                      transform.OfPoint(planar.Origin) * Constants.feet2Meter,
                      transform.OfVector(planar.XVector),
                      transform.OfVector(planar.YVector));
                }
                else
                {
                    var faceTrans = face.ComputeDerivatives(UV.Zero);
                    plane = Plane.CreateByOriginAndBasis(
                      transform.OfPoint(faceTrans.Origin) * Constants.feet2Meter,
                      transform.OfVector(faceTrans.BasisX),
                      transform.OfVector(faceTrans.BasisY));
                }

                var refPlane = RD.ReferencePlane.Create(plane, Constants.PlaneDigits);
                if (!RD.PlanarFace.Create(id, refPlane, mesh, transform, out var planarFace, out double maxPlaneDist)
                    || !(maxPlaneDist <= settings.MaxPlaneDist_Meter))
                {
                    totalFailedFaces += 1;
                    notAnalysedFaces.Add(id);
                    Log.Information("Conversion of id {id} failed", id);
                    continue;
                }
                Log.Information("maxPlaneDist: {maxPlaneDist}", maxPlaneDist);
                faces.Add(planarFace!);
                refPlanes.Add(refPlane.Id, refPlane);
            }
            else if (!settings.OnlyPlanarFaces)
            {
                for (int i = 0; i < mesh.NumTriangles; i++)
                {
                    var triangle = mesh.get_Triangle(i);
                    var tid = id with
                    {
                        PartId = i + 1
                    };

                    if (!RD.PlanarFace.Create(tid, triangle, transform, PlaneDigits, out var planarFace, out var refPlane))
                    {
                        totalFailedFaces += 1;
                        notAnalysedFaces.Add(id);
                        Log.Information("Conversion of id {id} failed", id);
                        continue;
                    }
                    faces.Add(planarFace!);
                    refPlanes.Add(refPlane!.Id, refPlane!);
                }
            }
        }

        return totalFailedFaces;
    }



}
