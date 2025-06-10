using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

using Serilog;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Revit.Green3DScan.SimulatePointCloud;

public static class Stations
{
    // Zentrale Konstanten für alle Stationen-Operationen
    public const int CsvLineCount = 3; // Anzahl der Spalten in der CSV-Datei
    public const string CsvHeader = "East;North;Elevation";
    public const string ObjectCsvHeader = "ObjectGuid;ElementId;East;North;Elevation";
    public const string ScanStationFamilyName = "ScanStation";
    public const string ScanStationFamilyFile = "ScanStation.rfa";
    public const string StationDirectory = "07_Stations/";
    public const string StationsFileName = "Stations.csv";
    public const string CsvFilter = "CSV file (*.csv)|*.csv";
    public const string VisibleFacesFileName = "VisibleFaces.csv";
    public const string VisibleRefPlanesFileName = "VisibleRefPlanes.csv";
    public const string NotVisibleFacesFileName = "NotVisibleFaces.csv";
    public const string NotVisibleRefPlanesFileName = "NotVisibleRefPlanes.csv";
    public const string Bim2StationsVisibleFacesFileName = "Bim2StationsVisibleFaces.csv";
    public const string Bim2StationsVisibleFacesRefFileName = "Bim2StationsVisibleFacesRef.csv";


    public static string GetStationsDirectoryPath(string projectPath) => Path.Combine(projectPath, StationDirectory);


    public static List<XYZ> CollectFromUser(UIDocument uiDoc, double scannerHeightFeet)
    {
        var stations = new List<XYZ>();
        while (true)
        {
            try
            {
                var point = uiDoc.Selection.PickPoint("Click to place a ScanStation or press ESC to finish");
                stations.Add(new XYZ(point.X, point.Y, scannerHeightFeet));
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                break;
            }
        }
        return stations;
    }

    public static List<XYZ> CollectFromFamilyInstances(Document doc, Transform trans)
    {
        if (!TryGetFamilyByName(doc, ScanStationFamilyName, out var family))
        {
            TaskDialog.Show("Message", $"Family {ScanStationFamilyName} not found.");
            return [];
        }

        return [.. GetFamilyInstances(doc, family!.Id)
            .Where(inst => inst.Location is LocationPoint)
            .Select(inst => trans.OfPoint(((LocationPoint)inst.Location).Point) * Constants.feet2Meter)];
    }

    public static void EnsureScanStationFamily(UIApplication app, string familyPath, SettingsJson settings)
    {
        string familyFile = Path.Combine(familyPath, ScanStationFamilyFile);
        if (!File.Exists(familyFile))
            CreateSphereFamily(app, settings.SphereDiameter_Meter / 2 * Constants.meter2Feet, familyFile);
    }

    public static void CreateSphereFamily(UIApplication uiapp, double radius, string familyPath)
    {
        string templatePath = $@"C:\ProgramData\Autodesk\RVT {Constants.year}\Family Templates\English\Metric Generic Model.rft";
        var familyDoc = uiapp.Application.NewFamilyDocument(templatePath);

        using (var t = new Transaction(familyDoc, "Create Sphere"))
        {
            t.Start();

            var basePoint = XYZ.Zero;
            var profile = new List<Curve>
            {
                Line.CreateBound(basePoint + new XYZ(0, radius, 0), basePoint - new XYZ(0, radius, 0)),
                Arc.Create(basePoint - new XYZ(0, radius, 0), basePoint + new XYZ(0, radius, 0), basePoint + new XYZ(radius, 0, 0))
            };

            var curveLoop = CurveLoop.Create(profile);
            var options = new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId);
            var frame = new Frame(basePoint, XYZ.BasisX, -XYZ.BasisZ, XYZ.BasisY);

            if (Frame.CanDefineRevitGeometry(frame))
            {
                var sphere = GeometryCreationUtilities.CreateRevolvedGeometry(frame, [curveLoop], 0, double.Tau, options);
                var ds = DirectShape.CreateElement(familyDoc, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.ApplicationId = "Application id";
                ds.ApplicationDataId = "Geometry object id";
                ds.SetShape([sphere]);
            }

            t.Commit();
        }

        familyDoc.SaveAs(familyPath);
        familyDoc.Close();
    }

    public static bool TryLoadAndPlaceSphereFamily(Document doc, string familyPath, List<XYZ> stations)
    {
        if (!TryLoadSphereFamily(doc, familyPath, out var familySymbol))
            return false;
        using var t = new Transaction(doc, "Place Sphere Family");
        t.Start();
        foreach (var station in stations)
        {
            doc.Create.NewFamilyInstance(station, familySymbol, StructuralType.NonStructural);
        }
        t.Commit();
        return true;
    }

    public static bool TryLoadSphereFamily(Document doc, string familyPath, out FamilySymbol? familySymbol)
    {
        using var t = new Transaction(doc, "Load Sphere Family");
        t.Start();

        string familyFile = Path.Combine(familyPath, ScanStationFamilyFile);
        if (!TryGetOrLoadFamilySymbol(doc, familyFile, ScanStationFamilyName, out familySymbol))
        {
            Log.Information("Error, no family symbol found.");
            t.Commit();
            return false;
        }

        if (!familySymbol!.IsActive)
        {
            familySymbol.Activate();
            doc.Regenerate();
        }

        t.Commit();
        return true;
    }


    public static bool TryGetOrLoadFamilySymbol(Document doc, string familyPath, string familyName, out FamilySymbol? familySymbol)
    {
        familySymbol = null;

        if (!doc.LoadFamily(familyPath, out var family))
        {
            TryGetFamilyByName(doc, familyName, out family);
        }
        if (family == null)
            return false;

        foreach (var id in family.GetFamilySymbolIds())
        {
            familySymbol = doc.GetElement(id) as FamilySymbol;
            if (familySymbol != null)
                break;
        }
        return familySymbol != null;
    }

    public static bool TryGetFamilyByName(Document document, string familyName, out Family? family)
    {
        var collector = new FilteredElementCollector(document).OfClass(typeof(Family));
        family = collector.Cast<Family>().FirstOrDefault(f => f.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase));
        return family != null;
    }

    public static List<FamilyInstance> GetFamilyInstances(Document doc, ElementId familyId)
    {
        return [.. new FilteredElementCollector(doc)
            .OfClass(typeof(FamilyInstance))
            .Cast<FamilyInstance>()
            .Where(inst => inst.Symbol.Family.Id == familyId)];
    }

    public static bool TryReadCsv(string path, Transform transform, out List<XYZ> stations)
    {
        string[] lines;
        stations = [];
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error reading CSV file: {Path}", path);
            return false;
        }

        if (lines.Length > 1)
        {
            for (int i = 1; i < lines.Length; i++)
            {
                if (TryParseCsvLine(lines[i].AsSpan(), transform, out var station))
                {
                    stations.Add(station!);
                    continue;
                }
            }
            return true;
        }
        Log.Error("TryReadCsv: CSV-File has no data lines or is not readable: {Path}", path);
        return false;
    }

    private static bool TryParseCsvLine(ReadOnlySpan<char> line, Transform trans, out XYZ? station)
    {
        station = default;

        if (line.IsEmpty || line.IsWhiteSpace())
        {
            Log.Error("TryParseCsvLine: Input string is null or empty");
            return false;
        }

        int firstSep = line.IndexOf(';');
        if (firstSep < 0)
        {
            Log.Error("TryParseCsvLine: Line: \r\n{Line}\r\n is not readable (missing first separator)", line.ToString());
            return false;
        }
        int secondSep = line[(firstSep + 1)..].IndexOf(';');
        if (secondSep < 0)
        {
            Log.Error("TryParseCsvLine: Line: \r\n{line}\r\n is not readable (missing second separator)", line.ToString());
            return false;
        }
        secondSep += firstSep + 1;

        var xSpan = line[..firstSep].Trim();
        var ySpan = line.Slice(firstSep + 1, secondSep - firstSep - 1).Trim();
        var zSpan = line[(secondSep + 1)..].Trim();

        if (double.TryParse(xSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
            double.TryParse(ySpan, NumberStyles.Float, CultureInfo.InvariantCulture, out double y) &&
            double.TryParse(zSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
        {
            var station_csv = new XYZ(x, y, z);
            station = trans.Inverse.OfPoint(station_csv * Constants.meter2Feet);
            return true;
        }

        Log.Error("TryParseCsvLine: Line: \r\n{Line}\r\n is not readable (invalid double values)", line.ToString());
        return false;
    }

    public static bool WriteCsv(string projectPath, List<XYZ> stations, Transform? transform = null)
    {
        string csvPath = Path.Combine(projectPath, StationDirectory);
        try
        {
            if (!Directory.Exists(csvPath))
                Directory.CreateDirectory(csvPath);

            string path = Path.Combine(csvPath, StationsFileName);

            using var csv = File.CreateText(path);
            csv.WriteLine(CsvHeader);

            foreach (var s in stations)
            {
                var ts = transform != null ? transform.OfPoint(s) * Constants.feet2Meter : s;
                csv.WriteLine(FormattableString.Invariant($"{ts.X};{ts.Y};{ts.Z}"));
            }
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error writing CSV to {Path}", csvPath);
            return false;
        }
    }
}