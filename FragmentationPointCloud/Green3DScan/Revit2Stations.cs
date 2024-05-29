using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using D2 = GeometryLib.Double.D2;
using D3 = GeometryLib.Double.D3;
using Serilog;
using Transform = Autodesk.Revit.DB.Transform;
using Sys = System.Globalization.CultureInfo;
using Path = System.IO.Path;
using Document = Autodesk.Revit.DB.Document;
using View = Autodesk.Revit.DB.View;
using Line = Autodesk.Revit.DB.Line;
using D = Revit.Data;


namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    
    public class Test : IExternalCommand
    {
        string path;
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
            Log.Information("start");
            Log.Information(set.BBox_Buffer.ToString());
            #endregion setup

            Transform trans = Helper.GetTransformation(doc, set, out var crs);

            string csvVisibleFaces = Path.Combine(path, "Revit2StationsVisibleFaces");
            string csvVisibleFacesRef = Path.Combine(path, "Revit2StationsVisibleFacesRef");

            #region select files
            // Revit
            var fodPfRevit = new FileOpenDialog("CSV file (*.csv)|*.csv");
            fodPfRevit.Title = "Select CSV file with BimFaces from Revit!";
            if (fodPfRevit.Show() == ItemSelectionDialogResult.Canceled)
            {
                return Result.Cancelled;
            }
            var csvPathPfRevit = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodPfRevit.GetSelectedModelPath());

            var fodRpRevit = new FileOpenDialog("CSV file (*.csv)|*.csv");
            fodRpRevit.Title = "Select CSV file with BimFacesPlanes fromRevit!";
            if (fodRpRevit.Show() == ItemSelectionDialogResult.Canceled)
            {
                return Result.Cancelled;
            }
            var csvPathRpRevit = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodRpRevit.GetSelectedModelPath());

            #endregion select files

            #region read files

            var facesRevit = D.PlanarFace.ReadCsv(csvPathPfRevit, out var lineErrors1, out string error1);
            var facesMap = new Dictionary<D.Id, D.PlanarFace>();
            foreach (var pf in facesRevit)
            {
                facesMap[pf.Id] = pf;
            }
            var referencePlanesRevit = D.ReferencePlane.ReadCsv(csvPathRpRevit, out var lineErrors2, out string error2);

            #endregion read files

            //Faces

            View activeView = doc.ActiveView;

            // Sammeln aller Räume in der aktiven Ansicht
            FilteredElementCollector collFaces= new FilteredElementCollector(doc, activeView.Id);
            ICollection<Element> rooms = collFaces.OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToElements();

            var refPlanes = new List<D.ReferencePlane>();
            var faces = new List<D.PlanarFace>();
            int totalFailedFaces = 0;
            var faceId = 0;

            foreach (Element roomElement in rooms)
            {
                Room room = roomElement as Room;
                if (room == null)
                    continue;

                var calculator = new SpatialElementGeometryCalculator(doc);
                var calcResult = calculator.CalculateSpatialElementGeometry(room);

                var geomSolid = calcResult.GetGeometry();
                string stateId = room.CreatedPhaseId.IntegerValue.ToString();
                

                foreach (Face geomFace in geomSolid.Faces)
                {
                    faceId += 1;
                    if (!(geomFace is Autodesk.Revit.DB.PlanarFace planarFace))
                    {
                        continue;
                    }

                    // The faces for the room cannot be coloured at the end of the analysis because the ID of the individual faces is not correct.
                    var objectId = "x";
                    // var faceId = "y";
                    string createStateId = "TODO";
                    string demolishedStateId = "TODO";

                    //string convertRepresentation = e.ConvertToStableRepresentation(doc);
                    //string[] tokenList = convertRepresentation.Split(new char[] { ':' });
                    //var faceIdnew = Convert.ToInt64(tokenList[1]);

                    // combine ID
                    var id = new D.Id(createStateId, demolishedStateId, objectId, faceId);
                    var faceNormalTranform = trans.OfVector(planarFace.FaceNormal);
                    var normal = D3.Direction.Create(faceNormalTranform.X, faceNormalTranform.Y, faceNormalTranform.Z, out var length);
                    var originTranform = trans.OfPoint(planarFace.Origin) * Constants.feet2Meter;
                    var plane = new D3.Plane(new D3.Vector(originTranform.X, originTranform.Y, originTranform.Z), normal);
                    var refPlane = new D.ReferencePlane(crs, plane, 2);
                    refPlanes.Add(refPlane);

                    var rings = CurveLoops(planarFace, trans);

                    try
                    {
                        NTSWrapper.GeometryLib.ToPolygon2d(plane, rings, out D2.Polygon polygon, out D3.BBox bbox, out double maxPlaneDist);
                        var planarFaceIO = new D.PlanarFace(id, refPlane, bbox, polygon);
                        if (!(maxPlaneDist <= 0.01)) //Parameter
                        {
                            Log.Information("maxPlaneDist: " + maxPlaneDist);
                        }
                        faces.Add(planarFaceIO);
                    }
                    catch (Exception)
                    {
                        totalFailedFaces += 1;
                        Log.Information("new Planarface failed");
                    }
                }
            }

            // write faces
            string csvPlanarFaces = Path.Combine(path, "roomFaces");
            string csvReferencePlanes = Path.Combine(path, "roomFacesRef");
            D.PlanarFace.WriteCsv(csvPlanarFaces, faces);
            D.ReferencePlane.WriteCsv(csvReferencePlanes, refPlanes);
            // write OBJ
            Dictionary<string, D.ReferencePlane> objPlanes = new Dictionary<string, D.ReferencePlane>();
            foreach (D.ReferencePlane refPlane in refPlanes)
            {
                objPlanes.Add(refPlane.Id, refPlane);
            }
            D.PlanarFace.WriteObj(Path.Combine(path, "roomFaces"), objPlanes, faces);

            var refPlanesMap = new Dictionary<string, D.ReferencePlane>();
            foreach (var plane in refPlanes)
            {
                refPlanesMap[plane.Id] = plane;
            }

            TaskDialog.Show("Message", faces.Count.ToString() + " Faces wurden von Räumen geschrieben");

            //Standpunkte

            // Sammeln aller Türen im Modell
            FilteredElementCollector collRooms = new FilteredElementCollector(doc, activeView.Id);
            ICollection<Element> doors = collRooms.OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().ToElements();

            // Liste für die Speicherung der Mittelpunkte
            List<D3.Vector> stations = new List<D3.Vector>();

            foreach (Element door in doors)
            {
                var loc = door.Location;

                if (loc is LocationCurve locationCurve)
                {
                    Curve curve = locationCurve.Curve;

                    if (curve is Line line)
                    {
                        var center2D = (line.GetEndPoint(0) + line.GetEndPoint(1)) / 2;

                        // must be transformed if coordinatereduktion true
                        var center3D = trans.OfPoint(center2D + new XYZ(0, 0, 1)) * Constants.feet2Meter;
                    }
                    else
                    {
                        Log.Information("Wall, but the LocationCurve is not a line.");
                    }
                }   
                else if (loc is LocationPoint locationPoint)
                {
                    var point = trans.OfPoint(locationPoint.Point) * Constants.feet2Meter;
                    stations.Add(new D3.Vector(point.X, point.Y, 1));
                }
            }

            TaskDialog.Show("Message", doors.Count.ToString() + " Türen werden verwendet");

            //List<D3.Vector> stations = new List<D3.Vector>
            //{
            //    //new D3.Vector(0, 0, 0),
            //    //new D3.Vector(20.70, 0.50, 0.4),
            //    new D3.Vector(5, -3, 1.5),
            //    new D3.Vector(2, -2, 1.5)
            //};

            var visibleFacesId = Raycasting.VisibleFaces(facesRevit, referencePlanesRevit, stations, set, out D3.Vector[][] pointClouds);
            #region write pointcloud in XYZ
            List<string> lines = new List<string>();
            var visibleFaceId = new HashSet<D.Id>();
            for (int i = 0; i < stations.Count; i++)
            {
                foreach (D.Id id in visibleFacesId[i])
                {
                    visibleFaceId.Add(id);
                }
                for (int j = 0; j < pointClouds[i].Length; j++)
                {
                    lines.Add(pointClouds[i][j].x.ToString(Sys.InvariantCulture) + " " + pointClouds[i][j].y.ToString(Sys.InvariantCulture) + " " + pointClouds[i][j].z.ToString(Sys.InvariantCulture));
                }
            }
            File.WriteAllLines(Path.Combine(path, "simulatedPointcloud"), lines);
            #endregion write pointcloud in XYZ

            var visibleFaces = new HashSet<D.PlanarFace>();
            var pFMap = new Dictionary<D.Id, D.PlanarFace>();
            foreach (var pf in facesRevit)
            {
                pFMap[pf.Id] = pf;
            }
            for (int i = 0; i < stations.Count; i++)
            {
                foreach (D.Id id in visibleFacesId[i])
                {
                    visibleFaces.Add(pFMap[id]);
                }
            }
            D.PlanarFace.WriteCsv(csvVisibleFaces, visibleFaces);
            D.ReferencePlane.WriteCsv(csvVisibleFacesRef, refPlanes);

            //List<S.Id> listVisibleFaceId = new List<S.Id>(visibleFaceId);

            var notVisibleFaces = new List<D.Id>();
            var facesIdList = new List<D.Id>();
            foreach (var item in facesMap)
            {
                facesIdList.Add(item.Key);
            }

            foreach (var item in facesIdList)
            {
                if (!visibleFaceId.Contains(item))
                {
                    notVisibleFaces.Add(item);
                }
            }

            for (int i = 0; i < stations.Count; i++)
            {

                // sphere
                List<Curve> profile = new List<Curve>();
                XYZ station = new XYZ(stations[i].x, stations[i].y, stations[i].z);
                var stationTransform = trans.OfVector(station);
                XYZ center = stationTransform * Constants.meter2Feet;
                double radius = 0.15 * Constants.meter2Feet;
                XYZ profile00 = center;
                XYZ profilePlus = center + new XYZ(0, radius, 0);
                XYZ profileMinus = center - new XYZ(0, radius, 0);

                profile.Add(Line.CreateBound(profilePlus, profileMinus));
                profile.Add(Arc.Create(profileMinus, profilePlus, center + new XYZ(radius, 0, 0)));

                CurveLoop curveLoop = CurveLoop.Create(profile);
                SolidOptions options = new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId);

                Frame frame = new Frame(center, XYZ.BasisX, -XYZ.BasisZ, XYZ.BasisY);
                if (Frame.CanDefineRevitGeometry(frame) == true)
                {
                    Solid sphere = GeometryCreationUtilities.CreateRevolvedGeometry(frame, new CurveLoop[] { curveLoop }, 0, 2 * Math.PI, options);
                    using Transaction t = new Transaction(doc, "Create sphere direct shape");
                    t.Start();
                    // create direct shape and assign the sphere shape
                    DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                    ds.ApplicationId = "Application id";
                    ds.ApplicationDataId = "Geometry object id";
                    ds.SetShape(new GeometryObject[] { sphere });
                    t.Commit();
                }
            }
            TaskDialog.Show("Message", "Fertig");
            return Result.Succeeded;
        }

        private static List<D3.LineString> CurveLoops(Face face, Transform trans)
        {
            var rings = new List<D3.LineString>();
            IList<CurveLoop> curveLoops = face.GetEdgesAsCurveLoops();
            // exteriors and interiors
            for (int i = 0; i < curveLoops.Count; i++)
            {
                var vertices = new List<D3.Vector>();
                CurveLoop curveLoop = curveLoops[i];
                foreach (Curve curve in curveLoop)
                {
                    XYZ pntStart = trans.OfPoint(curve.GetEndPoint(0)) * Constants.feet2Meter;
                    vertices.Add(new D3.Vector(pntStart.X, pntStart.Y, pntStart.Z));
                }
                vertices.Add(vertices[0]);
                // List umkehren
                //vertices.Reverse();
                var linestr = new D3.LineString(vertices);
                rings.Add(linestr);
            }
            return rings;
        }

    }
}