using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using D3 = GeometryLib.Double.D3;
using Serilog;
using Transform = Autodesk.Revit.DB.Transform;
using Sys = System.Globalization.CultureInfo;
using Path = System.IO.Path;
using Document = Autodesk.Revit.DB.Document;
using Line = Autodesk.Revit.DB.Line;
using S = ScantraIO.Data;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class Stations2NotVisibleFaces : IExternalCommand
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

            #region select files

            // Faces
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

            //// Stations
            //var fodStations = new FileOpenDialog("CSV file (*.csv)|*.csv");
            //fodStations.Title = "Select CSV file with stations from Revit!";
            //if (fodStations.Show() == ItemSelectionDialogResult.Canceled)
            //{
            //    return Result.Cancelled;
            //}
            //var csvPathStations = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodStations.GetSelectedModelPath());

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

            var allStations = CollectFamilyInstances(doc, trans, "ScanStation");
            TaskDialog.Show("Message", allStations.Count.ToString() + " ScanStations");

            #region write stations to csv

            string csvPath = Path.Combine(path, "07_Stations/");

            if (!Directory.Exists(csvPath))
            {
                Directory.CreateDirectory(csvPath);
            }

            using StreamWriter csv = File.CreateText(Path.Combine(csvPath, "Stations.csv"));
            csv.WriteLine(CsvHeader);

            foreach (var item in allStations)
            {
                csv.WriteLine(item.X.ToString(Sys.InvariantCulture) + ";" + item.Y.ToString(Sys.InvariantCulture) + ";" + item.Z.ToString(Sys.InvariantCulture));
            }
            csv.Close();

            var listVector = new List<D3.Vector>();
            foreach (var item in allStations)
            {
                listVector.Add(new D3.Vector(item.X, item.Y, item.Z));
            }
            Log.Information(allStations.Count.ToString() + " stations");


            #endregion read files

            #region visible and not visible faces

            var visibleFacesId = Raycasting.VisibleFaces(facesRevit, referencePlanesRevit, listVector, set, out D3.Vector[][] pointClouds, out Dictionary<S.Id, int> test);

            //Test 
            var y = 0;
            foreach (var item in test)
            {
                Log.Information(item.Key.ToString());
                Log.Information(item.Value.ToString());
                y += item.Value;
            }
            Log.Information(y.ToString());
            Log.Information(test.ToString());

            var visibleFaceId = new HashSet<S.Id>();
            var visibleFaces = new HashSet<S.PlanarFace>();
            var pFMap = new Dictionary<S.Id, S.PlanarFace>();
            foreach (var pf in facesRevit)
            {
                pFMap[pf.Id] = pf;
            }
            for (int i = 0; i < allStations.Count; i++)
            //for (int i = 0; i < stations.Count; i++)
            {
                foreach (S.Id id in visibleFacesId[i])
                {
                    visibleFaces.Add(pFMap[id]);
                    visibleFaceId.Add(id);
                }
            }
            // visible faces
            //S.PlanarFace.WriteCsv(csvVisibleFaces, visibleFaces);
            //S.ReferencePlane.WriteCsv(csvVisibleFacesRef, refPlanes);
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
            //S.PlanarFace.WriteCsv(csvVisibleFaces, notVisibleFaces);
            //S.ReferencePlane.WriteCsv(csvVisibleFacesRef, refPlanes);
            //S.PlanarFace.WriteObj(Path.Combine(path, "notVisible"), objPlanes, notVisibleFaces);

            #endregion visible and not visible faces

            #region write pointcloud in XYZ
            List<string> lines = new List<string>();
            for (int i = 0; i < allStations.Count; i++)
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

            #region ScanStation

            // create spheres with internal coordinates
            for (int i = 0; i < allStations.Count; i++)
            {
                // ScanStation
                List<Curve> profile = new List<Curve>();
                XYZ station = new XYZ(allStations[i].X, allStations[i].Y, allStations[i].Z);
                XYZ stationsInternal = trans.OfPoint(station) * Constants.meter2Feet;
                double radius = set.SphereDiameter_Meter / 2 * Constants.meter2Feet;
                XYZ profilePlus = stationsInternal + new XYZ(0, radius, 0);
                XYZ profileMinus = stationsInternal - new XYZ(0, radius, 0);

                profile.Add(Line.CreateBound(profilePlus, profileMinus));
                profile.Add(Arc.Create(profileMinus, profilePlus, stationsInternal + new XYZ(radius, 0, 0)));

                CurveLoop curveLoop = CurveLoop.Create(profile);
                SolidOptions options = new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId);

                Frame frame = new Frame(stationsInternal, XYZ.BasisX, -XYZ.BasisZ, XYZ.BasisY);
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

            #endregion ScanStation

            TaskDialog.Show("Message", "Successful");
            return Result.Succeeded;
        }
        public List<XYZ> CollectFamilyInstances(Document doc, Transform trans, string familyName)
        {
            var listStations = new List<XYZ>();
            // Step 1: Get the Family object by name
            Family family = GetFamilyByName(doc, familyName);
            if (family == null)
            {
                TaskDialog.Show("Message", $"Family {familyName} not found.");
                return default;
            }

            // Step 2: Get all instances of the Family
            List<FamilyInstance> familyInstances = GetFamilyInstances(doc, family.Id);
            foreach (var item in familyInstances)
            {
                if (item.Location is LocationPoint locationPoint)
                {
                    listStations.Add(trans.OfPoint(locationPoint.Point) * Constants.feet2Meter);
                }
            }
            return listStations;
        }
        private Family GetFamilyByName(Document doc, string familyName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(Family));

            foreach (Family family in collector)
            {
                if (family.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase))
                {
                    return family;
                }
            }
            return null;
        }
        private List<FamilyInstance> GetFamilyInstances(Document doc, ElementId familyId)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(FamilyInstance));

            List<FamilyInstance> instances = new List<FamilyInstance>();

            foreach (FamilyInstance instance in collector)
            {
                if (instance.Symbol.Family.Id == familyId)
                {
                    instances.Add(instance);
                }
            }
            return instances;
        }
    }
}