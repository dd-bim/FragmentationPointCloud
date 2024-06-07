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
using S = ScantraIO.Data;
using NetTopologySuite.Algorithm;
using System.Collections.ObjectModel;
using Autodesk.Revit.DB.Visual;
using Autodesk.Internal.Windows;


namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class Revit2Stations : IExternalCommand
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

            Transform trans = Helper.GetTransformation(doc, set, out var crs);

            #endregion setup

            string csvVisibleFaces = Path.Combine(path, "Revit2StationsVisibleFaces.csv");
            string csvVisibleFacesRef = Path.Combine(path, "Revit2StationsVisibleFacesRef.csv");

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

            var facesRevit = S.PlanarFace.ReadCsv(csvPathPfRevit, out var lineErrors1, out string error1);
            var facesMap = new Dictionary<S.Id, S.PlanarFace>();
            foreach (var pf in facesRevit)
            {
                facesMap[pf.Id] = pf;
            }

            var referencePlanesRevit = S.ReferencePlane.ReadCsv(csvPathRpRevit, out var lineErrors2, out string error2);

            #endregion read files

            //Faces
            View activeView = doc.ActiveView;

            // Sammeln aller Räume in der aktiven Ansicht
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

            TaskDialog.Show("Message", faces.Count.ToString() + " Faces wurden von Räumen geschrieben");
            
            #region stations

            //----------------------------------
            // Stations
            //----------------------------------

            List<D3.Vector> stations = new List<D3.Vector>(); 
            List<D3.Vector> stationsPBP = new List<D3.Vector>();

            // collect doors
            FilteredElementCollector collDoors = new FilteredElementCollector(doc, activeView.Id);
            ICollection<Element> doors = collDoors.OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().ToElements();
            
            double heigth = set.HeightOfSphere_Meter;
            //XYZ transformedHeight = trans.Inverse.OfPoint(new XYZ(0, 0, heigth)) * Constants.meter2Feet;

            foreach (Element door in doors)
            {
                var loc = door.Location;

                if (loc is LocationCurve locationCurve)
                {
                    Log.Information("Wall, but no LocationPoint.");
                }   
                else if (loc is LocationPoint locationPoint)
                {
                    //var doorPoint = trans.OfPoint(locationPoint.Point) * Constants.feet2Meter;
                    //stations.Add(new D3.Vector(doorPoint.X, doorPoint.Y, transformedHeight.Z));

                    stations.Add(new D3.Vector(locationPoint.Point.X, locationPoint.Point.Y, heigth * Constants.meter2Feet));
                }
            }

            TaskDialog.Show("Message", doors.Count.ToString() + " doors");

            // collect rooms
            foreach (Element room in rooms)
            {
                if (room is SpatialElement spatialRoom && spatialRoom.Location is LocationPoint locationPoint)
                {
                    //var roomPoint = trans.OfPoint(locationPoint.Point) * Constants.feet2Meter;
                    //stations.Add(new D3.Vector(roomPoint.X, roomPoint.Y, transformedHeight.Z));

                    stations.Add(new D3.Vector(locationPoint.Point.X, locationPoint.Point.Y, heigth * Constants.meter2Feet));
                }
            }

            //foreach (var item in stations)
            //{
            //    Log.Information(item.xyz.ToString());
            //    var x = new XYZ(item.x, item.y, item.z);
            //    var xTrans = trans.OfPoint(x) * Constants.feet2Meter;
            //    Log.Information(xTrans.ToString());
            //    stationsPBP.Add(new D3.Vector(xTrans.X, xTrans.Y, xTrans.Z));
            //}
            for (int i = 0; i < 1; i++)
            {
                Log.Information(stations[i].xyz.ToString());
                var x = new XYZ(stations[i].x, stations[i].y, stations[i].z);
                var xTrans = trans.OfPoint(x) * Constants.feet2Meter;
                Log.Information(xTrans.ToString());
                stationsPBP.Add(new D3.Vector(xTrans.X, xTrans.Y, xTrans.Z));
            }
            TaskDialog.Show("Message", rooms.Count.ToString() + " rooms");

            #endregion stations

            TaskDialog.Show("Message", stations.Count.ToString() + " stations");

            #region visible and not visible faces

            var visibleFacesId = Raycasting.VisibleFaces(facesRevit, referencePlanesRevit, stationsPBP, set, out D3.Vector[][] pointClouds);
            var visibleFaceId = new HashSet<S.Id>();
            var visibleFaces = new HashSet<S.PlanarFace>();
            var pFMap = new Dictionary<S.Id, S.PlanarFace>();
            foreach (var pf in facesRevit)
            {
                pFMap[pf.Id] = pf;
            }
            for (int i = 0; i < stationsPBP.Count; i++)
            //for (int i = 0; i < stations.Count; i++)
            {
                foreach (S.Id id in visibleFacesId[i])
                {
                    visibleFaces.Add(pFMap[id]);
                }
            }
            // visible faces
            S.PlanarFace.WriteCsv(csvVisibleFaces, visibleFaces);
            S.ReferencePlane.WriteCsv(csvVisibleFacesRef, refPlanes);
            //S.PlanarFace.WriteObj(Path.Combine(path, "visible"), objPlanes, visibleFaces);

            var notVisibleFacesId = new List<S.Id>();
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
            }
            S.PlanarFace.WriteCsv(csvVisibleFaces, notVisibleFaces);
            S.ReferencePlane.WriteCsv(csvVisibleFacesRef, refPlanes);
            //S.PlanarFace.WriteObj(Path.Combine(path, "notVisible"), objPlanes, notVisibleFaces);

            #endregion visible and not visible faces

            #region write pointcloud in XYZ
            List<string> lines = new List<string>();
            for (int i = 0; i < stationsPBP.Count; i++)
            {
                foreach (S.Id id in visibleFacesId[i])
                {
                    visibleFaceId.Add(id);
                }
                for (int j = 0; j < pointClouds[i].Length; j++)
                {
                    lines.Add(pointClouds[i][j].x.ToString(Sys.InvariantCulture) + " " + pointClouds[i][j].y.ToString(Sys.InvariantCulture) + " " + pointClouds[i][j].z.ToString(Sys.InvariantCulture));
                }
            }
            File.WriteAllLines(Path.Combine(path, "simulatedPointcloud.txt"), lines);
            #endregion write pointcloud in XYZ

            #region color not visible faces     

            ElementId[] matId = default;

            // add materials and save the ElementIds in a DataStorage
            try
            {
                matId = Helper.AddMaterials(doc);
            }
            catch (Exception)
            {

                matId = Helper.ReadMaterialsDS(doc);
            }

            Helper.Paint.ColourFace(doc, notVisibleFacesId, matId[0]);

            #endregion  color not visible faces

            #region sphere

            // create spheres with internal coordinates
            for (int i = 0; i < stationsPBP.Count; i++)
            {
               // sphere
                List<Curve> profile = new List<Curve>();
                XYZ station = new XYZ(stations[i].x, stations[i].y, stations[i].z);
                double radius = set.SphereDiameter_Meter/2 * Constants.meter2Feet;
                XYZ profilePlus = station + new XYZ(0, radius, 0);
                XYZ profileMinus = station - new XYZ(0, radius, 0);

                profile.Add(Line.CreateBound(profilePlus, profileMinus));
                profile.Add(Arc.Create(profileMinus, profilePlus, station + new XYZ(radius, 0, 0)));

                CurveLoop curveLoop = CurveLoop.Create(profile);
                SolidOptions options = new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId);

                Frame frame = new Frame(station, XYZ.BasisX, -XYZ.BasisZ, XYZ.BasisY);
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
            #endregion sphere

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