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
using S = ScantraIO.Data;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Structure;
using System.Diagnostics;
using System.Globalization;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class StationsRaster : IExternalCommand
    {
        string path;
        public const string CsvHeader = "Rechtswert;Hochwert;Hoehe";
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
            #region visible and not visible faces

            // visible faces per station
            var visibleFacesIdArray = Raycasting.VisibleFaces(facesRevit, referencePlanesRevit, listVector, set, out D3.Vector[][] pointClouds, out Dictionary<S.Id, int> test, out HashSet<S.Id> hashPMin);

            Log.Information("visible faces finish");
            //Test 
            //var pMin = set.StepsPerFullTurn * set.StepsPerFullTurn * set.Beta_Degree / 1000;
            //var y = 0;
            //foreach (var item in test)
            //{
            //    y += item.Value;
            //}

            var visibleFaceId = new HashSet<S.Id>();
            var visibleFaces = new HashSet<S.PlanarFace>();
            var pFMap = new Dictionary<S.Id, S.PlanarFace>();
            foreach (var pf in facesRevit)
            {
                pFMap[pf.Id] = pf;
            }
            for (int i = 0; i < listVector.Count; i++)
            {
                foreach (S.Id id in visibleFacesIdArray[i])
                {
                    visibleFaces.Add(pFMap[id]);
                    visibleFaceId.Add(id);
                }
            }

            // visible faces
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

            #endregion visible and not visible faces
            Log.Information("visible and not visible faces");
            #region write pointcloud in XYZ

            for (int i = 0; i < listVector.Count; i++)
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
                string xyzPath = Path.Combine(csvPath, $"Station_{i}.xyz");
                File.WriteAllLines(xyzPath, lines);

                //Umwandlung in cloudcompare

                double tx = -listVector[i].x;
                double ty = -listVector[i].y;
                double tz = -set.HeightOfScanner_Meter;

                // Erstelle die Transformationsmatrix als Liste von Strings
                var transformationLines = new string[]
                {
                    "1 0 0 " + tx.ToString(CultureInfo.InvariantCulture),
                    "0 1 0 " + ty.ToString(CultureInfo.InvariantCulture),
                    "0 0 1 " + tz.ToString(CultureInfo.InvariantCulture),
                    "0 0 0 1"
                };

                // Pfad zur Transformationsdatei
                string transformationFilePath = Path.Combine(csvPath, "transformation.txt");

                File.WriteAllLines(transformationFilePath, transformationLines);

                // Pfad zur Ausgabedatei
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
            
            TaskDialog.Show("Message", "fertig");
            return Result.Succeeded;
        }
    }
}