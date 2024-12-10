using System;
using System.IO;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Except = Autodesk.Revit.Exceptions;
using Serilog;
using Path = System.IO.Path;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class Pcd2E57 : IExternalCommand
    {
        string path;
        #region Execute
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

            // path to pcd
            FileOpenDialog fod = new FileOpenDialog("PCD file (*.pcd)|*.pcd");
            fod.Title = "Select PCD!";
            if (fod.Show() == ItemSelectionDialogResult.Canceled)
            {
                return Result.Cancelled;
            }
            string pcdFilePath = ModelPathUtils.ConvertModelPathToUserVisiblePath(fod.GetSelectedModelPath());
            string parentDirectory = Path.GetDirectoryName(pcdFilePath);
            string fileName = Path.GetFileNameWithoutExtension(pcdFilePath) + ".e57";

            // path to CloudCompare.exe
            string cloudComparePath = set.PathCloudCompare;

            // path to E57
            string e57FilePath = Path.Combine(parentDirectory, fileName);
   
            Process cloudCompareProcess = new Process();

            // Configure the process object with the required arguments
            cloudCompareProcess.StartInfo.FileName = cloudComparePath;
            cloudCompareProcess.StartInfo.Arguments = "-O \"" + pcdFilePath + "\" -C_EXPORT_FMT E57 -SAVE_CLOUDS FILE \"" + e57FilePath + "\"";

            cloudCompareProcess.Start();

            cloudCompareProcess.WaitForExit();

            try
            {
                TaskDialog.Show("Message", "finish");
                return Result.Succeeded;
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
        #endregion execute
    }
}