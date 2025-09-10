using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using JetBrains.Annotations;

using Serilog;

namespace Revit.Green3DScan.SimulatePointCloud;

[Transaction(TransactionMode.Manual)]
[UsedImplicitly]
public class SaveStations : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // Initialization
        if (!ExternalCommandHelper.GetProjectPath(commandData, out string projectPath, out var document, out _))
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

        #region select files

        var allStations = Stations.CollectFromFamilyInstances(document!, transform);
 
        #endregion select files

        #region write stations to csv

        if (!Stations.WriteCsv(projectPath, allStations))
        {
            Log.Error("Error writing stations to CSV");
            TaskDialog.Show("Error", "Failed to write stations to CSV. Please check the log for details.");
            return Result.Failed;
        }

        #endregion write stations to csv


        TaskDialog.Show("Message", allStations.Count + " ScanStations");
        Log.Information("end SaveStations");
        return Result.Succeeded;
    }


}