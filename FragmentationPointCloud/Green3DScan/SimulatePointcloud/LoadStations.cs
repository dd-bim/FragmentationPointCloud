using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using JetBrains.Annotations;

using Serilog;

using System.IO;

namespace Revit.Green3DScan.SimulatePointCloud;

[Transaction(TransactionMode.Manual)]
[UsedImplicitly]
public class LoadStations : IExternalCommand
{
    public const string CsvHeader = "East;North;Elevation";
    private const int LineCount = 3;

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // Initialization
        if (!ExternalCommandHelper.GetProjectPath(commandData, out string projectPath, out var document, out var uiDocument))
        {
            TaskDialog.Show("Message", "The project file has not been saved yet.");
            return Result.Failed;
        }
        ExternalCommandHelper.InitLogger(projectPath);
        var settings = SettingsJson.ReadSettingsJson(Constants.pathSettings);

        Log.Information("start LoadStations");
        Log.Information(settings.BBox_Buffer.ToString());

        // Get transformation
        var transform = Helper.GetTransformation(document!, settings);

        Log.Information("setup");


        if (!Stations.TryReadCsv(transform, out var allStations))
        {
            TaskDialog.Show("Error", "Failed to read stations CSV. Please check the log for details.");
            return Result.Failed;
        }


        Log.Information("read files");

        #region ScanStation

        Stations.EnsureScanStationFamily(uiDocument.Application, projectPath, settings);

        if (!Stations.TryLoadAndPlaceSphereFamily(document!, projectPath, allStations))
        {
            Log.Error("Error loading and placing ScanStation family.");
            TaskDialog.Show("Error", "Failed to load or place ScanStation family. Please check the log for details.");
            return Result.Failed;
        }

        #endregion ScanStation

        TaskDialog.Show("Message", allStations.Count + " ScanStations");
        Log.Information("end LoadStations");
        return Result.Succeeded;
    }


}