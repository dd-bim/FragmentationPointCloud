﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Except = Autodesk.Revit.Exceptions;
using Serilog;
using Transform = Autodesk.Revit.DB.Transform;
using Path = System.IO.Path;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class LoadFragmentationVoxel : IExternalCommand
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
            Log.Information("start LoadFragmentationVoxel");
            Log.Information(set.BBox_Buffer.ToString());
            #endregion setup

            Transform trans = Helper.GetTransformation(doc, set, out var crs);
            Transform transInverse = trans.Inverse;

            // indices
            FileOpenDialog fodBBox = new FileOpenDialog("CSV file (*.csv)|*.csv");
            fodBBox.Title = "Select BBox_Voxel_Indices!";
            if (fodBBox.Show() == ItemSelectionDialogResult.Canceled)
            {
                return Result.Cancelled;
            }
            string csvPathIndices = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodBBox.GetSelectedModelPath());

            ReadCsvIndices(csvPathIndices, out var dicIndices);

            // select only building components
            IList<Reference> pickedObjects = uidoc.Selection.PickObjects(ObjectType.Element, "Select components whose pointcloud to be input.");
            foreach (Reference reference in pickedObjects)
            {
                if (!GetGeometryElement(doc, reference, out GeometryElement geomElement, out string createStateId, out string demolishedStateId, out string objectId, out Category cat))
                {
                    Log.Information("skipped building component");
                    continue;
                }
                Element ele = doc.GetElement(reference.ElementId);

                foreach (GeometryObject geomObj in geomElement)
                {
                    string guid = ele.UniqueId;
                    try
                    {
                        var ifcGuid = Helper.ToIfcGuid(Helper.ToGuid(guid));

                        // search index and load point cloud
                        var listIndices = dicIndices[ifcGuid];
                        foreach (var item in listIndices)
                        {
                            string rcpFilePath = Path.Combine(path, "08_FragmentationVoxel\\voxel_" + item + ".rcp");
                            // load rcp into Revit
                            if (!LoadPointCloud(doc, rcpFilePath, transInverse))
                            {
                                TaskDialog.Show("Message", "File does not exist!");
                            }
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
            }
            TaskDialog.Show("Message", "Loading rcp successful!");
            return Result.Succeeded;
        }
        #endregion execute

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
            createStateId = ele.CreatedPhaseId.ToString();
            demolishedStateId = ele.DemolishedPhaseId.ToString();
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
        private bool ReadCsvIndices(string csvPathIndices, out Dictionary<string,List<string>> dicIndices)
        {
            Dictionary<string, List<string>> dic = new Dictionary<string, List<string>>();
            try
            {
                using (StreamReader reader = new StreamReader(csvPathIndices))
                {
                    reader.ReadLine();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] columns = line.Split(';');
                        if (columns.Length == 2)
                        {
                            string[] indices = columns[1].Split(','); 
                            dic.Add(columns[0], indices.ToList());
                        }
                        else
                        {
                            TaskDialog.Show("Message", "Incorrect line: " + line);
                        }
                    }
                }
                dicIndices = dic;
                return true;
            }
            catch (Exception)
            {
                dicIndices = dic;
                return false;
            }
        }
    }
}