using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Serilog;
using Transform = Autodesk.Revit.DB.Transform;
using Path = System.IO.Path;
using Document = Autodesk.Revit.DB.Document;
using Sys = System.Globalization.CultureInfo;
using D3 = GeometryLib.Double.D3;
using Autodesk.Revit.DB.Structure;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class AddStation : IExternalCommand
    {
        string path;
        public const string CsvHeader = "ObjectGuid;ElementId;Rechtswert;Hochwert;Hoehe";
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
               .WriteTo.File(Path.Combine(path, "LogFile_"), rollingInterval: RollingInterval.Minute)
               .CreateLogger();
            Log.Information("start AddStation");
            Log.Information(set.BBox_Buffer.ToString());
           
            Transform trans = Helper.GetTransformation(doc, set, out var crs);

            #endregion setup
            Log.Information("setup");
            #region ScanStation

            if (!File.Exists(Path.Combine(path, "ScanStation.rfa")))
            {
                Helper.CreateSphereFamily(uiapp, set.SphereDiameter_Meter / 2 * Constants.meter2Feet, Path.Combine(path, "ScanStation.rfa"));
            }

            #endregion ScanStation
            Log.Information("ScanStation");
            #region create new stations

            try
            {
                using (TransactionGroup tg = new TransactionGroup(doc, "Place ScanStation"))
                {
                    FamilySymbol familySymbol = null;
                    tg.Start();
                    using (Transaction t = new Transaction(doc, "Load and place ScanStation Family"))
                    {
                        t.Start();
                        Family family;
                        // load family, if not already present in the project
                        if (!doc.LoadFamily(Path.Combine(path, "ScanStation.rfa"), out family))
                        {
                            FilteredElementCollector collector = new FilteredElementCollector(doc);
                            ICollection<Element> familyInstances = collector.OfClass(typeof(Family)).ToElements();
                            foreach (Element element in familyInstances)
                            {
                                Family loadedFamily = element as Family;
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
                        if (familySymbol == null)
                        {
                            Log.Information("Error, no family symbol found.");
                        }

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
                        D3.Vector vector;
                        try
                        {
                            point = uidoc.Selection.PickPoint("Click to place a ScanStation or press ESC to finish");
                            vector = new D3.Vector(point.X, point.Y, point.Z);
                            vector = new D3.Vector(point.X, point.Y, set.HeightOfScanner_Meter * Constants.meter2Feet);
                            var pointPBP =  trans.OfPoint(point) * Constants.feet2Meter;
                        }
                        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                        {
                            break; // exit the loop when ESC is pressed
                        }
                        var listWithPoint = new List<D3.Vector>
                        {
                            vector
                        };

                        using (Transaction tx = new Transaction(doc, "Place ScanStation"))
                        {
                            tx.Start();

                            XYZ position = new XYZ(vector.x, vector.y, vector.z);
                            doc.Create.NewFamilyInstance(position, familySymbol, StructuralType.NonStructural);

                            tx.Commit();
                        }
                    }
                    tg.Assimilate(); // commit the transaction group
                }

                Level currentLevel = doc.ActiveView.GenLevel;
                string levelName = currentLevel.Name;

                var allStations = Helper.CollectFamilyInstances(doc, trans, "ScanStation");
                TaskDialog.Show("Message", allStations.Count.ToString() + " ScanStations");
                #endregion create new stations

                #region write stations to csv

                string csvPath = Path.Combine(path, "07_Stations/");

                if (!Directory.Exists(csvPath))
                {
                    Directory.CreateDirectory(csvPath);
                }

                using StreamWriter csv = File.CreateText(Path.Combine(csvPath, "Stations.csv"));
                csv.WriteLine(CsvHeader);

                foreach (var item in allStations)
                {
                    csv.WriteLine(item.X.ToString(Sys.InvariantCulture) + ";" + item.Y.ToString(Sys.InvariantCulture) + ";" + item.Z.ToString(Sys.InvariantCulture));
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