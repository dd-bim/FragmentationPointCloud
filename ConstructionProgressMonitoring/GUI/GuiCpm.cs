using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Revit.Green3DScan;
using Nice3point.Revit.Extensions;

namespace Revit.GUI
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class GuiCpm : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            // new Tab
            string tabName = "Green3DScan";
            application.CreateRibbonTab(tabName);

            RibbonPanel panel1 = application.CreateRibbonPanel(tabName, "Fragmentation");

            PushButton selectPointcloud = panel1.AddItem(new PushButtonData("33", "Select\npoint cloud", thisAssemblyPath, "Revit.Green3DScan.SelectPointcloud")) as PushButton;
            selectPointcloud.ToolTip = "Select point cloud";
            selectPointcloud.LargeImage = getBitmapFromResx(ResourcePng.cloud);

            PushButton oBBox = panel1.AddItem(new PushButtonData("17", "1.OBBoxXY", thisAssemblyPath, "Revit.Green3DScan.Revit2OBBox")) as PushButton;
            oBBox.ToolTip = "Export the Boudingboxes and oriented Boundingboxes.";
            oBBox.LargeImage = getBitmapFromResx(ResourcePng.oBBoxXY);

            var segButton = panel1.AddPullDownButton("18", "2.Fragmentation");
            segButton.LargeImage = getBitmapFromResx(ResourcePng.fragment);
            segButton.AddPushButton<FragmentationBBox>("BBox");
            segButton.AddPushButton<FragmentationVoxel>("Voxel");

            var loadButton = panel1.AddPullDownButton("19", "3.Load fragment\npoint cloud");
            loadButton.LargeImage = getBitmapFromResx(ResourcePng.loadFragmentation);
            loadButton.AddPushButton<LoadFragmentationBBox>("BBox");
            loadButton.AddPushButton<LoadFragmentationVoxel>("Voxel");
            loadButton.AddPushButton<LoadFragmentationIFC>("IFC");

            PushButton oBBoxComplete = panel1.AddItem(new PushButtonData("20", "OBBox\ncomplete", thisAssemblyPath, "Revit.Green3DScan.FragmenattionBBoxComplete")) as PushButton;
            oBBoxComplete.ToolTip = "Export the Boudingboxes and oriented Boundingboxes.";
            oBBoxComplete.LargeImage = getBitmapFromResx(ResourcePng.oBBoxXY);

            PushButton sectionBoxComplete = panel1.AddItem(new PushButtonData("21", "SectionBox\ncomplete", thisAssemblyPath, "Revit.Green3DScan.FragmentationSectionBoxComplete")) as PushButton;
            sectionBoxComplete.ToolTip = "Export the Boudingboxes and oriented Boundingboxes.";
            sectionBoxComplete.LargeImage = getBitmapFromResx(ResourcePng.oBBoxXY);

            RibbonPanel panel2 = application.CreateRibbonPanel(tabName, "Routing");

            PushButton route = panel2.AddItem(new PushButtonData("22", "RoutePgm", thisAssemblyPath, "Revit.Green3DScan.RoutePgm")) as PushButton;
            route.ToolTip = "Export plan as Portable Grey Map.";
            route.LargeImage = getBitmapFromResx(ResourcePng.tool2);

            PushButton route2 = panel2.AddItem(new PushButtonData("23", "RoutePgmPicture", thisAssemblyPath, "Revit.Green3DScan.RoutePgm2")) as PushButton;
            route2.ToolTip = "Export plan as Portable Grey Map.";
            route2.LargeImage = getBitmapFromResx(ResourcePng.tool2);
            
            PushButton route3 = panel2.AddItem(new PushButtonData("24", "RouteStations", thisAssemblyPath, "Revit.Green3DScan.RouteStations")) as PushButton;
            route3.ToolTip = "Export coordinates of ScanStations.";
            route3.LargeImage = getBitmapFromResx(ResourcePng.tool2);

            return Result.Succeeded;
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private BitmapImage getBitmapFromResx(System.Drawing.Bitmap bmp)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage img = new BitmapImage();
            ms.Position = 0;
            img.BeginInit();
            img.StreamSource = ms;
            img.EndInit();

            return img;
        }
    }
}

