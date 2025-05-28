//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using Serilog;
//using Except = Autodesk.Revit.Exceptions;
//using Sys = System.Globalization.CultureInfo;
//using TaskDialog = Autodesk.Revit.UI.TaskDialog;

//namespace Revit.Green3DScan.Fragmentation
//{
//    [Transaction(TransactionMode.Manual)]
//    public class FragmentationIFC : IExternalCommand
//    {
//        private static void ReadCsvBoxes(string csvPathBBoxes, out List<Helper.OrientedBoundingBox> listOBBox)
//        {
//            var list = new List<Helper.OrientedBoundingBox>();
//            try
//            {
//                using (var reader = new StreamReader(csvPathBBoxes))
//                {
//                    reader.ReadLine();
//                    while (reader.ReadLine() is { } line)
//                    {
//                        string[] columns = line.Split(';');

//                        if (columns.Length == 25)
//                            list.Add(new Helper.OrientedBoundingBox(bool.Parse(columns[0]), columns[1], columns[2],
//                                columns[4],
//                                new XYZ(double.Parse(columns[10], Sys.InvariantCulture),
//                                    double.Parse(columns[11], Sys.InvariantCulture),
//                                    double.Parse(columns[12], Sys.InvariantCulture)),
//                                new XYZ(double.Parse(columns[13], Sys.InvariantCulture),
//                                    double.Parse(columns[14], Sys.InvariantCulture),
//                                    double.Parse(columns[15], Sys.InvariantCulture)),
//                                new XYZ(double.Parse(columns[16], Sys.InvariantCulture),
//                                    double.Parse(columns[17], Sys.InvariantCulture),
//                                    double.Parse(columns[18], Sys.InvariantCulture)),
//                                new XYZ(double.Parse(columns[19], Sys.InvariantCulture),
//                                    double.Parse(columns[20], Sys.InvariantCulture),
//                                    double.Parse(columns[21], Sys.InvariantCulture)),
//                                double.Parse(columns[22], Sys.InvariantCulture),
//                                double.Parse(columns[23], Sys.InvariantCulture),
//                                double.Parse(columns[24], Sys.InvariantCulture),
//                                new XYZ(double.Parse(columns[4], Sys.InvariantCulture),
//                                    double.Parse(columns[5], Sys.InvariantCulture),
//                                    double.Parse(columns[6], Sys.InvariantCulture)),
//                                new XYZ(double.Parse(columns[7], Sys.InvariantCulture),
//                                    double.Parse(columns[8], Sys.InvariantCulture),
//                                    double.Parse(columns[9], Sys.InvariantCulture))));
//                        else
//                            TaskDialog.Show("Message", "Reading csv incorrect at line " + line + " " + columns.Length);
//                    }
//                }

//                listOBBox = list;
//            }
//            catch (Exception)
//            {
//                listOBBox = list;
//            }
//        }

//        private static void WriteObBoxToObjFile(List<Helper.OrientedBoundingBox> oboxes, string filePath)
//        {
//            using var objFile = new StreamWriter(filePath);
//            var indexOffset = 0;

//            foreach (Helper.OrientedBoundingBox obox in oboxes)
//            {
//                var points = new XYZ[8];
//                points[0] = obox.Center - obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth -
//                            obox.ZDirection * obox.HalfHeight; // Punkt 1
//                points[1] = obox.Center + obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth -
//                            obox.ZDirection * obox.HalfHeight; // Punkt 2
//                points[2] = obox.Center + obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth -
//                            obox.ZDirection * obox.HalfHeight; // Punkt 3
//                points[3] = obox.Center - obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth -
//                            obox.ZDirection * obox.HalfHeight; // Punkt 4
//                points[4] = obox.Center - obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth +
//                            obox.ZDirection * obox.HalfHeight; // Punkt 5
//                points[5] = obox.Center + obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth +
//                            obox.ZDirection * obox.HalfHeight; // Punkt 6
//                points[6] = obox.Center + obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth +
//                            obox.ZDirection * obox.HalfHeight; // Punkt 7
//                points[7] = obox.Center - obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth +
//                            obox.ZDirection * obox.HalfHeight; // Punkt 8

//                foreach (XYZ point in points)
//                {
//                    objFile.WriteLine(
//                        $"v {point.X.ToString(Sys.InvariantCulture)} {point.Y.ToString(Sys.InvariantCulture)} {point.Z.ToString(Sys.InvariantCulture)}");
//                }
//            }

//            foreach (Helper.OrientedBoundingBox unused in oboxes)
//            {
//                int v0 = 1 + indexOffset;
//                int v1 = 2 + indexOffset;
//                int v2 = 3 + indexOffset;
//                int v3 = 4 + indexOffset;
//                int v4 = 5 + indexOffset;
//                int v5 = 6 + indexOffset;
//                int v6 = 7 + indexOffset;
//                int v7 = 8 + indexOffset;

//                objFile.WriteLine("f " + v0 + " " + v1 + " " + v2 + " " + v3);
//                objFile.WriteLine("f " + v4 + " " + v5 + " " + v6 + " " + v7);
//                objFile.WriteLine("f " + v0 + " " + v1 + " " + v5 + " " + v4);
//                objFile.WriteLine("f " + v1 + " " + v2 + " " + v6 + " " + v5);
//                objFile.WriteLine("f " + v2 + " " + v3 + " " + v7 + " " + v6);
//                objFile.WriteLine("f " + v3 + " " + v0 + " " + v4 + " " + v7);

//                indexOffset += 8;
//            }
//        }

//        #region Execute

//        private string path;
//        private string dateBimLastModified;

//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            #region setup

//            // settings json
//            var set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

//            UIDocument uidoc = commandData.Application.ActiveUIDocument;
//            Document doc = uidoc.Document;
//            try
//            {
//                path = Path.GetDirectoryName(doc.PathName);
//                var fileInfo = new FileInfo(path ?? throw new InvalidOperationException());
//                DateTime date = fileInfo.LastWriteTime;
//                dateBimLastModified =
//                    date.Year + "-" + date.Month + "-" + date.Day + "-" + date.Hour + "-" + date.Minute;
//            }
//            catch (Exception)
//            {
//                TaskDialog.Show("Message", "The file has not been saved yet.");
//                return Result.Failed;
//            }

//            // logger
//            string logsPath = Path.Combine(path, "00_Logs/");
//            if (!Directory.Exists(logsPath)) Directory.CreateDirectory(logsPath);
//            Log.Logger = new LoggerConfiguration()
//                .MinimumLevel.Debug()
//                .WriteTo.File(Path.Combine(logsPath, "LogFile_"), rollingInterval: RollingInterval.Minute)
//                .CreateLogger();
//            Log.Information("start FragmentationIFC");
//            Log.Information(set.BBox_Buffer.ToString(Sys.InvariantCulture));

//            #endregion setup

//            string ifcBoxPath = Path.Combine(path, "10_FragmentationIFC\\");
//            string rcpOutputPath = Path.Combine(path, "10_FragmentationIFC");
//            if (!Directory.Exists(ifcBoxPath)) Directory.CreateDirectory(ifcBoxPath);

//            try
//            {
//                // step 1: select ifc
//                var fodBBox = new FileOpenDialog("IFC file (*.ifc)|*.ifc");
//                fodBBox.Title = "Select IFC file!";
//                if (fodBBox.Show() == ItemSelectionDialogResult.Canceled) return Result.Cancelled;
//                string csvPathIfc = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodBBox.GetSelectedModelPath());
//                string csvPathIfcBoxes = Path.Combine(ifcBoxPath, "IFCoBBoxes.csv");

//                // step 2: calculate obboxes from ifc with exe
//                var arg1 = "-i";
//                string arg2 = csvPathIfc;
//                var arg3 = "-boxes";
//                var arg4 = "-boxFile";
//                string arg5 = csvPathIfcBoxes;
//                var arg6 = "-entityList";
//                string arg7 = Constants.jsonIfcBox;
//                var arg8 = "-boxBuffer";
//                var arg9 = set.BBox_Buffer.ToString(Sys.InvariantCulture);
//                var arguments = $"{arg1} \"{arg2}\" {arg3} {arg4} \"{arg5}\" {arg6} \"{arg7}\" {arg8} {arg9}";

//                var startInfo = new ProcessStartInfo
//                {
//                    FileName = Constants.exeIfcBox,
//                    Arguments = arguments,
//                    UseShellExecute = false,
//                    RedirectStandardOutput = true
//                };

//                var process = new Process();
//                process.StartInfo = startInfo;

//                process.Start();
//                process.StandardOutput.ReadToEnd();
//                process.WaitForExit();

//                // step 3: read csv and write obj
//                ReadCsvBoxes(csvPathIfcBoxes, out var obboxes);
//                WriteObBoxToObjFile(obboxes, Path.Combine(ifcBoxPath, "IFCoBBoxes.obj"));

//                // step 4: fragmentation an save small pcd
//                string exeGreen3DPath = Constants.exeFragmentationBBox;
//                var command = $"{set.PathPointCloud} {csvPathIfcBoxes} {ifcBoxPath} {dateBimLastModified}";
//                if (!Helper.Fragmentation2Pcd(exeGreen3DPath, command))
//                {
//                    TaskDialog.Show("Message", "Fragmentation not successful!");
//                    return Result.Failed;
//                }

//                View view = uidoc.ActiveView;
//                try
//                {
//                    var tx = new Transaction(doc, "Load RCP");
//                    tx.Start();
//                    view.ArePointCloudsHidden = false;
//                    tx.Commit();
//                }
//                catch (Exception)
//                {
//                    TaskDialog.Show("Message", "Error change setting ArePointCloudsHidden");
//                }

//                foreach (Helper.OrientedBoundingBox obox in obboxes)
//                {
//                    try
//                    {
//                        // step 5: conversion PCD --> E57 
//                        if (!Helper.Pcd2e57(Path.Combine(ifcBoxPath, obox.ObjectGuid + ".pcd"),
//                                Path.Combine(ifcBoxPath, obox.ObjectGuid + ".e57"), set))
//                        {
//                            TaskDialog.Show("Message", "CloudCompare Fehler");
//                            return Result.Failed;
//                        }

//                        // step 6: conversion E57 --> RCP
//                        if (!Helper.DeCap(rcpOutputPath, obox.ObjectGuid,
//                                Path.Combine(ifcBoxPath, obox.ObjectGuid + ".e57")))
//                        {
//                            TaskDialog.Show("Message", "DeCap Fehler");
//                            return Result.Failed;
//                        }
//                    }

//                    #region catch

//                    catch (Except.OperationCanceledException)
//                    {
//                        TaskDialog.Show("Message", "Error 1: Command canceled.");
//                        return Result.Failed;
//                    }
//                    catch (Except.ForbiddenForDynamicUpdateException)
//                    {
//                        TaskDialog.Show("Message", "Error 2");
//                        return Result.Failed;
//                    }
//                    catch (Exception ex)
//                    {
//                        message += "Error message::" + ex;
//                        TaskDialog.Show("Message", message);
//                        return Result.Failed;
//                    }

//                    #endregion catch
//                }

//                TaskDialog.Show("Message", "IFC Fragmentation successful!");
//                return Result.Succeeded;
//            }

//            #region catch

//            catch (Except.OperationCanceledException)
//            {
//                TaskDialog.Show("Message", "Error 1: Command canceled.");
//                return Result.Failed;
//            }
//            catch (Except.ForbiddenForDynamicUpdateException)
//            {
//                TaskDialog.Show("Message", "Error 2");
//                return Result.Failed;
//            }
//            catch (Exception ex)
//            {
//                message += "Error message::" + ex;
//                TaskDialog.Show("Message", message);
//                return Result.Failed;
//            }

//            #endregion catch
//        }

//        #endregion execute
//    }
//}