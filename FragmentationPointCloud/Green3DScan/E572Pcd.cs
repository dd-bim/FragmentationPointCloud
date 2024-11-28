using System;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Except = Autodesk.Revit.Exceptions;
using Path = System.IO.Path;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class E572Pcd : IExternalCommand
    {
        #region Execute
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            SettingsJson set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

            // path to e57
            FileOpenDialog fod = new FileOpenDialog("E57 file (*.e57)|*.e57");
            fod.Title = "Select E57!";
            if (fod.Show() == ItemSelectionDialogResult.Canceled)
            {
                return Result.Cancelled;
            }
            string e57FilePath = ModelPathUtils.ConvertModelPathToUserVisiblePath(fod.GetSelectedModelPath());
            string parentDirectory = Path.GetDirectoryName(e57FilePath);
            string fileName = Path.GetFileNameWithoutExtension(e57FilePath) + ".pcd";

            // path to PCD
            string pcdFilePath = Path.Combine(parentDirectory, fileName);
   
            Process cloudCompareProcess = new Process();

            // Configure the process object with the required arguments
            cloudCompareProcess.StartInfo.FileName = set.PathCloudCompare;
            cloudCompareProcess.StartInfo.Arguments = "-O \"" + e57FilePath + "\" -C_EXPORT_FMT PCD -SAVE_CLOUDS FILE \"" + pcdFilePath + "\"";

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