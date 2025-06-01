using System;

namespace Revit
{
    public static class Constants
    {
        public const double gradToRad = Math.PI / 180;
        public const double feet2Meter = 0.3048;
        public const double meter2Feet = 1.0 / feet2Meter;
        public const double TRIGTOL = 1.0e-11; // tolerance for trigonometric calculations
        public const byte MinTinDigits = 6; // Minimal provided number of decimal places for TIN calculations

        public static readonly string year = "2025";

        public static readonly string pathSettings =
            $@"C:\ProgramData\Autodesk\Revit\Addins\{year}\SettingsGreen3DScan.json";

        public static readonly string readPathSettings =
            $@"C:\ProgramData\Autodesk\Revit\Addins\{year}\Green3DScan\SettingsGreen3DScan.json";

        public static readonly string exeFragmentationBBox =
            $@"C:\ProgramData\Autodesk\Revit\Addins\{year}\SegmentationBBox.exe";

        public static readonly string exeFragmentationVoxel =
            $@"C:\ProgramData\Autodesk\Revit\Addins\{year}\SegmentationVoxel.exe";

        public static readonly string exeSearchVoxel = $@"C:\ProgramData\Autodesk\Revit\Addins\{year}\SearchVoxel.exe";

        public static readonly string exeIfcBox =
            $@"C:\ProgramData\Autodesk\Revit\Addins\{year}\IFCFaceBoxExtractor.exe";

        public static readonly string jsonIfcBox = $@"C:\ProgramData\Autodesk\Revit\Addins\{year}\basicList.json";

        public const string directory = "C:";

        public const string lineDecap = "cd C:\\Program Files\\Autodesk\\Autodesk ReCap";

        public const int PlaneDigits = 2; // number of decimal places for the planes
    }
}