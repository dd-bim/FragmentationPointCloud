using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Serilog;
using D3 = GeometryLib.Double.D3;
using S = ScantraIO.Data;
using Transform = Autodesk.Revit.DB.Transform;
using Sys = System.Globalization.CultureInfo;
using Path = System.IO.Path;
using Document = Autodesk.Revit.DB.Document;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class Stations2Pointclouds : IExternalCommand
    {
        string path;
        public const string CsvHeader = "East;North;Elevation";
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
            Log.Information("start Stations2PointClouds");

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
            #region stations
            var allStations = Helper.CollectFamilyInstances(doc, trans, "ScanStation");

            Log.Information("read files");

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
            #endregion stations
            Log.Information(allStations.Count.ToString() + " stations");
            #region write pointcloud in XYZ
            var pointClouds = CreatePointcloud.VisibleFaces(facesRevit, referencePlanesRevit, listVector, set);
            for (int i = 0; i < listVector.Count; i++)
            {
                List<string> lines = new List<string>();

                // collect points of the current station
                for (int j = 0; j < pointClouds[i].Length; j++)
                {
                    lines.Add(pointClouds[i][j].x.ToString(Sys.InvariantCulture) + " "
                            + pointClouds[i][j].y.ToString(Sys.InvariantCulture) + " "
                            + pointClouds[i][j].z.ToString(Sys.InvariantCulture));
                }

                // create file for the current station and save points
                string xyzPath = Path.Combine(csvPath, $"Station_{i}.xyz");
                File.WriteAllLines(xyzPath, lines);

                // conversion with cloudcompare
                double tx = -listVector[i].x;
                double ty = -listVector[i].y;
                double tz = -listVector[i].z;

                // create the transformation matrix for station-centered point cloud
                var transformationLines = new string[]
                {
                    "1 0 0 " + tx.ToString(CultureInfo.InvariantCulture),
                    "0 1 0 " + ty.ToString(CultureInfo.InvariantCulture),
                    "0 0 1 " + tz.ToString(CultureInfo.InvariantCulture),
                    "0 0 0 1"
                };

                // path to transformation file
                string transformationFilePath = Path.Combine(csvPath, "transformation.txt");

                File.WriteAllLines(transformationFilePath, transformationLines);

                // path to E57
                string outputPointCloud = Path.Combine(csvPath, $"Station_{i}.e57");

                Process cloudCompareProcess = new Process();

                // Configure the process object with the required arguments
                cloudCompareProcess.StartInfo.FileName = set.PathCloudCompare;
                cloudCompareProcess.StartInfo.Arguments = "-SILENT -O \"" + xyzPath + "\" -APPLY_TRANS \"" + transformationFilePath + "\" -C_EXPORT_FMT E57 -SAVE_CLOUDS FILE \"" + outputPointCloud + "\"";
                cloudCompareProcess.Start();
                cloudCompareProcess.WaitForExit();
            }

            #endregion write pointcloud in XYZ
            Log.Information("write pointcloud in XYZ");
            TaskDialog.Show("Message", "finish");
            return Result.Succeeded;
        }
    }
}