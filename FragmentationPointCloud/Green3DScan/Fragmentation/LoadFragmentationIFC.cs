using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Except = Autodesk.Revit.Exceptions;
using Serilog;
using Transform = Autodesk.Revit.DB.Transform;
using Path = System.IO.Path;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class LoadFragmentationIFC : IExternalCommand
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
            string logsPath = Path.Combine(path, "00_Logs/");
            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File(Path.Combine(logsPath, "LogFile_"), rollingInterval: RollingInterval.Minute)
               .CreateLogger();
            Log.Information("start LoadFragmentationIFC");
            Log.Information(set.BBox_Buffer.ToString());
            #endregion setup

            // bboxes
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
                TaskDialog.Show("Message", "Reading csv successful!");
                return Result.Failed;
            }

            Transform trans = Helper.GetTransformation(doc, set, out var crs);
            Transform transInverse = trans.Inverse;

            int fail = 0;
            foreach (Helper.OrientedBoundingBox box in obboxes)
            {
                try
                {
                    string rcpFilePath = Path.Combine(path, "07_FragmentationBBox\\" + box.ObjectGuid + ".rcp");

                    // load rcp
                    if (!LoadPointCloud(doc, rcpFilePath, transInverse))
                    {
                        Log.Information("Fragment not existant!");
                        fail++;
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

            TaskDialog.Show("Message", "Loading rcp successful! " + fail + " Fragments not existants!");
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
                            TaskDialog.Show("Message", "Incorrect line " + line);
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
        /// <summary>
        /// GeometryElement with their StateId and ObjectId is taken from the reference
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="reference"></param>
        /// <param name="geomElement"></param>
        /// <param name="createStateId"></param>
        /// <param name="demolishedStateId"></param>
        /// <param name="objectId"></param>
        /// <param name="cat"></param>
        /// <returns></returns>
        private static bool GetGeometryElement(Document doc, Reference reference, out GeometryElement geomElement, out string createStateId, out string demolishedStateId,
            out string objectId, out Category cat)
        {
            Element ele = doc.GetElement(reference.ElementId);
            Options options = new Options
            {
                ComputeReferences = true
            };

            // category
            cat = ele.Category;

            geomElement = ele.get_Geometry(options);
            if (geomElement is null)
            {
                geomElement = default;
                createStateId = default;
                demolishedStateId = default;
                objectId = default;
                return false;
            }
            // stateId and objectId
            createStateId = ele.CreatedPhaseId.IntegerValue.ToString();
            demolishedStateId = ele.DemolishedPhaseId.IntegerValue.ToString();
            objectId = ele.UniqueId;
            return true;
        }
        private bool LoadPointCloud(Document doc, string path, Transform trans)
        {
            try
            {
                Transaction tx = new Transaction(doc, "Load RCP");
                tx.Start();
                PointCloudType type = PointCloudType.Create(doc, "rcp", path);
                PointCloudInstance.Create(doc, type.Id, trans);
                tx.Commit();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}