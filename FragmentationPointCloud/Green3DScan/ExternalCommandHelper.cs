using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Serilog;

using System.IO;

namespace Revit.Green3DScan;

/// <summary>
/// Provides utility methods for working with external commands in a Revit environment.
/// </summary>
/// <remarks>This class includes helper methods for retrieving project paths, initializing logging,  and
/// displaying file selection dialogs. It is designed to support external command workflows  and streamline common
/// operations in Revit add-ins.</remarks>
internal static class ExternalCommandHelper
{
    private const string LogsFolderName = "00_Logs";
    private const string LogFileName = "LogFile_";

    /// <summary>
    /// Attempts to retrieve the file path of the active project in the Revit application.
    /// </summary>
    /// <remarks>This method checks the validity of the active UI document and its associated document.  If
    /// either is invalid, the method returns <see langword="false"/> and outputs default values  for <paramref
    /// name="projectPath"/>, <paramref name="projectDocument"/>, and <paramref name="uiDocument"/>.</remarks>
    /// <param name="commandData">The external command data containing application and document context.</param>
    /// <param name="projectPath">When this method returns, contains the directory path of the active project file,  or an empty string if the
    /// project path is unavailable.</param>
    /// <param name="projectDocument">When this method returns, contains the active <see cref="Document"/> object,  or <see langword="null"/> if no
    /// valid document is available.</param>
    /// <param name="uiDocument">When this method returns, contains the active <see cref="UIDocument"/> object,  or <see langword="null"/> if no
    /// valid UI document is available.</param>
    /// <returns><see langword="true"/> if the project path is successfully retrieved; otherwise, <see langword="false"/>.</returns>
    internal static bool GetProjectPath(ExternalCommandData commandData,
        out string projectPath,
        out Document? projectDocument,
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
        projectPath = Path.GetDirectoryName(projectDocument.PathName) ?? string.Empty;
        return !string.IsNullOrEmpty(projectPath);
    }

    /// <summary>
    /// Initializes the logger for the application with a file-based logging configuration.
    /// </summary>
    /// <remarks>This method sets up a logger that writes log messages to a file in a subdirectory named after
    /// the logs folder. If the specified directory for logs does not exist, it will be created automatically. The log
    /// files are configured to roll over at one-minute intervals.</remarks>
    /// <param name="projectPath">The root directory of the project where the log files will be stored. Must not be null or empty.</param>
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

    /// <summary>
    /// Displays a file selection dialog to the user and retrieves the selected file path.
    /// </summary>
    /// <remarks>This method uses a file selection dialog to allow the user to choose a file. If the dialog is
    /// canceled or no file is selected, the <paramref name="filePath"/> parameter will be set to an empty string, and
    /// the method will return <see langword="false"/>.</remarks>
    /// <param name="title">The title to display on the file selection dialog.</param>
    /// <param name="filter">The file filter to apply in the dialog, specifying the types of files that can be selected.</param>
    /// <param name="filePath">When this method returns, contains the full path of the selected file if a file was selected; otherwise, an
    /// empty string.</param>
    /// <returns><see langword="true"/> if the user selected a file; otherwise, <see langword="false"/> if the dialog was
    /// canceled or no file was selected.</returns>
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