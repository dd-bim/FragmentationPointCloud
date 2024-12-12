using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Serilog;
using D2 = GeometryLib.Double.D2;
using D3 = GeometryLib.Double.D3;
using S = ScantraIO.Data;
using Transform = Autodesk.Revit.DB.Transform;
using Sys = System.Globalization.CultureInfo;
using Path = System.IO.Path;
using Document = Autodesk.Revit.DB.Document;
using View = Autodesk.Revit.DB.View;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class Revit2Stations : IExternalCommand
    {
        string path;
        public const string CsvHeader = "ObjectGuid;ElementId;East;North;Elevation";
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region setup
            // settings json
            SettingsJson set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            UIApplication uiapp = commandData.Application;
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
            string logsPath = Path.Combine(path, "00_Logs/");
            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File(Path.Combine(logsPath, "LogFile_"), rollingInterval: RollingInterval.Minute)
               .CreateLogger();
            Log.Information("start Revit2Station");

            Transform trans = Helper.GetTransformation(doc, set, out var crs);

            string csvVisibleFaces = Path.Combine(path, "Revit2StationsVisibleFaces.csv");
            string csvVisibleFacesRef = Path.Combine(path, "Revit2StationsVisibleFacesRef.csv");

            #endregion setup
            Log.Information("setup");

            if (uidoc.ActiveView is View3D current3DView)
            {
                TaskDialog.Show("Message", "You must be in a 2D viewplan!");
                return Result.Failed;
            }

            #region room faces

            View activeView = doc.ActiveView;

            // collect all rooms in active view
            FilteredElementCollector collRooms= new FilteredElementCollector(doc, activeView.Id);
            ICollection<Element> rooms = collRooms.OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToElements();

            var refPlanes = new List<S.ReferencePlane>();
            var faces = new List<S.PlanarFace>();
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
                string stateId = room.CreatedPhaseId.ToString();
                

                foreach (Face geomFace in geomSolid.Faces)
                {
                    faceId += 1;
                    if (!(geomFace is PlanarFace planarFace))
                    {
                        continue;
                    }

                    // The faces for the room cannot be coloured at the end of the analysis because the ID of the individual faces is not correct.
                    var objectId = "x";
                    // var faceId = "y";
                    string createStateId = "TODO";

                    //string convertRepresentation = e.ConvertToStableRepresentation(doc);
                    //string[] tokenList = convertRepresentation.Split(new char[] { ':' });
                    //var faceIdnew = Convert.ToInt64(tokenList[1]);

                    // combine ID
                    var id = new S.Id(createStateId, objectId, faceId.ToString());
                    var faceNormalTranform = trans.OfVector(planarFace.FaceNormal);
                    var normal = D3.Direction.Create(faceNormalTranform.X, faceNormalTranform.Y, faceNormalTranform.Z, out var length);
                    var originTranform = trans.OfPoint(planarFace.Origin) * Constants.feet2Meter;
                    var plane = new D3.Plane(new D3.Vector(originTranform.X, originTranform.Y, originTranform.Z), normal);
                    var refPlane = new S.ReferencePlane(crs, plane, 2);
                    refPlanes.Add(refPlane);

                    var rings = CurveLoops(planarFace, trans);

                    try
                    {
                        NTSWrapper.GeometryLib.ToPolygon2d(plane, rings, out D2.Polygon polygon, out D3.BBox bbox, out double maxPlaneDist);
                        var planarFaceIO = new S.PlanarFace(id, refPlane, bbox, polygon);
                        if (!(maxPlaneDist <= 0.01))
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
            string csvPlanarFaces = Path.Combine(path, "roomFaces.csv");
            string csvReferencePlanes = Path.Combine(path, "roomFacesRef.csv");
            S.PlanarFace.WriteCsv(csvPlanarFaces, faces);
            S.ReferencePlane.WriteCsv(csvReferencePlanes, refPlanes);
            
            // write OBJ
            Dictionary<string, S.ReferencePlane> objPlanes = new Dictionary<string, S.ReferencePlane>();
            foreach (S.ReferencePlane refPlane in refPlanes)
            {
                objPlanes.Add(refPlane.Id, refPlane);
            }
            S.PlanarFace.WriteObj(Path.Combine(path, "roomFaces"), objPlanes, faces);

            var refPlanesMap = new Dictionary<string, S.ReferencePlane>();
            foreach (var plane in refPlanes)
            {
                refPlanesMap[plane.Id] = plane;
            }

            Log.Information(faces.Count.ToString() + " room faces");

            #endregion room faces
            Log.Information("room faces");
            #region stations

            List<D3.Vector> stations = new List<D3.Vector>(); 
            List<D3.Vector> stationsPBP = new List<D3.Vector>();

            List<ElementId> listDoors = new List<ElementId>();

            // collect doors
            FilteredElementCollector collDoors = new FilteredElementCollector(doc, activeView.Id);
            ICollection<Element> doors = collDoors.OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().ToElements();
            
            double heigth = set.HeightOfScanner_Meter * Constants.meter2Feet;

            foreach (Element door in doors)
            {
                var loc = door.Location;

                if (loc is LocationCurve locationCurve)
                {
                    Log.Information("Door, but no LocationPoint.");
                }   
                else if (loc is LocationPoint locationPoint)
                {
                    stations.Add(new D3.Vector(locationPoint.Point.X, locationPoint.Point.Y, heigth));
                }
                else
                {
                    listDoors.Add(door.Id);
                    Log.Information(door.Id.ToString());
                    Log.Information("loc null");
                }
            }
            Log.Information(stations.Count.ToString() + " stations(doors)");
            Log.Information(doors.Count.ToString() + " doors");

            // collect rooms
            foreach (Element room in rooms)
            {
                if (room is SpatialElement spatialRoom && spatialRoom.Location is LocationPoint locationPoint)
                {
                    //var roomPoint = trans.OfPoint(locationPoint.Point) * Constants.feet2Meter;
                    //stations.Add(new D3.Vector(roomPoint.X, roomPoint.Y, transformedHeight.Z));

                    stations.Add(new D3.Vector(locationPoint.Point.X, locationPoint.Point.Y, heigth));
                }
                else
                {
                    Log.Information("room is no SpatialElement or LocationPoint ");
                }
            }

            foreach (var item in stations)
            {
                var x = new XYZ(item.x, item.y, item.z);
                var xTrans = trans.OfPoint(x) * Constants.feet2Meter;
                stationsPBP.Add(new D3.Vector(xTrans.X, xTrans.Y, xTrans.Z));
            }
            Log.Information(rooms.Count.ToString() + " rooms");

            #endregion stations
            Log.Information(stations.Count.ToString() + " stations");
            #region ScanStation

            if (!File.Exists(Path.Combine(path, "ScanStation.rfa")))
            {
                Helper.CreateSphereFamily(uiapp, set.SphereDiameter_Meter / 2 * Constants.meter2Feet, Path.Combine(path, "ScanStation.rfa"));
            }

            Helper.LoadAndPlaceSphereFamily(doc, Path.Combine(path, "ScanStation.rfa"), stations);

            Family family;
            if (!doc.LoadFamily(Path.Combine(path, "ScanStation.rfa"), out family))
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                ICollection<Element> familyInstances = collector.OfClass(typeof(Family)).ToElements();
                foreach (Element element in familyInstances)
                {
                    Family loadedFamily = element as Family;
                    if (loadedFamily.Name == "ScanStation")
                    {
                        family = loadedFamily;
                        break;
                    }
                }
            }

            #endregion ScanStation
            Log.Information("ScanStation");
            #region station to csv

            string csvPath = Path.Combine(path, "07_Stations/");

            if (!Directory.Exists(csvPath))
            {
                Directory.CreateDirectory(csvPath);
            }
            using StreamWriter csv = File.CreateText(Path.Combine(csvPath, "Stations.csv"));
            csv.WriteLine(CsvHeader);

            foreach (var item in stationsPBP)
            {
                csv.WriteLine(item.x.ToString(Sys.InvariantCulture) + ";" + item.y.ToString(Sys.InvariantCulture) + ";" + item.z.ToString(Sys.InvariantCulture));
            }

            #endregion station to csv
            Log.Information("station to csv");
            var allStations = Helper.CollectFamilyInstances(doc, trans, "ScanStation");
            TaskDialog.Show("Message", stationsPBP.Count.ToString() + " new ScanStations, total " + allStations.Count.ToString() + " ScanStations");
            Log.Information("end Revit2Station");
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
                var linestr = new D3.LineString(vertices);
                rings.Add(linestr);
            }
            return rings;
        }
    }
}