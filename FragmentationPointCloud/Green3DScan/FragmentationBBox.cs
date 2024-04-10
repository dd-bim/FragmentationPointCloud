using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Except = Autodesk.Revit.Exceptions;
using Serilog;
using Path = System.IO.Path;
using D3 = GeometryLib.Double.D3;


namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class FragmentationBBox : IExternalCommand
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
            Log.Information("start");
            Log.Information(set.ConstructionTolerance_Meter.ToString());
            #endregion setup

            // step 1: select csv file with bboxes
            FileOpenDialog fodBBox = new FileOpenDialog("CSV file (*.csv)|*.csv");
            fodBBox.Title = "Select CSV file with BBoxes from Revit!";
            if (fodBBox.Show() == ItemSelectionDialogResult.Canceled)
            {
                return Result.Cancelled;
            }
            string csvPathBBoxes = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodBBox.GetSelectedModelPath());

            // read csv
            if (!ReadCsvBoxes(csvPathBBoxes, out List<Helper.OrientedBoundingBox> obboxes))
            {
                TaskDialog.Show("Message", "Reading csv not successful!");
                return Result.Failed;
            }

            // step 2: fragmentation an save small pcd
            string exeGreen3DPath = Constants.exeFragmentationBBox;
            string rcpFilePath = Path.Combine(path, "07_FragmentationBBox\\");
            if (!Directory.Exists(rcpFilePath))
            {
                Directory.CreateDirectory(rcpFilePath);
            }

            string command = $"{set.PathPointCloud} {csvPathBBoxes} {rcpFilePath} {dateBimLastModified}";
            if (!Helper.Fragmentation2Pcd(exeGreen3DPath, command))
            {
                TaskDialog.Show("Message", "Fragmentation not successful!");
                return Result.Failed;
            }

            foreach (Helper.OrientedBoundingBox obox in obboxes)
            {
                try
                {
                    // step 3: CloudCompare PCD --> E57 
                    if (!Helper.Pcd2e57(Path.Combine(rcpFilePath, obox.ObjectGuid + ".pcd"), Path.Combine(rcpFilePath, obox.ObjectGuid + ".e57")))
                    {
                        TaskDialog.Show("Message", "CloudCompare error");
                        return Result.Failed;
                    }

                    // step 4: DeCap E57 --> RCP
                    if(!Helper.DeCap(path, obox.ObjectGuid, Path.Combine(rcpFilePath, obox.ObjectGuid + ".e57")))
                    {
                        TaskDialog.Show("Message", "DeCap error");
                        return Result.Failed;
                    }
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
            }
            TaskDialog.Show("Message", "BBox Fragmentation successful!");
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
        
    }
}