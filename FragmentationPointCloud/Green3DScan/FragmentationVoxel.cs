using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Except = Autodesk.Revit.Exceptions;
using Serilog;
using Transform = Autodesk.Revit.DB.Transform;
using Path = System.IO.Path;
using Sys = System.Globalization.CultureInfo;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class FragmentationVoxel : IExternalCommand
    {
        #region Execute
        string path;
        string dateBimLastModified;
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
                dateBimLastModified = date.Year + "-" + date.Month + "-" + date.Day + "-" + date.Hour + "-" + date.Minute;
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
            Log.Information("start FragmentationVoxel");
            Log.Information(set.BBox_Buffer.ToString());
            #endregion setup

            string pcdPathPointcloud = set.PathPointCloud;

            try
            {
                // step 1: choose bboxes an read them
                FileOpenDialog fodBBox = new FileOpenDialog("CSV file (*.csv)|*.csv");
                fodBBox.Title = "Select csv file with bboxes from Revit!";
                if (fodBBox.Show() == ItemSelectionDialogResult.Canceled)
                {
                    return Result.Cancelled;
                }
                string csvPathBBoxes = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodBBox.GetSelectedModelPath());

                // CSV lesen
                if (!ReadCsvBoxes(csvPathBBoxes, out List<Helper.OrientedBoundingBox> obboxes))
                {
                    TaskDialog.Show("Message", "Reading csv not successful");
                    return Result.Failed;
                }
                Log.Information("read csv successful");

                // step 2: Fragmentation and save small pcd
                string exeGreen3DPath = Constants.exeFragmentationBBox;
                string rcpFilePathBBox = Path.Combine(path, "07_FragmentationBBox\\");
                if (!Directory.Exists(rcpFilePathBBox))
                {
                    Directory.CreateDirectory(rcpFilePathBBox);
                }

                string command = $"{pcdPathPointcloud} {csvPathBBoxes} {rcpFilePathBBox} {dateBimLastModified}";
                if (!Helper.Fragmentation2Pcd(exeGreen3DPath, command))
                {
                    TaskDialog.Show("Message", "Fragmentation error");
                    return Result.Failed;
                }
                Log.Information("bbox fragmentation successful");
                foreach (Helper.OrientedBoundingBox obox in obboxes)
                {

                    // step 3: conversion PCD --> E57 
                    if (!Helper.Pcd2e57(Path.Combine(rcpFilePathBBox, obox.ObjectGuid + ".pcd"), Path.Combine(rcpFilePathBBox, obox.ObjectGuid + ".e57")))
                    {
                        Log.Information("Message", "CloudCompare error");
                        return Result.Failed;
                    }

                    // step 4: conversion E57 --> RCP
                    if (!Helper.DeCap(Path.Combine(path, "07_FragmentationBBox"), obox.ObjectGuid, Path.Combine(rcpFilePathBBox, obox.ObjectGuid + ".e57")))
                    {
                        Log.Information("Message", "DeCap error");
                        return Result.Failed;
                    }
                }

                // step 5: Fragmentation and save voxel as pcd
                string pathExeFragmentationVoxel = Constants.exeFragmentationVoxel;
                string rcpFilePathVoxel = Path.Combine(path, "08_FragmentationVoxel\\");
                if (!Directory.Exists(rcpFilePathVoxel))
                {
                    Directory.CreateDirectory(rcpFilePathVoxel);
                }
                string commandSegementation = $"{pcdPathPointcloud} {rcpFilePathVoxel} {set.FragmentationVoxelResolution_Meter.ToString(Sys.InvariantCulture)}";
                if (!FragmentationVoxel2Pcd(pathExeFragmentationVoxel, commandSegementation))
                {
                    TaskDialog.Show("Message", "Fragmentation error");
                    return Result.Failed;
                }

                // Schritt 6: Suchen der Voxel Indices zu jeder BBox
                string pathExeSearchVoxel = Constants.exeSearchVoxel;
                string commandSearch = $"{pcdPathPointcloud} {csvPathBBoxes} {path} {set.FragmentationVoxelResolution_Meter.ToString(Sys.InvariantCulture)}";
                Log.Information(commandSearch);
                if (!SearchVoxel(pathExeSearchVoxel, commandSearch))
                {
                    TaskDialog.Show("Message", "Fragmentation error");
                    return Result.Failed;
                }

                ////ReadCsvAllIndices(Path.Combine(rcpFilePath, "all_voxel_indices.csv"), out var allIndices);
                //ReadCsvAllIndices(Path.Combine(Path.GetDirectoryName(csvPathBBoxes), "Occupied_Voxel.csv"), out var occupiedVoxel);
            
                ////foreach (string index in allIndices)
                //foreach (string index in occupiedVoxel)
                //{
                //    // Schritt 4: Mit CloudCompare PCD --> E57 
                //    if (!Helper.Pcd2e57(Path.Combine(rcpFilePathVoxel, index + ".pcd"), Path.Combine(rcpFilePathVoxel, index + ".e57")))
                //    {
                //        TaskDialog.Show("Message", "CloudCompare error");
                //        return Result.Failed;
                //    }

                //    // Schritt 5: Mit DeCap E57 --> RCP
                //    if (!Helper.DeCap(Path.Combine(path, "08_FragmentationVoxel"), index, Path.Combine(rcpFilePathVoxel, index + ".e57")))
                //    {
                //        TaskDialog.Show("Message", "DeCap error");
                //        return Result.Failed;
                //    }
                //}
            }
            #region catch
            catch (Except.OperationCanceledException)
            {
                TaskDialog.Show("Message", "Error 1: Command canceled.");
                return Result.Failed;
            }
            catch (Except.ForbiddenForDynamicUpdateException)
            {
                TaskDialog.Show("Message", "Error 2");
                return Result.Failed;
            }
            catch (Exception ex)
            {
                message += "Error message::" + ex.ToString();
                TaskDialog.Show("Message", message);
                return Result.Failed;
            }
                #endregion catch
            TaskDialog.Show("Message", "Fragmentation successful!");
            return Result.Succeeded;
        }
        #endregion execute
        private bool ReadCsvBoxes(string csvPathBBoxes, out List<Helper.OrientedBoundingBox> listOBBox)
        {
            List<Helper.OrientedBoundingBox> list = new List<Helper.OrientedBoundingBox>();
            try
            {
                using (StreamReader reader = new StreamReader(csvPathBBoxes))
                {
                    reader.ReadLine();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] columns = line.Split(';');

                        if (columns.Length == 25)
                        {
                            list.Add(new Helper.OrientedBoundingBox(bool.Parse(columns[0]), columns[1], columns[2], columns[3],
                                new XYZ(double.Parse(columns[4]), double.Parse(columns[5]), double.Parse(columns[6])),
                                new XYZ(double.Parse(columns[7]), double.Parse(columns[8]), double.Parse(columns[9])),
                                new XYZ(double.Parse(columns[10]), double.Parse(columns[11]), double.Parse(columns[12])),
                                new XYZ(double.Parse(columns[13]), double.Parse(columns[14]), double.Parse(columns[15])),
                                double.Parse(columns[16]), double.Parse(columns[17]), double.Parse(columns[18])));
                        }
                        else
                        {
                            TaskDialog.Show("Message", "Incorrect line: " + line);
                        }
                    }
                }
                listOBBox = list;
                return true;
            }
            catch (Exception)
            {
                listOBBox = list;
                return false;
            }
        }
        private bool FragmentationVoxel2Pcd(string exePath, string command)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(exePath, command);
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.CreateNoWindow = true;
                Process process = new Process();
                process.StartInfo = processInfo;

                process.Start();

                while (!process.StandardOutput.EndOfStream)
                {
                    string outputLine = process.StandardOutput.ReadLine();
                    Log.Information(outputLine); 
                }

                process.WaitForExit();

                //string output = process.StandardOutput.ReadToEnd();
                //Console.WriteLine(output);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private bool SearchVoxel(string exePath, string command)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(exePath, command);
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.CreateNoWindow = true;
                Process process = new Process();
                process.StartInfo = processInfo;

                process.Start();

                while (!process.StandardOutput.EndOfStream)
                {
                    string outputLine = process.StandardOutput.ReadLine();
                    Log.Information(outputLine);
                }

                process.WaitForExit();

                //string output = process.StandardOutput.ReadToEnd();
                //Console.WriteLine(output);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        private bool ReadCsvAllIndices(string csvPathIndices, out List<string> listAllIndices)
        {
            List<string> list = new List<string>();
            try
            {
                using (StreamReader reader = new StreamReader(csvPathIndices))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        list.Add(line);
                    }
                }
                listAllIndices = list;
                return true;
            }
            catch (Exception)
            {
                listAllIndices = list;
                return false;
            }
        }
    }
}