using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Except = Autodesk.Revit.Exceptions;
using Serilog;
using S = ScantraIO.Data;
using D2 = GeometryLib.Double.D2;
using D3 = GeometryLib.Double.D3;
using static GeometryLib.Double.Constants;
using Document = Autodesk.Revit.DB.Document;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class Revit2FaceObjects : IExternalCommand
    {
        private int solids;
        private int totalFailedFaces;

        #region Execute
        string path;
        string dateBimLastModified;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region setup
            // settings json
            SettingsJson set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            try
            {
                path = Path.GetDirectoryName(doc.PathName);
                FileInfo fileInfo = new FileInfo(path);
                var date = fileInfo.LastWriteTime;
            }
            catch (Exception)
            {
                TaskDialog.Show("Message", "The file has not been saved yet.");
                return Result.Failed;
            }

            // logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(path, "LogFile_"), rollingInterval: RollingInterval.Day)
                .CreateLogger();
            Log.Information("start Stations2NotVisibleFaces");
            Log.Information(set.BBox_Buffer.ToString());

            Transform trans = Helper.GetTransformation(doc, set, out var crs);
            #endregion setup

            string csvPlanarFaces = Path.Combine(path, "1_BimFaces.csv");
            string csvReferencePlanes = Path.Combine(path, "1_BimPlanes.csv");
            string objR = "RevitObjects";
            solids = 0;
            totalFailedFaces = 0;

            List<S.PlanarFace> faces = new List<S.PlanarFace>();
            HashSet<S.ReferencePlane> refPlanes = new HashSet<S.ReferencePlane>();
            //HashSet<string> categories = new HashSet<string>();
            List<S.Id> notAnalysedFaces = new List<S.Id>();

            try
            {
                // select only building components
                IList<Reference> pickedObjects = uidoc.Selection.PickObjects(ObjectType.Element, "TEST Select building components whose faces are to be output.");
                foreach (Reference reference in pickedObjects)
                {
                    if (!GetGeometryElement(doc, reference, out ElementId eleId, out GeometryElement geomElement, out string createStateId, out string demolishedStateId, out string objectId, out Category cat))
                    {
                        Log.Information("skipped building component");
                        continue;
                    }
                    // Are the selected building components Solids or GeometryInstances?
                    foreach (GeometryObject geomObj in geomElement)
                    {
                        // collect faces from Solids and GeometryInstances in a FaceArray
                        FaceArray faceArray = new FaceArray();
                        if (!(geomObj is Solid || geomObj is GeometryInstance))
                        {
                            Type type = geomObj.GetType();
                            Log.Information("GeometryObject is not solid, or GeometryInstance!");
                            continue;
                        }
                        else if (geomObj is Solid)
                        {
                            Solid solid = geomObj as Solid;
                            faceArray = solid.Faces;
                        }
                        else if (geomObj is GeometryInstance)
                        {
                            GeometryInstance geometryInstance = geomObj as GeometryInstance;

                            GeometryElement instanceGeometry = geometryInstance.SymbolGeometry;

                            foreach (GeometryObject instanceGeomObj in instanceGeometry)
                            {
                                Solid solid = instanceGeomObj as Solid;
                                if (solid != null && solid.Faces.Size > 0)
                                {
                                    foreach (Face item in solid.Faces)
                                    {
                                        faceArray.Append(item);
                                    }
                                }
                            }
                        }
                        // distinction between planar and triangulated faces 
                        foreach (Face face in faceArray)
                        {
                            if (face is PlanarFace planarFace)
                            {
                                Element elem = doc.GetElement(eleId);
                                Location loc = elem.Location;
                                LocationPoint locPoint = loc as LocationPoint;
                                Transform transLocationPoint;

                                if (face.Reference == null)
                                {
                                    // skipped faces
                                    totalFailedFaces += 1;
                                    continue;
                                }

                                string convertRepresentation = face.Reference.ConvertToStableRepresentation(doc);
                                string faceId = convertRepresentation;
                                S.Id id = new S.Id(createStateId, demolishedStateId, objectId, faceId, 0);

                                XYZ faceNormalTranform;
                                D3.Direction normal;
                                XYZ originTranform;
                                List<D3.LineString> rings;

                                if (locPoint != null)
                                {
                                    XYZ origin = locPoint.Point;
                                    double angle = locPoint.Rotation;
                                    double elevation = origin.Z;
                                    double easting = origin.X;
                                    double northing = origin.Y;

                                    Transform tRotation = Transform.CreateRotation(XYZ.BasisZ, angle);
                                    XYZ vectorTranslation = new XYZ(easting, northing, elevation);
                                    Transform tTranslation = Transform.CreateTranslation(vectorTranslation);
                                    transLocationPoint = tTranslation.Multiply(tRotation);

                                    //faceNormalTranform = trans.OfVector(transLocationPoint.OfVector(planarFace.FaceNormal));
                                    faceNormalTranform = trans.OfVector(planarFace.FaceNormal);
                                    normal = D3.Direction.Create(faceNormalTranform.X, faceNormalTranform.Y, faceNormalTranform.Z, out double length);
                                    //originTranform = trans.OfPoint(transLocationPoint.OfPoint(planarFace.Origin)) * C.Constants.feet2Meter;
                                    originTranform = trans.OfPoint(planarFace.Origin) * Constants.feet2Meter;
                                    rings = CurveLoopsLocationPoint(planarFace, trans, transLocationPoint);
                                }
                                else
                                {
                                    faceNormalTranform = trans.OfVector(planarFace.FaceNormal);
                                    normal = D3.Direction.Create(faceNormalTranform.X, faceNormalTranform.Y, faceNormalTranform.Z, out double length);
                                    originTranform = trans.OfPoint(planarFace.Origin) * Constants.feet2Meter;
                                    rings = CurveLoops(planarFace, trans);
                                }

                                D3.Plane plane = new D3.Plane(new D3.Vector(originTranform.X, originTranform.Y, originTranform.Z), normal);
                                S.ReferencePlane refPlane = new S.ReferencePlane(crs, plane, 2);
                                refPlanes.Add(refPlane);

                                try
                                {
                                    NTSWrapper.GeometryLib.ToPolygon2d(plane, rings, out D2.Polygon polygon, out D3.BBox bbox, out double maxPlaneDist);
                                    S.PlanarFace planarFaceIO = new S.PlanarFace(id, refPlane, bbox, polygon);
                                    if (!(maxPlaneDist <= set.MaxPlaneDist_Meter))
                                    {
                                        notAnalysedFaces.Add(id);
                                        Log.Information("maxPlaneDist: " + maxPlaneDist);
                                    }
                                    faces.Add(planarFaceIO);
                                }
                                catch (Exception)
                                {
                                    notAnalysedFaces.Add(id);
                                    totalFailedFaces += 1;
                                    Log.Information(id + " Coversion failed");
                                }
                            }
                            else
                            {
                                if (set.OnlyPlanarFaces)
                                {
                                    continue;
                                }
                                Element elem = doc.GetElement(eleId);
                                Location loc = elem.Location;
                                LocationPoint locPoint = loc as LocationPoint;
                                Transform transformation;
                                Mesh mesh = face.Triangulate();
                                for (int i = 0; i < mesh.NumTriangles; i++)
                                {
                                    D3.Vector a;
                                    D3.Vector b;
                                    D3.Vector c;
                                    D3.Vector d;
                                    D3.Vector e;
                                    D3.Vector n;
                                    if (locPoint != null)
                                    {
                                        XYZ origin = locPoint.Point;
                                        double angle = locPoint.Rotation;
                                        double elevation = origin.Z;
                                        double easting = origin.X;
                                        double northing = origin.Y;

                                        Transform tRotation = Transform.CreateRotation(XYZ.BasisZ, angle);
                                        XYZ vectorTranslation = new XYZ(easting, northing, elevation);
                                        Transform tTranslation = Transform.CreateTranslation(vectorTranslation);
                                        transformation = tTranslation.Multiply(tRotation);

                                        MeshTriangle triangle = mesh.get_Triangle(i);
                                        XYZ vertex1 = transformation.OfPoint(triangle.get_Vertex(0));
                                        XYZ vertex2 = transformation.OfPoint(triangle.get_Vertex(1));
                                        XYZ vertex3 = transformation.OfPoint(triangle.get_Vertex(2));

                                        a = new D3.Vector(vertex1.X, vertex1.Y, vertex1.Z);
                                        b = new D3.Vector(vertex2.X, vertex2.Y, vertex2.Z);
                                        c = new D3.Vector(vertex3.X, vertex3.Y, vertex3.Z);

                                        d = a - b;
                                        e = a - c;

                                        n = d.Cross(e);
                                    }
                                    else
                                    {
                                        MeshTriangle triangle = mesh.get_Triangle(i);
                                        XYZ vertex1 = triangle.get_Vertex(0);
                                        XYZ vertex2 = triangle.get_Vertex(1);
                                        XYZ vertex3 = triangle.get_Vertex(2);

                                        a = new D3.Vector(vertex1.X, vertex1.Y, vertex1.Z);
                                        b = new D3.Vector(vertex2.X, vertex2.Y, vertex2.Z);
                                        c = new D3.Vector(vertex3.X, vertex3.Y, vertex3.Z);

                                        d = a - b;
                                        e = a - c;

                                        n = d.Cross(e);
                                    }

                                    // faceId
                                    string convertRepresentation = face.Reference.ConvertToStableRepresentation(doc);
                                    //string[] tokenList = convertRepresentation.Split(new char[] { ':' });
                                    int partId = i + 1;
                                    string faceId = convertRepresentation;

                                    // combine ID
                                    S.Id id = new S.Id(createStateId, demolishedStateId, objectId, faceId, partId);

                                    XYZ faceNormalTranform = trans.OfVector(new XYZ(n.x, n.y, n.z));
                                    D3.Direction normal = D3.Direction.Create(faceNormalTranform.X, faceNormalTranform.Y, faceNormalTranform.Z, out double length);

                                    XYZ originTranform = trans.OfPoint(new XYZ(a.x, a.y, a.z)) * Constants.feet2Meter;

                                    D3.Plane plane = new D3.Plane(new D3.Vector(originTranform.X, originTranform.Y, originTranform.Z), normal);
                                    S.ReferencePlane refPlane = new S.ReferencePlane(crs, plane, 2); // 2 decimal places
                                    refPlanes.Add(refPlane);
                                    List<D3.LineString> rings = new List<D3.LineString>();

                                    List<D3.Vector> vertices = new List<D3.Vector>();

                                    XYZ aXYZ = trans.OfPoint(new XYZ(a.x, a.y, a.z)) * Constants.feet2Meter;
                                    XYZ bXYZ = trans.OfPoint(new XYZ(b.x, b.y, b.z)) * Constants.feet2Meter;
                                    XYZ cXYZ = trans.OfPoint(new XYZ(c.x, c.y, c.z)) * Constants.feet2Meter;

                                    vertices.Add(new D3.Vector(aXYZ.X, aXYZ.Y, aXYZ.Z));
                                    vertices.Add(new D3.Vector(bXYZ.X, bXYZ.Y, bXYZ.Z));
                                    vertices.Add(new D3.Vector(cXYZ.X, cXYZ.Y, cXYZ.Z));
                                    vertices.Add(vertices[0]);

                                    D3.LineString linestr = new D3.LineString(vertices);
                                    rings.Add(linestr);

                                    try
                                    {
                                        NTSWrapper.GeometryLib.ToPolygon2d(plane, rings, out D2.Polygon polygon, out D3.BBox bbox, out double maxPlaneDist);
                                        S.PlanarFace planarFaceIO = new S.PlanarFace(id, refPlane, bbox, polygon);
                                        if (!(maxPlaneDist <= set.MaxPlaneDist_Meter))
                                        {
                                            notAnalysedFaces.Add(id);
                                            Log.Information("maxPlaneDist failed");
                                        }
                                        faces.Add(planarFaceIO);
                                    }
                                    catch
                                    {
                                        notAnalysedFaces.Add(id);
                                        Log.Information(id + " Conversion failed");
                                    }
                                }
                            }
                        }
                    }
                }
                Log.Information("number of faces:" + faces.Count);
                S.PlanarFace.WriteCsv(csvPlanarFaces, faces);
                S.ReferencePlane.WriteCsv(csvReferencePlanes, refPlanes);
                // write OBJ
                Dictionary<string, S.ReferencePlane> objPlanes = new Dictionary<string, S.ReferencePlane>();
                foreach (S.ReferencePlane refPlane in refPlanes)
                {
                    objPlanes.Add(refPlane.Id, refPlane);
                }
                S.PlanarFace.WriteObj(Path.Combine(path, objR), objPlanes, faces);
                //Log.Information("used building component categories:");
                //foreach (var item in categories)
                //{
                //    Log.Information(item);
                //}
                Log.Information("skipped faces:" + totalFailedFaces);
                foreach (S.Id item in notAnalysedFaces)
                {
                    Log.Information(item.ToString());
                }
                TaskDialog.Show("Message", faces.Count + " faces write to csv file! " + solids + " building components were used. " +
                    totalFailedFaces + " faces skipped.");
                return Result.Succeeded;
            }
            #region catch
            catch (Except.OperationCanceledException)
            {
                TaskDialog.Show("Message", "Error 1: Command canceled.");
                return Result.Failed;
            }
            catch (Except.ForbiddenForDynamicUpdateException)
            {
                TaskDialog.Show("Message", "Error 2");
                return Result.Failed;
            }
            catch (Exception ex)
            {
                message += "Error message::" + ex.ToString();
                TaskDialog.Show("Message", message);
                return Result.Failed;
            }
            #endregion catch
        }
        #endregion execute
        /// <summary>
        /// transformation from internal Revit crs to user crs
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="crs"></param>
        /// <returns></returns>
        public static Transform GetTransformation(in Document doc, in SettingsJson set, out D3.CoordinateSystem crs)
        {
            ProjectLocation projloc = doc.ActiveProjectLocation;
            ProjectPosition position_data = projloc.GetProjectPosition(XYZ.Zero);
            double angle = position_data.Angle;
            double elevation = 0;
            double easting = 0;
            double northing = 0;
            // Differentiation whether a reduction is to be calculated or not
            if (set.CoordinatesReduction == false)
            {
                elevation = position_data.Elevation;
                easting = position_data.EastWest;
                northing = position_data.NorthSouth;
            }
            Transform tRotation = Transform.CreateRotation(XYZ.BasisZ, angle);
            XYZ vectorTranslation = new XYZ(easting, northing, elevation);
            Transform tTranslation = Transform.CreateTranslation(vectorTranslation);
            Transform transformation = tTranslation.Multiply(tRotation);

            crs = new D3.CoordinateSystem(new D3.Vector(easting, northing, elevation), new D3.Direction(angle, HALFPI), D3.Axes.X, new D3.Direction(angle + HALFPI, HALFPI));
            return transformation;
        }
        /// <summary>
        /// GeometryElement with their StateId and ObjectId is taken from the reference
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="reference"></param>
        /// <param name="geomElement"></param>
        /// <param name="createStateId"></param>
        /// <param name="demolishedStateId"></param>
        /// <param name="objectId"></param>
        /// <param name="cat"></param>
        /// <returns></returns>
        public static bool GetGeometryElement(Document doc, Reference reference, out ElementId eleId, out GeometryElement geomElement, out string createStateId, out string demolishedStateId,
            out string objectId, out Category cat)
        {
            Element ele = doc.GetElement(reference.ElementId);
            Options options = new Options
            {
                ComputeReferences = true
            };

            // category
            cat = ele.Category;

            geomElement = ele.get_Geometry(options);
            if (geomElement is null)
            {
                eleId = default;
                geomElement = default;
                createStateId = default;
                demolishedStateId = default;
                objectId = default;
                return false;
            }
            // stateId and objectId
            createStateId = ele.CreatedPhaseId.IntegerValue.ToString();
            demolishedStateId = ele.DemolishedPhaseId.IntegerValue.ToString();
            objectId = ele.UniqueId;
            eleId = reference.ElementId;
            return true;
        }
        /// <summary>
        /// face is converted to the ScantraIO-PlanarFace
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="geomFace"></param>
        /// <param name="createStateId"></param>
        /// <param name="demolishedStateId"></param>
        /// <param name="objectId"></param>
        /// <param name="trans"></param>
        /// <param name="crs"></param>
        /// <param name="set"></param>
        /// <param name="pF"></param>
        /// <param name="rP"></param>
        /// <returns></returns>
        public static bool GeomFace2PlanarFace(Document doc, Face geomFace, string createStateId, string demolishedStateId, string objectId,
            Transform trans, D3.CoordinateSystem crs, SettingsJson set, out S.PlanarFace pF, out S.ReferencePlane rP)
        {
            pF = default;
            rP = default;

            PlanarFace planarFace = geomFace as PlanarFace;
            if (planarFace == null || geomFace.Reference == null)
            {
                Log.Information("no PlanarFace");
                return false;
            }

            // faceId
            string convertRepresentation = geomFace.Reference.ConvertToStableRepresentation(doc);
            string[] tokenList = convertRepresentation.Split(new char[] { ':' });
            string faceId = tokenList[1];

            // combine ID
            S.Id id = new S.Id(createStateId, demolishedStateId, objectId, faceId);

            XYZ faceNormalTranform = trans.OfVector(planarFace.FaceNormal);
            D3.Direction normal = D3.Direction.Create(faceNormalTranform.X, faceNormalTranform.Y, faceNormalTranform.Z, out double length);

            XYZ originTranform = trans.OfPoint(planarFace.Origin) * Constants.feet2Meter;

            D3.Plane plane = new D3.Plane(new D3.Vector(originTranform.X, originTranform.Y, originTranform.Z), normal);
            S.ReferencePlane refPlane = new S.ReferencePlane(crs, plane, 2); // 2 decimal places

            List<D3.LineString> rings = CurveLoops(planarFace, trans);

            try
            {
                NTSWrapper.GeometryLib.ToPolygon2d(plane, rings, out D2.Polygon polygon, out D3.BBox bbox, out double maxPlaneDist);
                S.PlanarFace planarFaceIO = new S.PlanarFace(id, refPlane, bbox, polygon);
                if (!(maxPlaneDist <= set.MaxPlaneDist_Meter))
                {
                    return false;
                }
                pF = planarFaceIO;
                rP = refPlane;
                return true;
            }
            catch
            {
                Log.Information("PlanarFace failed");
                return false;
            }
        }

        /// <summary>
        /// calculates and transforms the rings of a face
        /// </summary>
        /// <param name="face"></param>
        /// <param name="trans"></param>
        /// <returns></returns>
        private static List<D3.LineString> CurveLoops(Face face, Transform trans)
        {
            List<D3.LineString> rings = new List<D3.LineString>();
            IList<CurveLoop> curveLoops = face.GetEdgesAsCurveLoops();
            // exteriors and interiors
            for (int i = 0; i < curveLoops.Count; i++)
            {
                List<D3.Vector> vertices = new List<D3.Vector>();
                CurveLoop curveLoop = curveLoops[i];
                foreach (Curve curve in curveLoop)
                {
                    XYZ pntStart = trans.OfPoint(curve.GetEndPoint(0)) * Constants.feet2Meter;
                    vertices.Add(new D3.Vector(pntStart.X, pntStart.Y, pntStart.Z));
                }
                vertices.Add(vertices[0]);
                D3.LineString linestr = new D3.LineString(vertices);
                rings.Add(linestr);
            }
            return rings;
        }
        private static List<D3.LineString> CurveLoopsLocationPoint(Face face, Transform trans, Transform transLoc)
        {
            List<D3.LineString> rings = new List<D3.LineString>();
            IList<CurveLoop> curveLoops = face.GetEdgesAsCurveLoops();
            // exteriors and interiors
            for (int i = 0; i < curveLoops.Count; i++)
            {
                List<D3.Vector> vertices = new List<D3.Vector>();
                CurveLoop curveLoop = curveLoops[i];
                foreach (Curve curve in curveLoop)
                {
                    XYZ pntStart = trans.OfPoint(curve.GetEndPoint(0)) * Constants.feet2Meter;

                    //XYZ pntStart = trans.OfPoint(transLoc.OfPoint(curve.GetEndPoint(0))) * C.Constants.feet2Meter;
                    vertices.Add(new D3.Vector(pntStart.X, pntStart.Y, pntStart.Z));
                }
                vertices.Add(vertices[0]);
                D3.LineString linestr = new D3.LineString(vertices);
                rings.Add(linestr);
            }
            return rings;
        }
    }
}