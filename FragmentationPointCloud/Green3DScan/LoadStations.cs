using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using D3 = GeometryLib.Double.D3;
using Serilog;
using Transform = Autodesk.Revit.DB.Transform;
using Sys = System.Globalization.CultureInfo;
using Path = System.IO.Path;
using Document = Autodesk.Revit.DB.Document;
using Line = Autodesk.Revit.DB.Line;
using S = ScantraIO.Data;
using Autodesk.Revit.DB.ExtensibleStorage;
using System.Linq;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class LoadStations : IExternalCommand
    {

        string path;
        public const string CsvHeader = "Rechtswert;Hochwert;Hoehe";
        public const int LineCount = 3;
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
            Log.Information("start Stations2NotVisibleFaces");
            Log.Information(set.BBox_Buffer.ToString());

            Transform trans = Helper.GetTransformation(doc, set, out var crs);

            #endregion setup
            Log.Information("setup");
            #region select files

            // Stations
            var fodStations = new FileOpenDialog("CSV file (*.csv)|*.csv");
            fodStations.Title = "Select CSV file with stations from Revit!";
            if (fodStations.Show() == ItemSelectionDialogResult.Canceled)
            {
                return Result.Cancelled;
            }
            var csvPathStations = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodStations.GetSelectedModelPath());

            #endregion select files
            Log.Information("select files");
            #region read files

            var allStations = ReadCsv(csvPathStations, out var lineErrors1, out string error1);

            #endregion read files
            Log.Information("read files");
            #region write stations to csv

            string csvPath = Path.Combine(path, "07_Stations/");

            if (!Directory.Exists(csvPath))
            {
                Directory.CreateDirectory(csvPath);
            }

            Log.Information(allStations.Count.ToString() + " stations");
            #endregion read files

            #region ScanStation

            Helper.LoadAndPlaceSphereFamily(doc, Path.Combine(path, "ScanStation.rfa"), allStations);

            //// create spheres with internal coordinates
            //for (int i = 0; i < allStations.Count; i++)
            //{
            //    // ScanStation
            //    List<Curve> profile = new List<Curve>();
            //    XYZ station = new XYZ(allStations[i].x, allStations[i].y, allStations[i].z);
            //    XYZ stationsInternal = trans.OfPoint(station) * Constants.meter2Feet;
            //    double radius = set.SphereDiameter_Meter / 2 * Constants.meter2Feet;
            //    XYZ profilePlus = stationsInternal + new XYZ(0, radius, 0);
            //    XYZ profileMinus = stationsInternal - new XYZ(0, radius, 0);

            //    profile.Add(Line.CreateBound(profilePlus, profileMinus));
            //    profile.Add(Arc.Create(profileMinus, profilePlus, stationsInternal + new XYZ(radius, 0, 0)));

            //    CurveLoop curveLoop = CurveLoop.Create(profile);
            //    SolidOptions options = new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId);

            //    Frame frame = new Frame(stationsInternal, XYZ.BasisX, -XYZ.BasisZ, XYZ.BasisY);
            //    if (Frame.CanDefineRevitGeometry(frame) == true)
            //    {
            //        Solid sphere = GeometryCreationUtilities.CreateRevolvedGeometry(frame, new CurveLoop[] { curveLoop }, 0, 2 * Math.PI, options);
            //        using Transaction t = new Transaction(doc, "Create sphere direct shape");
            //        t.Start();
            //        // create direct shape and assign the sphere shape
            //        DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            //        ds.ApplicationId = "Application id";
            //        ds.ApplicationDataId = "Geometry object id";
            //        ds.SetShape(new GeometryObject[] { sphere });
            //        t.Commit();
            //    }
            //}

            #endregion ScanStation
            TaskDialog.Show("Message", allStations.Count.ToString() + " ScanStations");
            Log.Information("end Stations2NotVisibleFaces");
            return Result.Succeeded;
        }
        public static List<D3.Vector> ReadCsv(string path, out string[] lineErrors, out string error)
        {
            string[] lines;
            var stations = new List<D3.Vector>();
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception e)
            {
                lineErrors = Array.Empty<string>();
                error = "LoadStations.ReadCsv: " + e.Message;
                return stations;
            }

            if (lines.Length > 1)
            {
                var errors = new List<string>();
                for (int i = 1; i < lines.Length; i++)
                {
                    if (TryParseCsvLine(lines[i], out var station, out error))
                    {
                        stations.Add(station);
                        continue;
                    }
                    errors.Add($"Line {i + 1} has Error: {error}");
                    error = string.Empty;
                }
                lineErrors = errors.ToArray();
                error = string.Empty;
                return stations;
            }

            lineErrors = Array.Empty<string>();
            error = "LoadStations.ReadCsv: CSV-File has no data lines";
            return stations;
        }

        public static bool TryParseCsvLine(string line, out D3.Vector station, out string error)
        {
            if (string.IsNullOrEmpty(line))
            {
                error = "LoadStations.ParseCsvLine: Input string is null or empty";
                station = default;
                return false;
            }

            var strings = line.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (strings.Length == LineCount)
            {
                error = string.Empty;
                try
                {
                    station = new D3.Vector(
                        double.Parse(strings[0], Sys.InvariantCulture) * Constants.meter2Feet,
                        double.Parse(strings[1], Sys.InvariantCulture) * Constants.meter2Feet,
                        double.Parse(strings[2], Sys.InvariantCulture) * Constants.meter2Feet
                    );
                    return true;
                }
                catch (FormatException)
                {
                    error = "LoadStations.ParseCsvLine: One of the values is not a valid double.";
                    station = default;
                    return false;
                }
                catch (OverflowException)
                {
                    error = "LoadStations.ParseCsvLine: One of the values is too large or too small.";
                    station = default;
                    return false;
                }
            }

            error = $"LoadStations.ParseCsvLine: Line: \r\n{line}\r\n is not readable";
            station = default;
            return false;
        }
    }
}
