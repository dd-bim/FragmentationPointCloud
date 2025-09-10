using System;
using System.IO;
using System.Reflection;

namespace Revit
{
    public static class Constants
    {
        // Mathematische Konstanten (unverändert)
        public const double gradToRad = Math.PI / 180;
        public const double feet2Meter = 0.3048;
        public const double meter2Feet = 1.0 / feet2Meter;
        public const double TRIGTOL = 1.0e-11;
        public const byte MinTinDigits = 6;
        public const int PlaneDigits = 2;

        // Revit-Jahr (falls an anderer Stelle verwendet)
        public static readonly string year = "2025";

        // Basis: Ordner der laufenden Assembly (Bin im Bundle oder Build-Ausgabeordner)
        private static readonly Lazy<string> _assemblyDir = new(() =>
        {
            var loc = Assembly.GetExecutingAssembly().Location;
            return string.IsNullOrEmpty(loc)
                ? AppContext.BaseDirectory
                : Path.GetDirectoryName(loc)!;
        });

        public static string AssemblyBinDirectory => _assemblyDir.Value;

        // Settings-Datei liegt im Bin-Ordner
        public static string SettingsFilePath => Path.Combine(AssemblyBinDirectory, "SettingsGreen3DScan.json");

        // Abwärtskompatibilität (vorher unterschiedliche Werte) -> beide zeigen jetzt auf dieselbe Datei
        public static readonly string pathSettings = SettingsFilePath;
        public static readonly string readPathSettings = SettingsFilePath;

        // Exe / Hilfsdateien: angenommen ebenfalls im Bin (ansonsten später anpassen)
        private static string Tool(string fileName) => Path.Combine(AssemblyBinDirectory, fileName);

        public static readonly string exeFragmentationBBox = Tool("SegmentationBBox.exe");
        public static readonly string exeFragmentationVoxel = Tool("SegmentationVoxel.exe");
        public static readonly string exeSearchVoxel = Tool("SearchVoxel.exe");
        public static readonly string exeIfcBox = Tool("IFCFaceBoxExtractor.exe");
        public static readonly string jsonIfcBox = Tool("basicList.json");

        // Ehemalige "directory" Konstante (fraglich ob nötig) – beibehalten
        public const string directory = "C:";

        // Unverändert (ggf. später dynamisch prüfen)
        public const string lineDecap = "cd C:\\Program Files\\Autodesk\\Autodesk ReCap";

        // Optional: Für Diagnose
        public static string DiagnosticsSummary() =>
            $"AssemblyBinDirectory={AssemblyBinDirectory}\n" +
            $"SettingsFilePath={SettingsFilePath}\n" +
            $"exeFragmentationBBox={exeFragmentationBBox}";
    }
}