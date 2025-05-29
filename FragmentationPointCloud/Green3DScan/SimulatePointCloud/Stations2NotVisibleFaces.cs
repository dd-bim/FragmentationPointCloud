using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JetBrains.Annotations;

using Revit.Green3DScan.SimulatePointCloud;

using Serilog;
//using CoordinateSystem = GeometryLib.D3.CoordinateSystem;
using S = Revit.Data;
//using Sys = System.Globalization.CultureInfo;
//using Path = System.IO.Path;
//using Vector = GeometryLib.D3.Vector;

namespace Revit.Green3DScan.SimulatePointCloud
{
    [Transaction(TransactionMode.Manual)]
    [UsedImplicitly]
    public class Stations2NotVisibleFaces : IExternalCommand
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
            Log.Information("start Stations2NotVisibleFaces");
            Log.Information(set.BBox_Buffer.ToString());

            Transform trans = Helper.GetTransformation(doc, set, out CoordinateSystem crs);

            #endregion setup

            Log.Information("setup");

            #region select files

            // Faces
            var fodPfRevit = new FileOpenDialog("CSV file (*.csv)|*.csv");
            fodPfRevit.Title = "Select CSV file with BimFaces from Revit!";
            if (fodPfRevit.Show() == ItemSelectionDialogResult.Canceled) return Result.Cancelled;
            string csvPathPfRevit = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodPfRevit.GetSelectedModelPath());

            var fodRpRevit = new FileOpenDialog("CSV file (*.csv)|*.csv");
            fodRpRevit.Title = "Select CSV file with BimFacesPlanes fromRevit!";
            if (fodRpRevit.Show() == ItemSelectionDialogResult.Canceled) return Result.Cancelled;
            string csvPathRpRevit = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodRpRevit.GetSelectedModelPath());

            #endregion select files

            Log.Information("select files");

            #region read files

            var facesRevit = S.PlanarFace.ReadCsv(csvPathPfRevit, out string[] lineErrors1, out string error1);
            var facesMap = new Dictionary<S.Id, S.PlanarFace>();
            foreach (S.PlanarFace pf in facesRevit) facesMap[pf.Id] = pf;

            var referencePlanesRevit =
                S.ReferencePlane.ReadCsv(csvPathRpRevit, out string[] lineErrors2, out string error2);

            var stations = Helper.CollectFamilyInstances(doc, trans, "ScanStation");

            #endregion read files

            Log.Information("read files");

            #region write stations to csv

            string csvPath = Path.Combine(path, "07_Stations/");

            if (!Directory.Exists(csvPath)) Directory.CreateDirectory(csvPath);

            using StreamWriter csv = File.CreateText(Path.Combine(csvPath, "Stations.csv"));
            csv.WriteLine(CsvHeader);

            foreach (XYZ item in stations)
            {
                csv.WriteLine(item.X.ToString(Sys.InvariantCulture) + ";" + item.Y.ToString(Sys.InvariantCulture) +
                              ";" + item.Z.ToString(Sys.InvariantCulture));
            }

            csv.Close();

            var listVector = new List<Vector>();
            foreach (XYZ item in stations) listVector.Add(new Vector(item.X, item.Y, item.Z));
            Log.Information(stations.Count + " stations");

            #endregion read files

            Log.Information("write stations to csv");

            #region visible and not visible faces

            var visibleFacesId = RayCasting.VisibleFaces(facesRevit, referencePlanesRevit, listVector, set,
                out var pointClouds, out var test, out var hashPMin);

            //Test 
            var y = 0;
            foreach (var item in test)
                //Log.Information(item.Key.ToString());
                //Log.Information(item.Value.ToString());
                y += item.Value;
            Log.Information(y.ToString());

            var visibleFaceId = new HashSet<S.Id>();
            var visibleFaces = new HashSet<S.PlanarFace>();
            var visibleRefPlanes = new HashSet<S.ReferencePlane>();
            var pFMap = new Dictionary<S.Id, S.PlanarFace>();
            foreach (S.PlanarFace pf in facesRevit) pFMap[pf.Id] = pf;
            for (var i = 0; i < stations.Count; i++)
            {
                foreach (S.Id id in visibleFacesId[i])
                {
                    visibleFaces.Add(pFMap[id]);
                    visibleRefPlanes.Add(referencePlanesRevit[pFMap[id].ReferencePlaneId]);
                    visibleFaceId.Add(id);
                }
            }

            // visible faces
            S.PlanarFace.WriteCsv(Path.Combine(path, "VisibleFaces.csv"), visibleFaces);
            S.ReferencePlane.WriteCsv(Path.Combine(path, "VisibleRefPlanes.csv"), visibleRefPlanes);

            var notVisibleFacesId = new List<S.Id>();
            var notVisibleFaces = new List<S.PlanarFace>();
            var notvisibleRefPlanes = new HashSet<S.ReferencePlane>();

            var facesIdList = new List<S.Id>();
            foreach (var item in facesMap) facesIdList.Add(item.Key);

            // not visible faces
            foreach (S.Id item in facesIdList)
            {
                if (!visibleFaceId.Contains(item))
                {
                    notVisibleFacesId.Add(item);
                    notvisibleRefPlanes.Add(referencePlanesRevit[facesMap[item].ReferencePlaneId]);
                    notVisibleFaces.Add(facesMap[item]);
                }
            }

            S.PlanarFace.WriteCsv(Path.Combine(path, "NotVisibleFaces.csv"), notVisibleFaces);
            S.ReferencePlane.WriteCsv(Path.Combine(path, "NotVisibleRefPlanes.csv"), notvisibleRefPlanes);

            #endregion visible and not visible faces

            Log.Information("visible and not visible faces");

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

            //Helper.Paint.ColourFace(doc, notVisibleFacesId, matId[0]);
            Helper.Paint.ColourFace(doc, hashPMin.ToList(), matId[5]);

            #endregion color not visible faces

            Log.Information("color not visible faces");
            TaskDialog.Show("Message", "finish");
            Log.Information("end Stations2NotVisibleFaces");
            return Result.Succeeded;
        }
    }
}