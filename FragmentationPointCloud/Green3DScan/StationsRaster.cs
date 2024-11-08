﻿using System;
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
using S = ScantraIO.Data;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Structure;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class StationsRaster : IExternalCommand
    {
        string path;
        public const string CsvHeader = "ObjectGuid;ElementId;Rechtswert;Hochwert;Hoehe";
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
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File(Path.Combine(path, "LogFile_"), rollingInterval: RollingInterval.Day)
               .CreateLogger();
            Log.Information("start Revit2Station");

            Transform trans = Helper.GetTransformation(doc, set, out var crs);

            string csvVisibleFaces = Path.Combine(path, "Revit2StationsVisibleFaces.csv");
            string csvVisibleFacesRef = Path.Combine(path, "Revit2StationsVisibleFacesRef.csv");

            #endregion setup
            Log.Information("setup");
            #region select files

            if (uidoc.ActiveView is View3D current3DView)
            {
                TaskDialog.Show("Message", "You must be in a 2D viewplan!");
                return Result.Failed;
            }

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
            Log.Information("select files");
            #region read files

            var facesRevit = S.PlanarFace.ReadCsv(csvPathPfRevit, out var lineErrors1, out string error1);
            var facesMap = new Dictionary<S.Id, S.PlanarFace>();
            foreach (var pf in facesRevit)
            {
                facesMap[pf.Id] = pf;
            }

            var referencePlanesRevit = S.ReferencePlane.ReadCsv(csvPathRpRevit, out var lineErrors2, out string error2);

            #endregion read files
            Log.Information("read files");
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
                string stateId = room.CreatedPhaseId.IntegerValue.ToString();
                

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
            #region visible and not visible faces

            // visible faces per station
            var visibleFacesIdArray = Raycasting.VisibleFaces(facesRevit, referencePlanesRevit, stationsPBP, set, out D3.Vector[][] pointClouds, out Dictionary<S.Id, int> test, out HashSet<S.Id> hashPMin);

            //Test 
            var pMin = set.StepsPerFullTurn * set.StepsPerFullTurn * set.Beta_Degree / 1000;
            Log.Information(pMin.ToString() + " pMin");
            var y = 0;
            foreach (var item in test)
            {
                //Log.Information(item.Key.ToString());
                Log.Information(item.Value.ToString());
                y += item.Value;
            }
            Log.Information(y.ToString());
            
            var visibleFaceId = new HashSet<S.Id>();
            var visibleFaces = new HashSet<S.PlanarFace>();
            var pFMap = new Dictionary<S.Id, S.PlanarFace>();
            foreach (var pf in facesRevit)
            {
                pFMap[pf.Id] = pf;
            }
            for (int i = 0; i < stationsPBP.Count; i++)
            {
                foreach (S.Id id in visibleFacesIdArray[i])
                {
                    visibleFaces.Add(pFMap[id]);
                    visibleFaceId.Add(id);
                }
            }

            // visible faces
            S.PlanarFace.WriteCsv(csvVisibleFaces, visibleFaces);
            S.ReferencePlane.WriteCsv(csvVisibleFacesRef, refPlanes);
            S.PlanarFace.WriteObj(Path.Combine(path, "visible"), referencePlanesRevit, visibleFaces);

            var notVisibleFacesId = new List<S.Id>();
            var visibleFacesId = new List<S.Id>();
            var notVisibleFaces = new List<S.PlanarFace>();

            var facesIdList = new List<S.Id>();
            foreach (var item in facesMap)
            {
                facesIdList.Add(item.Key);
            }

            // not visible faces
            foreach (var item in facesIdList)
            {
                if (!visibleFaceId.Contains(item))
                {
                    notVisibleFacesId.Add(item);
                    notVisibleFaces.Add(facesMap[item]);
                }
                else
                {
                    visibleFacesId.Add(item);
                }
            }
            S.PlanarFace.WriteCsv(csvVisibleFaces, notVisibleFaces);
            S.ReferencePlane.WriteCsv(csvVisibleFacesRef, refPlanes);
            S.PlanarFace.WriteObj(Path.Combine(path, "notVisible"), referencePlanesRevit, notVisibleFaces);

            #endregion visible and not visible faces
            Log.Information("visible and not visible faces");
            #region write pointcloud in XYZ

            //List<string> lines = new List<string>();
            //for (int i = 0; i < stationsPBP.Count; i++)
            //{
            //    foreach (S.Id id in visibleFacesIdArray[i])
            //    {
            //        visibleFaceId.Add(id);
            //    }
            //    for (int j = 0; j < pointClouds[i].Length; j++)
            //    {
            //        lines.Add(pointClouds[i][j].x.ToString(Sys.InvariantCulture) + " " + pointClouds[i][j].y.ToString(Sys.InvariantCulture) + " " + pointClouds[i][j].z.ToString(Sys.InvariantCulture));
            //    }
            //}
            for (int i = 0; i < stationsPBP.Count; i++)
            {
                List<string> lines = new List<string>();

                foreach (S.Id id in visibleFacesIdArray[i])
                {
                    visibleFaceId.Add(id);
                }

                // Sammle die Punkte der aktuellen Station
                for (int j = 0; j < pointClouds[i].Length; j++)
                {
                    lines.Add(pointClouds[i][j].x.ToString(Sys.InvariantCulture) + " "
                            + pointClouds[i][j].y.ToString(Sys.InvariantCulture) + " "
                            + pointClouds[i][j].z.ToString(Sys.InvariantCulture));
                }

                // Datei für die aktuelle Station erstellen und Punkte speichern
                string fileName = $"Station_{i + 1}.txt"; // oder .e57, je nach Format
                File.WriteAllLines(fileName, lines);
            }

            //File.WriteAllLines(Path.Combine(path, "simulatedPointcloud.txt"), lines);
            #endregion write pointcloud in XYZ
            Log.Information("write pointcloud in XYZ");
            #region color not visible faces     

            //ElementId[] matId = default;

            //// add materials and save the ElementIds in a DataStorage
            //try
            //{
            //    matId = Helper.AddMaterials(doc);
            //}
            //catch (Exception)
            //{

            //    matId = Helper.ReadMaterialsDS(doc);
            //}

            //Helper.Paint.ColourFace(doc, notVisibleFacesId, matId[0]);
            //Helper.Paint.ColourFace(doc, visibleFacesId, matId[5]);

            #endregion  color not visible faces
            Log.Information("coloring faces");
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
            #region dataStorage
            //var z = new List<XYZ>();
            //foreach (var item in stationsPBP)
            //{
            //   z.Add(new XYZ(item.x, item.y, item.z));
            //}   
            ////IList<XYZ> stationsDS = new List<XYZ>();

            //// add stations to DataStorage
            //try
            //{
            //    TaskDialog.Show("Message", "okay");
            //    //stationsDS = Helper.AddStation(doc, z);
            //    Helper.AddStation(doc, z);
            //}
            //catch (Exception)
            //{
            //    TaskDialog.Show("Message", "shit");
            //    //stationsDS = Helper.ReadStationsDS(doc);
            //}

            #endregion dataStorage
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