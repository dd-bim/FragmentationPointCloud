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

        public static readonly string pathSettings = @"C:\ProgramData\Autodesk\Revit\Addins\2023\CPMSettings.json";
        public static readonly string readPathSettings = @"C:\ProgramData\Autodesk\Revit\Addins\2023\CPM\CPMSettings.json";

        public static readonly string fragmentationBBox = @"C:\ProgramData\Autodesk\Revit\Addins\2023\SegmentationBBox.exe";
        public static readonly string fragmentationVoxel = @"C:\ProgramData\Autodesk\Revit\Addins\2023\SegmentationVoxel.exe";
        public static readonly string searchVoxel = @"C:\ProgramData\Autodesk\Revit\Addins\2023\SearchVoxel.exe";

        public static readonly string cloudComparePath = @"C:\Program Files\CloudCompare\CloudCompare.exe";

        //public static readonly string directory = "C:";
        //public static readonly string lineDecap = "cd C:\\Program Files\\Autodesk\\Autodesk ReCap";
        public static readonly string directory = "D:";
        public static readonly string lineDecap = "cd D:\\Programme_\\Autodesk ReCap";
    }
}
