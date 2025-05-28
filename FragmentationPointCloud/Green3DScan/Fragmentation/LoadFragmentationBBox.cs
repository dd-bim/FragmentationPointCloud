//using System;
//using System.Globalization;
//using System.IO;
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
//    public class LoadFragmentationBBox : IExternalCommand
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
//            if (geomElement is null)
//            {
//                geomElement = null;
//                return false;
//            }

//            // stateId and objectId
//            return true;
//        }

//        private static bool LoadPointCloud(Document doc, string pointCloudPath, Transform trans)
//        {
//            try
//            {
//                var tx = new Transaction(doc, "Load RCP");
//                tx.Start();
//                var type = PointCloudType.Create(doc, "rcp", pointCloudPath);
//                PointCloudInstance.Create(doc, type.Id, trans);
//                tx.Commit();
//                return true;
//            }
//            catch (Exception)
//            {
//                return false;
//            }
//        }

//        #region Execute

//        private string _path;

//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            #region setup

//            // settings json
//            var set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

//            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
//            Document doc = uiDoc.Document;
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
//            Log.Information("start LoadFragmentationBBox");
//            Log.Information(set.BBox_Buffer.ToString(CultureInfo.InvariantCulture));

//            #endregion setup

//            Transform trans = Helper.GetTransformation(doc, set, out CoordinateSystem _);
//            Transform transInverse = trans.Inverse;

//            try
//            {
//                // select only building components
//                var pickedObjects = uiDoc.Selection.PickObjects(ObjectType.Element,
//                    "Select components whose point cloud to be input.");
//                foreach (Reference reference in pickedObjects)
//                {
//                    if (!GetGeometryElement(doc, reference, out GeometryElement geomElement))
//                    {
//                        Log.Information("skipped building component");
//                        continue;
//                    }

//                    Element ele = doc.GetElement(reference.ElementId);

//                    foreach (GeometryObject unused in geomElement)
//                    {
//                        string guid = ele.UniqueId;

//                        try
//                        {
//                            string ifcGuid = Helper.ToIfcGuid(Helper.ToGuid(guid));
//                            string rcpFilePath = Path.Combine(_path, "07_FragmentationBBox\\" + ifcGuid + ".rcp");

//                            // load rcp into revit
//                            if (!LoadPointCloud(doc, rcpFilePath, transInverse))
//                                TaskDialog.Show("Message", "File does not exist!");
//                        }

//                        #region catch

//                        catch (Except.OperationCanceledException)
//                        {
//                            TaskDialog.Show("Message", "Error 1: Command canceled.");
//                            return Result.Failed;
//                        }
//                        catch (Except.ForbiddenForDynamicUpdateException)
//                        {
//                            TaskDialog.Show("Message", "Error 2");
//                            return Result.Failed;
//                        }
//                        catch (Exception ex)
//                        {
//                            message += "Error message::" + ex;
//                            TaskDialog.Show("Message", message);
//                            return Result.Failed;
//                        }

//                        #endregion catch
//                    }
//                }
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

//            TaskDialog.Show("Message", "Loading RCP successful!");
//            return Result.Succeeded;
//        }

//        #endregion execute
//    }
//}