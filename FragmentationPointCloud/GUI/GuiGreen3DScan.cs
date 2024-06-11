using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Revit.Green3DScan;
using Nice3point.Revit.Extensions;

namespace Revit.GUI
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class GuiGreen3DScan : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            // new Tab
            string tabName = "Green3DScan";
            application.CreateRibbonTab(tabName);

            RibbonPanel panel1 = application.CreateRibbonPanel(tabName, "Fragmentation");

            PushButton settings = panel1.AddItem(new PushButtonData("1", "Settings", thisAssemblyPath, "Revit.GUI.CmdShowSettings")) as PushButton;
            settings.ToolTip = "Management of the basic setting, such as construction tolerance.";
            settings.LargeImage = GetBitmapFromResx(ResourcePng.set);

            PushButton selectPointcloud = panel1.AddItem(new PushButtonData("2", "Select\npoint cloud", thisAssemblyPath, "Revit.Green3DScan.SelectPointcloud")) as PushButton;
            selectPointcloud.ToolTip = "Select point cloud";
            selectPointcloud.LargeImage = GetBitmapFromResx(ResourcePng.cloud);

            PushButton oBBox = panel1.AddItem(new PushButtonData("3", "1.OBBoxXY", thisAssemblyPath, "Revit.Green3DScan.Revit2OBBox")) as PushButton;
            oBBox.ToolTip = "Export the Boudingboxes and oriented Boundingboxes.";
            oBBox.LargeImage = GetBitmapFromResx(ResourcePng.oBBoxXY);

            var segButton = panel1.AddPullDownButton("4", "2.Fragmentation");
            segButton.LargeImage = GetBitmapFromResx(ResourcePng.fragment);
            segButton.AddPushButton<FragmentationBBox>("BBox");
            segButton.AddPushButton<FragmentationVoxel>("Voxel");

            var loadButton = panel1.AddPullDownButton("5", "3.Load fragment\npoint cloud");
            loadButton.LargeImage = GetBitmapFromResx(ResourcePng.loadFragmentation);
            loadButton.AddPushButton<LoadFragmentationBBox>("BBox");
            loadButton.AddPushButton<LoadFragmentationVoxel>("Voxel");
            loadButton.AddPushButton<LoadFragmentationIFC>("IFC");

            PushButton oBBoxComplete = panel1.AddItem(new PushButtonData("6", "OBBox\ncomplete", thisAssemblyPath, "Revit.Green3DScan.FragmenattionBBoxComplete")) as PushButton;
            oBBoxComplete.ToolTip = "Export the Boudingboxes and oriented Boundingboxes.";
            oBBoxComplete.LargeImage = GetBitmapFromResx(ResourcePng.oBBoxXY);

            PushButton sectionBoxComplete = panel1.AddItem(new PushButtonData("7", "SectionBox\ncomplete", thisAssemblyPath, "Revit.Green3DScan.FragmentationSectionBoxComplete")) as PushButton;
            sectionBoxComplete.ToolTip = "Export the SectionBox and fragmented the point cloud.";
            sectionBoxComplete.LargeImage = GetBitmapFromResx(ResourcePng.oBBoxXY);

            PushButton fragmentationIFC = panel1.AddItem(new PushButtonData("8", "Fragmentation IFC", thisAssemblyPath, "Revit.Green3DScan.FragmentationIFC")) as PushButton;
            fragmentationIFC.ToolTip = "Use IFC to calculate the oriented bounding boxes and fragmented the point cloud.";
            fragmentationIFC.LargeImage = GetBitmapFromResx(ResourcePng.ifc);

            RibbonPanel panel2 = application.CreateRibbonPanel(tabName, "Routing");

            PushButton route = panel2.AddItem(new PushButtonData("8", "RoutePgm", thisAssemblyPath, "Revit.Green3DScan.RoutePgm")) as PushButton;
            route.ToolTip = "Export plan as Portable Grey Map.";
            route.LargeImage = GetBitmapFromResx(ResourcePng.tool);

            PushButton route2 = panel2.AddItem(new PushButtonData("9", "RoutePgmPicture", thisAssemblyPath, "Revit.Green3DScan.RoutePgm2")) as PushButton;
            route2.ToolTip = "Export plan as Portable Grey Map.";
            route2.LargeImage = GetBitmapFromResx(ResourcePng.tool);
            
            PushButton route3 = panel2.AddItem(new PushButtonData("10", "RouteStations", thisAssemblyPath, "Revit.Green3DScan.RouteStations")) as PushButton;
            route3.ToolTip = "Export coordinates of ScanStations.";
            route3.LargeImage = GetBitmapFromResx(ResourcePng.tool);

            RibbonPanel panel3 = application.CreateRibbonPanel(tabName, "ScanStations");

            PushButton stations = panel3.AddItem(new PushButtonData("11", "BIM2Stations", thisAssemblyPath, "Revit.Green3DScan.Revit2Stations")) as PushButton;
            stations.ToolTip = "BIM2Stations";
            stations.LargeImage = GetBitmapFromResx(ResourcePng.tool);

            PushButton AddStation = panel3.AddItem(new PushButtonData("12", "AddStation", thisAssemblyPath, "Revit.Green3DScan.AddStation")) as PushButton;
            AddStation.ToolTip = "AddStation";
            AddStation.LargeImage = GetBitmapFromResx(ResourcePng.tool);

            PushButton Stations2NotVisibleFaces = panel3.AddItem(new PushButtonData("13", "Stations2NotVisibleFaces", thisAssemblyPath, "Revit.Green3DScan.Stations2NotVisibleFaces")) as PushButton;
            Stations2NotVisibleFaces.ToolTip = "Stations2NotVisibleFaces";
            Stations2NotVisibleFaces.LargeImage = GetBitmapFromResx(ResourcePng.tool);

            RibbonPanel panel4 = application.CreateRibbonPanel(tabName, "Tools");

            PushButton pcd2E57 = panel4.AddItem(new PushButtonData("14", "Pcd2E57", thisAssemblyPath, "Revit.Green3DScan.Pcd2E57")) as PushButton;
            pcd2E57.ToolTip = "pcd2E57";
            pcd2E57.LargeImage = GetBitmapFromResx(ResourcePng.tool);

            PushButton e572pcd = panel4.AddItem(new PushButtonData("15", "E572pcd", thisAssemblyPath, "Revit.Green3DScan.E572Pcd")) as PushButton;
            e572pcd.ToolTip = "e572pcd";
            e572pcd.LargeImage = GetBitmapFromResx(ResourcePng.tool);

            return Result.Succeeded;
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private BitmapImage GetBitmapFromResx(System.Drawing.Bitmap bmp)
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

