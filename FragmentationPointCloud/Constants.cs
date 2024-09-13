using System;

namespace Revit
{
    public static class Constants
    {
        public static readonly double gradToRad = Math.PI / 180;
        public static readonly double radToGrad = 180 / Math.PI;
        public static readonly double feet2Meter = 0.3048;
        public static readonly double meter2Feet = 1.0 / feet2Meter;
        public static readonly double squareFeet2SquareMeter = feet2Meter * feet2Meter;

        public static readonly double SQRT2 = 1.4142135623730950488016887242097;

        public static readonly double SQRT3 = 1.7320508075688772935274463415059;

        public static readonly double RSQRT2 = 0.70710678118654752440084436210485;

        public static readonly double RSQRT3 = 0.57735026918962576450914878050196;

        public static readonly string year = "2024";

        public static readonly string pathSettings = $@"C:\ProgramData\Autodesk\Revit\Addins\{year}\SettingsGreen3DScan.json";
        public static readonly string readPathSettings = $@"C:\ProgramData\Autodesk\Revit\Addins\{year}\Green3DScan\SettingsGreen3DScan.json";

        public static readonly string exeFragmentationBBox = $@"C:\ProgramData\Autodesk\Revit\Addins\{year}\SegmentationBBox.exe";
        public static readonly string exeFragmentationVoxel = $@"C:\ProgramData\Autodesk\Revit\Addins\{year}\SegmentationVoxel.exe";
        public static readonly string exeSearchVoxel = $@"C:\ProgramData\Autodesk\Revit\Addins\{year}\SearchVoxel.exe";
        public static readonly string exeIfcBox = $@"C:\ProgramData\Autodesk\Revit\Addins\{year}\IFCFaceBoxExtractor.exe";
        public static readonly string jsonIfcBox = $@"C:\ProgramData\Autodesk\Revit\Addins\{year}\basicList.json";

        public static readonly string cloudComparePath = @"C:\Program Files\CloudCompare\CloudCompare.exe";

        public static readonly string directory = "C:";
        public static readonly string lineDecap = "cd C:\\Program Files\\Autodesk\\Autodesk ReCap";
        //public static readonly string directory = "D:";
        //public static readonly string lineDecap = "cd D:\\Programme_\\Autodesk ReCap";
    }
}
