using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using GeometryLib.D3;
using JetBrains.Annotations;
using Serilog;
using CoordinateSystem = GeometryLib.D3.CoordinateSystem;
using LineString = GeometryLib.D3.LineString;
using S = Revit.Data;
using Transform = Autodesk.Revit.DB.Transform;
using Sys = System.Globalization.CultureInfo;
using Path = System.IO.Path;
using Plane = GeometryLib.D3.Plane;
using Vector = GeometryLib.D3.Vector;

namespace Revit.Green3DScan.SimulatePointCloud
{
    [Transaction(TransactionMode.Manual)]
    [UsedImplicitly]
    public class Revit2Stations : IExternalCommand
    {
        public const string CsvHeader = "ObjectGuid;ElementId;East;North;Elevation";
        private string path;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region setup

            // settings json
            var set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            UIApplication uiapp = commandData.Application;
            try
            {
                path = Path.GetDirectoryName(doc.PathName);
                var fileInfo = new FileInfo(path);
                DateTime date = fileInfo.LastWriteTime;
            }
            catch (Exception)
            {
                TaskDialog.Show("Message", "The file has not been saved yet.");
                return Result.Failed;
            }

            // logger
            string logsPath = Path.Combine(path, "00_Logs/");
            if (!Directory.Exists(logsPath)) Directory.CreateDirectory(logsPath);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(logsPath, "LogFile_"), rollingInterval: RollingInterval.Minute)
                .CreateLogger();
            Log.Information("start Revit2Station");

            Transform trans = Helper.GetTransformation(doc, set, out CoordinateSystem crs);

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
            var collRooms = new FilteredElementCollector(doc, activeView.Id);
            ICollection<Element> rooms = collRooms.OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType()
                .ToElements();

            var refPlanes = new List<S.ReferencePlane>();
            var faces = new List<S.PlanarFace>();
            var totalFailedFaces = 0;
            var faceId = 0;

            foreach (Element roomElement in rooms)
            {
                var room = roomElement as Room;
                if (room == null)
                    continue;

                var calculator = new SpatialElementGeometryCalculator(doc);
                SpatialElementGeometryResults calcResult = calculator.CalculateSpatialElementGeometry(room);

                Solid geomSolid = calcResult.GetGeometry();
                var stateId = room.CreatedPhaseId.ToString();


                foreach (Face geomFace in geomSolid.Faces)
                {
                    faceId += 1;
                    if (!(geomFace is PlanarFace planarFace)) continue;

                    // The faces for the room cannot be coloured at the end of the analysis because the ID of the individual faces is not correct.
                    var objectId = "x";
                    // var faceId = "y";
                    var createStateId = "TODO";

                    //string convertRepresentation = e.ConvertToStableRepresentation(doc);
                    //string[] tokenList = convertRepresentation.Split(new char[] { ':' });
                    //var faceIdnew = Convert.ToInt64(tokenList[1]);

                    // combine ID
                    var id = new S.Id(createStateId, objectId, faceId.ToString());
                    XYZ faceNormalTranform = trans.OfVector(planarFace.FaceNormal);
                    var normal = Direction.Create(faceNormalTranform.X, faceNormalTranform.Y, faceNormalTranform.Z,
                        out double length);
                    XYZ originTranform = trans.OfPoint(planarFace.Origin) * Constants.feet2Meter;
                    var plane = new Plane(new Vector(originTranform.X, originTranform.Y, originTranform.Z),
                        normal);
                    var refPlane = new S.ReferencePlane(crs, plane, 2);
                    refPlanes.Add(refPlane);

                    var rings = CurveLoops(planarFace, trans);

                    try
                    {
                        if (!S.PlanarFace.Create(id, refPlane, rings, out S.PlanarFace planarFaceIO,
                                out double maxPlaneDist)
                            || !(maxPlaneDist <= 0.01))
                            Log.Information("maxPlaneDist: " + maxPlaneDist);
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
            var objPlanes = new Dictionary<string, S.ReferencePlane>();
            foreach (S.ReferencePlane refPlane in refPlanes) objPlanes.Add(refPlane.Id, refPlane);
            S.PlanarFace.WriteObj(Path.Combine(path, "roomFaces"), objPlanes, faces);

            var refPlanesMap = new Dictionary<string, S.ReferencePlane>();
            foreach (S.ReferencePlane plane in refPlanes) refPlanesMap[plane.Id] = plane;

            Log.Information(faces.Count + " room faces");

            #endregion room faces

            Log.Information("room faces");

            #region stations

            var stations = new List<Vector>();
            var stationsPBP = new List<Vector>();

            var listDoors = new List<ElementId>();

            // collect doors
            var collDoors = new FilteredElementCollector(doc, activeView.Id);
            ICollection<Element> doors = collDoors.OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType()
                .ToElements();

            double heigth = set.HeightOfScanner_Meter * Constants.meter2Feet;

            foreach (Element door in doors)
            {
                Location loc = door.Location;

                if (loc is LocationCurve locationCurve)
                {
                    Log.Information("Door, but no LocationPoint.");
                }
                else if (loc is LocationPoint locationPoint)
                {
                    stations.Add(new Vector(locationPoint.Point.X, locationPoint.Point.Y, heigth));
                }
                else
                {
                    listDoors.Add(door.Id);
                    Log.Information(door.Id.ToString());
                    Log.Information("loc null");
                }
            }

            Log.Information(stations.Count + " stations(doors)");
            Log.Information(doors.Count + " doors");

            // collect rooms
            foreach (Element room in rooms)
            {
                if (room is SpatialElement spatialRoom && spatialRoom.Location is LocationPoint locationPoint)
                    //var roomPoint = trans.OfPoint(locationPoint.Point) * Constants.feet2Meter;
                    //stations.Add(new D3.Vector(roomPoint.X, roomPoint.Y, transformedHeight.Z));
                    stations.Add(new Vector(locationPoint.Point.X, locationPoint.Point.Y, heigth));
                else
                    Log.Information("room is no SpatialElement or LocationPoint ");
            }

            foreach (Vector item in stations)
            {
                var x = new XYZ(item.x, item.y, item.z);
                XYZ xTrans = trans.OfPoint(x) * Constants.feet2Meter;
                stationsPBP.Add(new Vector(xTrans.X, xTrans.Y, xTrans.Z));
            }

            Log.Information(rooms.Count + " rooms");

            #endregion stations

            Log.Information(stations.Count + " stations");

            #region ScanStation

            if (!File.Exists(Path.Combine(path, "ScanStation.rfa")))
                Helper.CreateSphereFamily(uiapp, set.SphereDiameter_Meter / 2 * Constants.meter2Feet,
                    Path.Combine(path, "ScanStation.rfa"));

            Helper.LoadAndPlaceSphereFamily(doc, Path.Combine(path, "ScanStation.rfa"), stations);

            Family family;
            if (!doc.LoadFamily(Path.Combine(path, "ScanStation.rfa"), out family))
            {
                var collector = new FilteredElementCollector(doc);
                ICollection<Element> familyInstances = collector.OfClass(typeof(Family)).ToElements();
                foreach (Element element in familyInstances)
                {
                    var loadedFamily = element as Family;
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

            if (!Directory.Exists(csvPath)) Directory.CreateDirectory(csvPath);
            using StreamWriter csv = File.CreateText(Path.Combine(csvPath, "Stations.csv"));
            csv.WriteLine(CsvHeader);

            foreach (Vector item in stationsPBP)
            {
                csv.WriteLine(item.x.ToString(Sys.InvariantCulture) + ";" + item.y.ToString(Sys.InvariantCulture) +
                              ";" + item.z.ToString(Sys.InvariantCulture));
            }

            #endregion station to csv

            Log.Information("station to csv");
            var allStations = Helper.CollectFamilyInstances(doc, trans, "ScanStation");
            TaskDialog.Show("Message",
                stationsPBP.Count + " new ScanStations, total " + allStations.Count + " ScanStations");
            Log.Information("end Revit2Station");
            return Result.Succeeded;
        }

        private static List<LineString> CurveLoops(PlanarFace face, Transform trans)
        {
            var rings = new List<LineString>();
            var curveLoops = face.GetEdgesAsCurveLoops();
            // exteriors and interiors
            for (var i = 0; i < curveLoops.Count; i++)
            {
                var vertices = new List<Vector>();
                CurveLoop curveLoop = curveLoops[i];
                foreach (Curve curve in curveLoop)
                {
                    XYZ pntStart = trans.OfPoint(curve.GetEndPoint(0)) * Constants.feet2Meter;
                    vertices.Add(new Vector(pntStart.X, pntStart.Y, pntStart.Z));
                }

                vertices.Add(vertices[0]);
                var linestr = new LineString(vertices);
                rings.Add(linestr);
            }

            return rings;
        }
    }
}