using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Serilog;
using D3 = GeometryLib.Double.D3;
using Transform = Autodesk.Revit.DB.Transform;
using Path = System.IO.Path;
using Document = Autodesk.Revit.DB.Document;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class Raster : IExternalCommand
    {
        string path;
        public const string CsvHeader = "East;North;Elevation";
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
            Log.Information("setup");
            #region stations

            // user clicks to select a point
            XYZ point;
            D3.Vector startStation;

            List<D3.Vector> stations = new List<D3.Vector>();
            List<D3.Vector> stationsPBP = new List<D3.Vector>();

            try
            {
                point = uidoc.Selection.PickPoint("Click to place a ScanStation or press ESC to finish");
                startStation = new D3.Vector(point.X, point.Y, set.HeightOfScanner_Meter * Constants.meter2Feet);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            var gridSpacing = set.GridSpacing_Meter * Constants.meter2Feet;
            
            // calculation of grid
            // columns
            for (int i = 0; i < set.GridColumns; i++)
            {
                // rows
                for (int j = 0; j < set.GridRows; j++)
                {
                    double x = startStation.x + (i * gridSpacing);
                    double y = startStation.y + (j * gridSpacing);
                    double z = startStation.z;

                    stations.Add(new D3.Vector(x, y, z));
                }
            }

            Helper.LoadAndPlaceSphereFamily(doc, Path.Combine(path, "ScanStation.rfa"), stations);

            #endregion stations
            TaskDialog.Show("Message", "Creation of the grid completed!");
            return Result.Succeeded;
        }
    }
}