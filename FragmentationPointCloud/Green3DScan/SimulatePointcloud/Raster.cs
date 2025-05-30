using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JetBrains.Annotations;
using Serilog;
//using CoordinateSystem = GeometryLib.D3.CoordinateSystem;
//using Path = System.IO.Path;
//using Vector = GeometryLib.D3.Vector;

namespace Revit.Green3DScan.SimulatePointCloud;

// TODO: Combine code with Bim2Stations.cs to avoid duplication of logic
[Transaction(TransactionMode.Manual)]
[UsedImplicitly]
public class Raster : IExternalCommand
{
    //public const string VisibleFacesFileName = "Revit2StationsVisibleFaces.csv";
    //public const string VisibleFacesRefFileName = "Revit2StationsVisibleFacesRef.csv";

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
        Log.Information("start Raster");

        // Get transformation
        var transform = Helper.GetTransformation(document, settings);

        Log.Information("setup");

        #region ScanStation

        Stations.EnsureScanStationFamily(commandData.Application, projectPath, settings);

        #endregion ScanStation

        Log.Information("ScanStation");

        #region stations

        // user clicks to select a point
        XYZ point;
        XYZ startStation;

        var stations = new List<XYZ>();

        try
        {
            point = uiDocument.Selection.PickPoint("Click to place a ScanStation or press ESC to finish");
            startStation = new XYZ(point.X, point.Y, settings.HeightOfScanner_Meter * Constants.meter2Feet);
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }

        double gridSpacing = settings.GridSpacing_Meter * Constants.meter2Feet;

        // calculation of grid
        // columns
        for (int i = 0; i < settings.GridColumns; i++)
            // rows
        for (int j = 0; j < settings.GridRows; j++)
        {
            double x = startStation.X + i * gridSpacing;
            double y = startStation.Y + j * gridSpacing;
            double z = startStation.Z;

            stations.Add(new XYZ(x, y, z));
        }

        if (!Stations.TryLoadAndPlaceSphereFamily(document, projectPath, stations))
        {
            Log.Error("Error loading and placing ScanStation family.");
            TaskDialog.Show("Error", "Failed to load or place ScanStation family. Please check the log for details.");
            return Result.Failed;
        }

        #endregion stations

        TaskDialog.Show("Message", "Creation of the grid completed!");
        return Result.Succeeded;
    }
}