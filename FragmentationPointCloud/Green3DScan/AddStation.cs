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
using Line = Autodesk.Revit.DB.Line;
using YamlDotNet.Core.Tokens;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class AddStation : IExternalCommand
    {
        string path;
        public const string CsvHeader = "ObjectGuid;ElementId;Rchtswert;Hochwert;Hoehe";
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region setup
            // settings json
            SettingsJson set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
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
            Log.Information("start");
            Log.Information(set.BBox_Buffer.ToString());
            #endregion setup

            Transform trans = Helper.GetTransformation(doc, set, out var crs);
            
            XYZ transformedHeight = trans.Inverse.OfPoint(new XYZ(0, 0, set.HeightOfSphere_Meter)) * Constants.meter2Feet; 

            double radius = set.SphereDiameter_Meter/2 * Constants.meter2Feet;


            var fodPfRevit = new FileOpenDialog("CSV file (*.csv)|*.csv");
            fodPfRevit.Title = "Select CSV file with stations from Revit!";
            if (fodPfRevit.Show() == ItemSelectionDialogResult.Canceled)
            {
                return Result.Cancelled;
            }
            var csvPathStations = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodPfRevit.GetSelectedModelPath());

            string[] lines = null;
            try
            {
                lines = File.ReadAllLines(csvPathStations);
            }
            catch 
            {
                Log.Information("Bug read csv with stations");
            }

            var stations = new List<XYZ>();
            if (lines.Length > 1)
            {
                var errors = new List<string>();
                
                for (int i = 1; i < lines.Length; i++)
                {
                    if (TryParseCsvLine(lines[i], out XYZ xyz, out string error))
                    {
                        stations.Add(xyz);
                        continue;
                    }
                    errors.Add($"Line {i + 1} has error: {error}");
                }

                if (errors.Count > 0)
                {
                    foreach (var error in errors)
                    {
                        Log.Information(error);
                    }
                }
            }
            else
            {
                Log.Information("CSV file is empty or has only headers.");
            }

            List<XYZ> newStations = new List<XYZ>();
            try
            {
                using (TransactionGroup tg = new TransactionGroup(doc, "Place Spheres"))
                {
                    tg.Start();

                    while (true)
                    {
                        // Step 1: User clicks to select a point
                        XYZ point;
                        try
                        {
                            point = uidoc.Selection.PickPoint("Click to place a sphere or press ESC to finish");
                            var x =  trans.OfPoint(point) * Constants.feet2Meter;
                            newStations.Add(x);
                        }
                        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                        {
                            break; // Exit the loop when ESC is pressed
                        }

                        using (Transaction tx = new Transaction(doc, "Place Sphere"))
                        {
                            tx.Start();

                            // Create a profile curve for the sphere (a semi-circle)
                            List<Curve> profileCurves = new List<Curve>();
                            XYZ center = new XYZ(point.X, point.Y, transformedHeight.Z);
                            XYZ top = new XYZ(point.X, point.Y, transformedHeight.Z + radius);
                            XYZ bottom = new XYZ(point.X, point.Y, transformedHeight.Z - radius);
                            Arc arc = Arc.Create(top, bottom, center + new XYZ(radius, 0, 0));
                            profileCurves.Add(Line.CreateBound(bottom, top));
                            profileCurves.Add(arc);

                            CurveLoop profile = CurveLoop.Create(profileCurves);

                            // Axis of revolution (Y-axis through the center point)
                            Line axis = Line.CreateBound(point, point + XYZ.BasisY);

                            // Create a revolved solid using the profile and the axis
                            Solid sphere = GeometryCreationUtilities.CreateRevolvedGeometry(
                                new Frame(point, XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ),
                                new CurveLoop[] { profile },0,2 * Math.PI);

                            // Create a DirectShape element in Revit
                            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                            ds.SetShape(new GeometryObject[] { sphere });

                            tx.Commit();
                        }
                    }
                    tg.Assimilate(); // Commit the transaction group
                }

                Level currentLevel = doc.ActiveView.GenLevel;
                string levelName = currentLevel.Name;
                TaskDialog.Show("Message", newStations.Count.ToString() + " newStations");
                #region write stations to csv

                using StreamWriter csv = File.CreateText(Path.Combine(csvPathStations, "Stations2.csv"));
                csv.WriteLine(CsvHeader);

                foreach (var item in stations)
                {
                    csv.WriteLine(item.X.ToString() + ";" + item.Y.ToString() + ";" + item.Z.ToString());
                }
                foreach (var item in newStations)
                {
                    csv.WriteLine(item.X.ToString() + ";" + item.Y.ToString() + ";" + item.Z.ToString());
                }
                csv.Close();
                #endregion write stations to csv

                //Helper.CreateAndStoreSphereData(doc, sphereCoordinate, levelName);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
        public static bool TryParseCsvLine(string line, out XYZ xyz, out string error)
        {
            xyz = null;
            error = string.Empty;

            var parts = line.Split(',');
            if (parts.Length < 3)
            {
                error = "Not enough parts in the line.";
                return false;
            }

            if (!double.TryParse(parts[0], out double x))
            {
                error = "Invalid X coordinate.";
                return false;
            }

            if (!double.TryParse(parts[1], out double y))
            {
                error = "Invalid Y coordinate.";
                return false;
            }

            if (!double.TryParse(parts[2], out double z))
            {
                error = "Invalid Z coordinate.";
                return false;
            }

            xyz = new XYZ(x, y, z);
            return true;
        }
    }
}