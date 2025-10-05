using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

using JetBrains.Annotations;

using Serilog;

using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace Revit.Green3DScan.SimulatePointCloud;

[Transaction(TransactionMode.Manual)]
[UsedImplicitly]
public class AddStation : IExternalCommand
{
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
        Log.Information("start AddStation");
        Log.Information(settings.BBox_Buffer.ToString());

        // Get transformation
        var transform = Helper.GetTransformation(document!, settings);

        Log.Information("setup");

        Stations.EnsureScanStationFamily(commandData.Application, projectPath, settings);

        Log.Information("ScanStation");

        #region create new stations

        using (var tg = new TransactionGroup(document, "Place ScanStation"))
        {
            tg.Start();

            Stations.EnsureScanStationFamily(uiDocument.Application, projectPath, settings);

            if (!Stations.TryLoadSphereFamily(document!, projectPath, out var familySymbol))
            {
                Log.Error("Error loading ScanStation family.");
                TaskDialog.Show("Error", "Failed to load ScanStation family. Please check the log for details.");
                tg.RollBack();
                return Result.Failed;
            }

            while (true)
            {
                try
                {
                    // Benutzer wählt einen Punkt
                    var point = uiDocument.Selection.PickPoint("Click to place a ScanStation or press ESC to finish");
                    // Höhe auf Scannerhöhe setzen
                    var position = new XYZ(point.X, point.Y, point.Z + (settings.HeightOfScanner_Meter * Constants.meter2Feet));

                    using var tx = new Transaction(document, "Place ScanStation");
                    tx.Start();
                    document!.Create.NewFamilyInstance(position, familySymbol, StructuralType.NonStructural);
                    tx.Commit();
                }
                catch (OperationCanceledException)
                {
                    break; // ESC beendet die Platzierung
                }
            }
            tg.Assimilate(); // commit the transaction group
        }

        #endregion create new stations

 
        Log.Information("end AddStation");
        return Result.Succeeded;
    }
}