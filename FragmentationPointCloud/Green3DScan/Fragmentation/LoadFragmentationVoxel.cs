//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.UI.Selection;
//using GeometryLib.D3;
//using Serilog;
//using Except = Autodesk.Revit.Exceptions;
//using Transform = Autodesk.Revit.DB.Transform;
//using Path = System.IO.Path;

//namespace Revit.Green3DScan.Fragmentation
//{
//    [Transaction(TransactionMode.Manual)]
//    public class LoadFragmentationVoxel : IExternalCommand
//    {
//        /// <summary>
//        ///     GeometryElement with their StateId and ObjectId is taken from the reference
//        /// </summary>
//        /// <param name="doc"></param>
//        /// <param name="reference"></param>
//        /// <param name="geomElement"></param>
//        /// <returns></returns>
//        private static bool GetGeometryElement(Document doc, Reference reference, out GeometryElement geomElement)
//        {
//            Element ele = doc.GetElement(reference.ElementId);
//            var options = new Options
//            {
//                ComputeReferences = true
//            };

//            // category

//            geomElement = ele.get_Geometry(options);
//            return geomElement is not null;
//        }

//        private static bool LoadPointCloud(Document doc, string path, Transform trans)
//        {
//            try
//            {
//                var tx = new Transaction(doc, "Load RCP");
//                tx.Start();
//                var type = PointCloudType.Create(doc, "rcp", path);
//                PointCloudInstance.Create(doc, type.Id, trans);
//                tx.Commit();
//                return true;
//            }
//            catch (Exception)
//            {
//                return false;
//            }
//        }

//        private static void ReadCsvIndices(string csvPathIndices, out Dictionary<string, List<string>> dicIndices)
//        {
//            var dic = new Dictionary<string, List<string>>();
//            try
//            {
//                using (var reader = new StreamReader(csvPathIndices))
//                {
//                    reader.ReadLine();
//                    while (reader.ReadLine() is { } line)
//                    {
//                        string[] columns = line.Split(';');
//                        if (columns.Length == 2)
//                        {
//                            string[] indices = columns[1].Split(',');
//                            dic.Add(columns[0], indices.ToList());
//                        }
//                        else
//                        {
//                            TaskDialog.Show("Message", "Incorrect line: " + line);
//                        }
//                    }
//                }

//                dicIndices = dic;
//            }
//            catch (Exception)
//            {
//                dicIndices = dic;
//            }
//        }

//        #region Execute

//        private string _path;

//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            #region setup

//            // settings json
//            var set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

//            UIDocument activeUiDocument = commandData.Application.ActiveUIDocument;
//            Document doc = activeUiDocument.Document;
//            try
//            {
//                _path = Path.GetDirectoryName(doc.PathName);
//            }
//            catch (Exception)
//            {
//                TaskDialog.Show("Message", "The file has not been saved yet.");
//                return Result.Failed;
//            }

//            // logger
//            string logsPath = Path.Combine(_path!, "00_Logs/");
//            if (!Directory.Exists(logsPath)) Directory.CreateDirectory(logsPath);
//            Log.Logger = new LoggerConfiguration()
//                .MinimumLevel.Debug()
//                .WriteTo.File(Path.Combine(logsPath, "LogFile_"), rollingInterval: RollingInterval.Minute)
//                .CreateLogger();
//            Log.Information("start LoadFragmentationVoxel");
//            Log.Information(set.BBox_Buffer.ToString(CultureInfo.InvariantCulture));

//            #endregion setup

//            Transform trans = Helper.GetTransformation(doc, set, out CoordinateSystem _);
//            Transform transInverse = trans.Inverse;

//            // indices
//            var fodBBox = new FileOpenDialog("CSV file (*.csv)|*.csv");
//            fodBBox.Title = "Select BBox_Voxel_Indices!";
//            if (fodBBox.Show() == ItemSelectionDialogResult.Canceled) return Result.Cancelled;
//            string csvPathIndices = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodBBox.GetSelectedModelPath());

//            ReadCsvIndices(csvPathIndices, out var dicIndices);

//            // select only building components
//            var pickedObjects =
//                activeUiDocument.Selection.PickObjects(ObjectType.Element, "Select components whose point cloud to be input.");
//            foreach (Reference reference in pickedObjects)
//            {
//                if (!GetGeometryElement(doc, reference, out GeometryElement geomElement))
//                {
//                    Log.Information("skipped building component");
//                    continue;
//                }

//                Element ele = doc.GetElement(reference.ElementId);

//                foreach (GeometryObject unused in geomElement)
//                {
//                    string guid = ele.UniqueId;
//                    try
//                    {
//                        string ifcGuid = Helper.ToIfcGuid(Helper.ToGuid(guid));

//                        // search index and load point cloud
//                        var listIndices = dicIndices[ifcGuid];
//                        foreach (string item in listIndices)
//                        {
//                            string rcpFilePath = Path.Combine(_path, "08_FragmentationVoxel\\voxel_" + item + ".rcp");
//                            // load rcp into Revit
//                            if (!LoadPointCloud(doc, rcpFilePath, transInverse))
//                                TaskDialog.Show("Message", "File does not exist!");
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
//            }

//            TaskDialog.Show("Message", "Loading rcp successful!");
//            return Result.Succeeded;
//        }

//        #endregion execute
//    }
//}