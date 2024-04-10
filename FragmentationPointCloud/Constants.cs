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

        public static readonly string pathSettings = @"C:\ProgramData\Autodesk\Revit\Addins\2023\SettingsGreen3DScan.json";
        public static readonly string readPathSettings = @"C:\ProgramData\Autodesk\Revit\Addins\2023\Green3DScan\SettingsGreen3DScan.json";

        public static readonly string exeFragmentationBBox = @"C:\ProgramData\Autodesk\Revit\Addins\2023\SegmentationBBox.exe";
        public static readonly string exeFragmentationVoxel = @"C:\ProgramData\Autodesk\Revit\Addins\2023\SegmentationVoxel.exe";
        public static readonly string exeSearchVoxel = @"C:\ProgramData\Autodesk\Revit\Addins\2023\SearchVoxel.exe";
        public static readonly string exeIfcBox = @"C:\ProgramData\Autodesk\Revit\Addins\2023\IFCFaceBoxExtractor.exe";
        public static readonly string jsonIfcBox = @"C:\ProgramData\Autodesk\Revit\Addins\2023\basicList.json";

        public static readonly string cloudComparePath = @"C:\Program Files\CloudCompare\CloudCompare.exe";

        public static readonly string directory = "C:";
        public static readonly string lineDecap = "cd C:\\Program Files\\Autodesk\\Autodesk ReCap";
        //public static readonly string directory = "D:";
        //public static readonly string lineDecap = "cd D:\\Programme_\\Autodesk ReCap";
    }
}
