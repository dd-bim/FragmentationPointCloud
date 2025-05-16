using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Serilog;
using System.IO;

namespace Revit.Green3DScan;

public static class ExternalCommandHelper
{
    private const string LogsFolderName = "00_Logs";
    private const string LogFileName = "LogFile_";


    internal static bool GetProjectPath(ExternalCommandData commandData, 
        out string projectPath,
        out Document projectDocument,
        out UIDocument uiDocument)
    {
        uiDocument = commandData.Application.ActiveUIDocument;
        if (!uiDocument.IsValidObject
            || !uiDocument.Document.IsValidObject)
        {
            projectPath = string.Empty;
            projectDocument = null;
            return false;
        }
        projectDocument = uiDocument.Document;
        projectPath = Path.GetDirectoryName(projectDocument.PathName);
        return string.IsNullOrEmpty(projectPath);
    }

    internal static void InitLogger(string projectPath)
    {
        string loggerPath = Path.Combine(projectPath, LogsFolderName);

        if (!Directory.Exists(loggerPath))
            Directory.CreateDirectory(loggerPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(Path.Combine(loggerPath, LogFileName), rollingInterval: RollingInterval.Minute)
            .CreateLogger();
    }

    internal static bool GetFilePathDialog(string title, string filter, out string filePath)
    {
        using var fileOpenDialog = new FileOpenDialog(filter);
        fileOpenDialog.Title = title;
        ModelPath selectedModelPath;
        if (fileOpenDialog.Show() == ItemSelectionDialogResult.Canceled
            || (selectedModelPath = fileOpenDialog.GetSelectedModelPath()) is null)
        {
            filePath = string.Empty;
            return false;
        }
        filePath = ModelPathUtils.ConvertModelPathToUserVisiblePath(selectedModelPath);
        return true;
    }
}