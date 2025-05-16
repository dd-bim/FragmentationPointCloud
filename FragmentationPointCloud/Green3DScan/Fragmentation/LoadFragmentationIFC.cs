using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GeometryLib.D3;
using Serilog;
using Except = Autodesk.Revit.Exceptions;
using Transform = Autodesk.Revit.DB.Transform;
using Path = System.IO.Path;

namespace Revit.Green3DScan.Fragmentation
{
    [Transaction(TransactionMode.Manual)]
    public class LoadFragmentationIFC : IExternalCommand
    {
        private bool ReadCsvBoxes(string csvPathBBoxes, out List<Helper.OrientedBoundingBox> listOBBox)
        {
            var list = new List<Helper.OrientedBoundingBox>();
            try
            {
                using (var reader = new StreamReader(csvPathBBoxes))
                {
                    reader.ReadLine();
                    while (reader.ReadLine() is { } line)
                    {
                        string[] columns = line.Split(';');

                        if (columns.Length == 25)
                            list.Add(new Helper.OrientedBoundingBox(bool.Parse(columns[0]), columns[1], columns[2],
                                columns[3],
                                new XYZ(double.Parse(columns[4]), double.Parse(columns[5]), double.Parse(columns[6])),
                                new XYZ(double.Parse(columns[7]), double.Parse(columns[8]), double.Parse(columns[9])),
                                new XYZ(double.Parse(columns[10]), double.Parse(columns[11]),
                                    double.Parse(columns[12])),
                                new XYZ(double.Parse(columns[13]), double.Parse(columns[14]),
                                    double.Parse(columns[15])),
                                double.Parse(columns[16]), double.Parse(columns[17]), double.Parse(columns[18])));
                        else
                            TaskDialog.Show("Message", "Incorrect line " + line);
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

        private bool LoadPointCloud(Document doc, string path, Transform trans)
        {
            try
            {
                var tx = new Transaction(doc, "Load RCP");
                tx.Start();
                var type = PointCloudType.Create(doc, "rcp", path);
                PointCloudInstance.Create(doc, type.Id, trans);
                tx.Commit();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region Execute

        private string _path;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region setup

            // settings json
            var set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

            UIDocument activeUiDocument = commandData.Application.ActiveUIDocument;
            Document doc = activeUiDocument.Document;
            try
            {
                _path = Path.GetDirectoryName(doc.PathName);
            }
            catch (Exception)
            {
                TaskDialog.Show("Message", "The file has not been saved yet.");
                return Result.Failed;
            }

            // logger
            string logsPath = Path.Combine(_path!, "00_Logs/");
            if (!Directory.Exists(logsPath)) Directory.CreateDirectory(logsPath);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(logsPath, "LogFile_"), rollingInterval: RollingInterval.Minute)
                .CreateLogger();
            Log.Information("start LoadFragmentationIFC");
            Log.Information(set.BBox_Buffer.ToString(CultureInfo.InvariantCulture));

            #endregion setup

            // bboxes
            var fodBBox = new FileOpenDialog("CSV file (*.csv)|*.csv");
            fodBBox.Title = "Select CSV file with BBoxes from Revit!";
            if (fodBBox.Show() == ItemSelectionDialogResult.Canceled) return Result.Cancelled;
            string csvPathBBoxes = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodBBox.GetSelectedModelPath());

            // read csv
            if (!ReadCsvBoxes(csvPathBBoxes, out var obboxes))
            {
                TaskDialog.Show("Message", "Reading csv successful!");
                return Result.Failed;
            }

            Transform trans = Helper.GetTransformation(doc, set, out CoordinateSystem _);
            Transform transInverse = trans.Inverse;

            int fail = 0;
            foreach (Helper.OrientedBoundingBox box in obboxes)
            {
                try
                {
                    string rcpFilePath = Path.Combine(_path, "07_FragmentationBBox\\" + box.ObjectGuid + ".rcp");

                    // load rcp
                    if (LoadPointCloud(doc, rcpFilePath, transInverse)) continue;
                    Log.Information("Fragment not existent!");
                    fail++;
                    return Result.Failed;
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
                    message += "Error message::" + ex;
                    TaskDialog.Show("Message", message);
                    return Result.Failed;
                }

                #endregion catch
            }

            TaskDialog.Show("Message", "Loading rcp successful! " + fail + " Fragments not existants!");
            return Result.Succeeded;
        }

        #endregion execute
    }
}