using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using JetBrains.Annotations;
using Serilog;
//using CoordinateSystem = GeometryLib.D3.CoordinateSystem;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;
//using Path = System.IO.Path;
//using Sys = System.Globalization.CultureInfo;
//using Vector = GeometryLib.D3.Vector;

namespace Revit.Green3DScan.SimulatePointCloud
{
    [Transaction(TransactionMode.Manual)]
    [UsedImplicitly]
    public class AddStation : IExternalCommand
    {
        private const string CsvHeader = "East;North;Elevation";
        private string _path;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region setup

            // settings json
            var set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

            var uiDocument = commandData.Application.ActiveUIDocument;
            var doc = uiDocument.Document;
            var uiApplication = commandData.Application;
            try
            {
                _path = Path.GetDirectoryName(doc.PathName);
                var fileInfo = new FileInfo(_path);
                DateTime date = fileInfo.LastWriteTime;
            }
            catch (Exception)
            {
                TaskDialog.Show("Message", "The file has not been saved yet.");
                return Result.Failed;
            }

            // logger
            string logsPath = Path.Combine(_path, "00_Logs/");
            if (!Directory.Exists(logsPath)) Directory.CreateDirectory(logsPath);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(logsPath, "LogFile_"), rollingInterval: RollingInterval.Minute)
                .CreateLogger();
            Log.Information("start AddStation");
            Log.Information(set.BBox_Buffer.ToString());

            Transform trans = Helper.GetTransformation(doc, set, out CoordinateSystem crs);

            #endregion setup

            Log.Information("setup");

            #region ScanStation

            if (!File.Exists(Path.Combine(_path, "ScanStation.rfa")))
                Helper.CreateSphereFamily(uiApplication, set.SphereDiameter_Meter / 2 * Constants.meter2Feet,
                    Path.Combine(_path, "ScanStation.rfa"));

            #endregion ScanStation

            Log.Information("ScanStation");

            #region create new stations

            try
            {
                using (var tg = new TransactionGroup(doc, "Place ScanStation"))
                {
                    FamilySymbol familySymbol = null;
                    tg.Start();
                    using (var t = new Transaction(doc, "Load and place ScanStation Family"))
                    {
                        t.Start();
                        Family family;
                        // load family, if not already present in the project
                        if (!doc.LoadFamily(Path.Combine(_path, "ScanStation.rfa"), out family))
                        {
                            var collector = new FilteredElementCollector(doc);
                            ICollection<Element> familyInstances = collector.OfClass(typeof(Family)).ToElements();
                            foreach (Element element in familyInstances)
                            {
                                var loadedFamily = element as Family;
                                if (loadedFamily.Name == "ScanStation")
                                {
                                    family = loadedFamily;
                                    break;
                                }
                            }
                        }

                        foreach (ElementId id in family.GetFamilySymbolIds())
                        {
                            familySymbol = doc.GetElement(id) as FamilySymbol;
                            break;
                        }

                        if (familySymbol == null) Log.Information("Error, no family symbol found.");

                        if (!familySymbol.IsActive)
                        {
                            familySymbol.Activate();
                            doc.Regenerate();
                        }

                        t.Commit();
                    }

                    while (true)
                    {
                        // user clicks to select a point
                        XYZ point;
                        Vector vector;
                        try
                        {
                            point = uiDocument.Selection.PickPoint("Click to place a ScanStation or press ESC to finish");
                            vector = new Vector(point.X, point.Y, point.Z);
                            vector = new Vector(point.X, point.Y, set.HeightOfScanner_Meter * Constants.meter2Feet);
                            XYZ pointPBP = trans.OfPoint(point) * Constants.feet2Meter;
                        }
                        catch (OperationCanceledException)
                        {
                            break; // exit the loop when ESC is pressed
                        }

                        var listWithPoint = new List<Vector>
                        {
                            vector
                        };

                        using (var tx = new Transaction(doc, "Place ScanStation"))
                        {
                            tx.Start();

                            var position = new XYZ(vector.x, vector.y, vector.z);
                            doc.Create.NewFamilyInstance(position, familySymbol, StructuralType.NonStructural);

                            tx.Commit();
                        }
                    }

                    tg.Assimilate(); // commit the transaction group
                }

                Level currentLevel = doc.ActiveView.GenLevel;
                string levelName = currentLevel.Name;

                var allStations = Helper.CollectFamilyInstances(doc, trans, "ScanStation");
                TaskDialog.Show("Message", allStations.Count + " ScanStations");

                #endregion create new stations

                #region write stations to csv

                string csvPath = Path.Combine(_path, "07_Stations/");

                if (!Directory.Exists(csvPath)) Directory.CreateDirectory(csvPath);

                using StreamWriter csv = File.CreateText(Path.Combine(csvPath, "Stations.csv"));
                csv.WriteLine(CsvHeader);

                foreach (XYZ item in allStations)
                {
                    csv.WriteLine(item.X.ToString(Sys.InvariantCulture) + ";" + item.Y.ToString(Sys.InvariantCulture) +
                                  ";" + item.Z.ToString(Sys.InvariantCulture));
                }

                csv.Close();

                #endregion write stations to csv
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            Log.Information("end AddStation");
            return Result.Succeeded;
        }
    }
}