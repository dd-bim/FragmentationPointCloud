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

namespace Revit.Green3DScan.SimulatePointCloud
{
    [Transaction(TransactionMode.Manual)]
    [UsedImplicitly]
    public class Raster : IExternalCommand
    {
        public const string CsvHeader = "East;North;Elevation";
        private string path;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region setup

            // settings json
            var set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            UIApplication uiapp = commandData.Application;
            try
            {
                path = Path.GetDirectoryName(doc.PathName);
                var fileInfo = new FileInfo(path);
                DateTime date = fileInfo.LastWriteTime;
            }
            catch (Exception)
            {
                TaskDialog.Show("Message", "The file has not been saved yet.");
                return Result.Failed;
            }

            // logger
            string logsPath = Path.Combine(path, "00_Logs/");
            if (!Directory.Exists(logsPath)) Directory.CreateDirectory(logsPath);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(logsPath, "LogFile_"), rollingInterval: RollingInterval.Minute)
                .CreateLogger();
            Log.Information("start Raster");

            Transform trans = Helper.GetTransformation(doc, set, out CoordinateSystem crs);

            string csvVisibleFaces = Path.Combine(path, "Revit2StationsVisibleFaces.csv");
            string csvVisibleFacesRef = Path.Combine(path, "Revit2StationsVisibleFacesRef.csv");

            #endregion setup

            Log.Information("setup");

            #region ScanStation

            if (!File.Exists(Path.Combine(path, "ScanStation.rfa")))
                Helper.CreateSphereFamily(uiapp, set.SphereDiameter_Meter / 2 * Constants.meter2Feet,
                    Path.Combine(path, "ScanStation.rfa"));

            #endregion ScanStation

            Log.Information("ScanStation");

            #region stations

            // user clicks to select a point
            XYZ point;
            Vector startStation;

            var stations = new List<Vector>();
            var stationsPBP = new List<Vector>();

            try
            {
                point = uidoc.Selection.PickPoint("Click to place a ScanStation or press ESC to finish");
                startStation = new Vector(point.X, point.Y, set.HeightOfScanner_Meter * Constants.meter2Feet);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            double gridSpacing = set.GridSpacing_Meter * Constants.meter2Feet;

            // calculation of grid
            // columns
            for (var i = 0; i < set.GridColumns; i++)
                // rows
            for (var j = 0; j < set.GridRows; j++)
            {
                double x = startStation.x + i * gridSpacing;
                double y = startStation.y + j * gridSpacing;
                double z = startStation.z;

                stations.Add(new Vector(x, y, z));
            }

            Helper.LoadAndPlaceSphereFamily(doc, Path.Combine(path, "ScanStation.rfa"), stations);

            #endregion stations

            TaskDialog.Show("Message", "Creation of the grid completed!");
            return Result.Succeeded;
        }
    }
}