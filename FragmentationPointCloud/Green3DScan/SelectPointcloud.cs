using System;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Serilog;
using Path = System.IO.Path;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class SelectPointcloud : IExternalCommand
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
            Log.Information("start selectPointcloud");
            Log.Information(set.BBox_Buffer.ToString());
            #endregion setup

            // Gesamtpunktwolke
            FileOpenDialog fodPointcloud = new FileOpenDialog("PCD file (*.pcd)|*.pcd");
            fodPointcloud.Title = "Select PCD File with Pointcloud!";
            if (fodPointcloud.Show() == ItemSelectionDialogResult.Canceled)
            {
                return Result.Cancelled;
            }
            string pcdPathPointcloud = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodPointcloud.GetSelectedModelPath());
            try
            {
                var newSettings = SettingsJson.ReadSettingsJson(Constants.pathSettings);
                newSettings.PathPointCloud = pcdPathPointcloud;
                SettingsJson.WriteSettingsJson(newSettings, Constants.pathSettings);
                TaskDialog.Show("Message", "Selection of Pointcloud successful!");
            }
            catch (Exception)
            {
                TaskDialog.Show("Message", "Selection of Pointcloud NOT successful!");
                return Result.Failed;
            }
            return Result.Succeeded;
        }
        #endregion execute
    }
}