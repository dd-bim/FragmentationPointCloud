using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
//using Revit.Green3DScan.Fragmentation;

namespace Revit.GUI
{
    [Transaction(TransactionMode.Manual)]
    public class GuiGreen3DScan : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            const string tabName = "Green3DScan";
            application.CreateRibbonTab(tabName);

            var panel1 = application.CreateRibbonPanel(tabName, "Fragmentation");

            var settings = panel1.AddItem(
                    new PushButtonData("1", "Settings", thisAssemblyPath, "Revit.GUI.CmdShowSettings")) as
                    PushButton;
            settings?.ToolTip = "Management of the basic setting, such as bounding box buffer.";
            settings?.LargeImage = GetBitmapFromResx(ResourcePng.set);

            //var selectPointCloud = panel1.AddItem(new PushButtonData("2", "Select\npoint cloud", thisAssemblyPath,
            //    "Revit.Green3DScan.SelectPointCloud")) as PushButton;
            //selectPointCloud.ToolTip = "Select the point cloud.";
            //selectPointCloud.LargeImage = GetBitmapFromResx(ResourcePng.cloud);

            //var oBBox = panel1.AddItem(new PushButtonData("3", "1.OBBoxXY", thisAssemblyPath,
            //    "Revit.Green3DScan.Revit2OBBox")) as PushButton;
            //oBBox.ToolTip = "Export the bounding boxes and oriented bounding boxes.";
            //oBBox.LargeImage = GetBitmapFromResx(ResourcePng.oBBoxXY);

            //PulldownButton segButton = panel1.AddPullDownButton("4", "2.Fragmentation");
            //segButton.LargeImage = GetBitmapFromResx(ResourcePng.fragment);
            //segButton.AddPushButton<FragmentationBBox>("BBox");
            //segButton.AddPushButton<FragmentationVoxel>("Voxel");

            //PulldownButton loadButton = panel1.AddPullDownButton("5", "3.Load fragment\npoint cloud");
            //loadButton.LargeImage = GetBitmapFromResx(ResourcePng.loadFragmentation);
            //loadButton.AddPushButton<LoadFragmentationBBox>("BBox");
            //loadButton.AddPushButton<LoadFragmentationVoxel>("Voxel");
            //loadButton.AddPushButton<LoadFragmentationIFC>("IFC");

            //PushButton oBBoxComplete = panel1.AddItem(new PushButtonData("6", "OBBox\ncomplete", thisAssemblyPath,
            //    "Revit.Green3DScan.FragmentationBBoxComplete")) as PushButton;
            //oBBoxComplete.ToolTip = "Export the bounding boxes and oriented bounding boxes.";
            //oBBoxComplete.LargeImage = GetBitmapFromResx(ResourcePng.oBBoxXY);

            //var sectionBoxComplete = panel1.AddItem(new PushButtonData("7", "SectionBox\ncomplete", thisAssemblyPath,
            //    "Revit.Green3DScan.FragmentationSectionBoxComplete")) as PushButton;
            //sectionBoxComplete.ToolTip = "Export the section box and fragmented the point cloud.";
            //sectionBoxComplete.LargeImage = GetBitmapFromResx(ResourcePng.oBBoxXY);

            //var fragmentationIFC = panel1.AddItem(new PushButtonData("8", "Fragmentation IFC", thisAssemblyPath,
            //    "Revit.Green3DScan.FragmentationIFC")) as PushButton;
            //fragmentationIFC.ToolTip =
            //    "Use IFC to calculate the oriented bounding boxes and fragmented the point cloud.";
            //fragmentationIFC.LargeImage = GetBitmapFromResx(ResourcePng.ifc);

            var panel2 = application.CreateRibbonPanel(tabName, "Routing");

            //var route = panel2.AddItem(new PushButtonData("9", "RoutePgm", thisAssemblyPath,
            //    "Revit.Green3DScan.RoutePgm")) as PushButton;
            //route.ToolTip = "Export plan as Portable Grey Map.";
            //route.LargeImage = GetBitmapFromResx(ResourcePng.routePgm);

            //var route2 =
            //    panel2.AddItem(new PushButtonData("10", "RoutePgmExport", thisAssemblyPath,
            //        "Revit.Green3DScan.RoutePgmExport")) as PushButton;
            //route2.ToolTip = "Export plan as Portable Grey Map.";
            //route2.LargeImage = GetBitmapFromResx(ResourcePng.routePgmExport);

            var panel3 = application.CreateRibbonPanel(tabName, "Stations and PointClouds");

            var route3 = panel3.AddItem(new PushButtonData("11", "BIM2FaceObjects", thisAssemblyPath,
                "Revit.Green3DScan.SimulatePointCloud.Bim2FaceObjects")) as PushButton;
            route3?.ToolTip = "Export faces from BIM.";
            route3?.LargeImage = GetBitmapFromResx(ResourcePng.bim2Face);

            var stations =
                panel3.AddItem(new PushButtonData("12", "BIM2Stations", thisAssemblyPath,
                    "Revit.Green3DScan.SimulatePointCloud.Bim2Stations")) as PushButton;
            stations?.ToolTip = "Calculates stations in door centers and room centers.";
            stations?.LargeImage = GetBitmapFromResx(ResourcePng.bim2Stations);

            var AddStation =
                panel3.AddItem(new PushButtonData("13", "AddStation", thisAssemblyPath, "Revit.Green3DScan.SimulatePointCloud.AddStation"))
                    as PushButton;
            AddStation?.ToolTip = "Adds one or more stations to a floor plan.";
            AddStation?.LargeImage = GetBitmapFromResx(ResourcePng.addStation);

            var raster =
                panel3.AddItem(new PushButtonData("14", "Grid", thisAssemblyPath, "Revit.Green3DScan.SimulatePointCloud.Raster")) as
                    PushButton;
            raster?.ToolTip = "Creates a grid of stations according to the settings.";
            raster?.LargeImage = GetBitmapFromResx(ResourcePng.grid);

            var loadStations =
                panel3.AddItem(new PushButtonData("15", "LoadStations", thisAssemblyPath,
                    "Revit.Green3DScan.SimulatePointCloud.LoadStations")) as PushButton;
            loadStations?.ToolTip = "Loads stations from a CSV file.";
            loadStations?.LargeImage = GetBitmapFromResx(ResourcePng.loadStations);

            var Stations2NotVisibleFaces = panel3.AddItem(new PushButtonData("16", "Stations2NotVisibleFaces",
                thisAssemblyPath, "Revit.Green3DScan.SimulatePointCloud.Stations2NotVisibleFaces")) as PushButton;
            Stations2NotVisibleFaces?.ToolTip =
                "Calculates the non-visible faces based on the stations of a floor plan.";
            Stations2NotVisibleFaces?.LargeImage = GetBitmapFromResx(ResourcePng.stations2NotVisibleFaces);

            var stationsPointClouds = panel3.AddItem(new PushButtonData("17", "Stations2PointClouds", thisAssemblyPath,
                "Revit.Green3DScan.SimulatePointCloud.Stations2PointClouds")) as PushButton;
            stationsPointClouds?.ToolTip =
                "Calculates the simulated point clouds based on the stations of a floor plan.";
            stationsPointClouds?.LargeImage = GetBitmapFromResx(ResourcePng.stations2PointClouds);

            var panel4 = application.CreateRibbonPanel(tabName, "Server");

            //var server =
            //    panel4.AddItem(new PushButtonData("18", "Server", thisAssemblyPath, "Revit.Green3DScan.Server")) as
            //        PushButton;
            //server.ToolTip = "Creates a connection to the server for point cloud fragments.";
            //server.LargeImage = GetBitmapFromResx(ResourcePng.server);

            var panel5 = application.CreateRibbonPanel(tabName, "Tools");

            //var pcd2E57 =
            //    panel5.AddItem(new PushButtonData("19", "Pcd2E57", thisAssemblyPath, "Revit.Green3DScan.Pcd2E57")) as
            //        PushButton;
            //pcd2E57.ToolTip = "Converts PCD to E57.";
            //pcd2E57.LargeImage = GetBitmapFromResx(ResourcePng.tool);

            //var e572pcd =
            //    panel5.AddItem(new PushButtonData("20", "E572pcd", thisAssemblyPath, "Revit.Green3DScan.E572Pcd")) as
            //        PushButton;
            //e572pcd.ToolTip = "Converts E57 to PCD.";
            //e572pcd.LargeImage = GetBitmapFromResx(ResourcePng.tool);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;

        private static BitmapImage GetBitmapFromResx(Bitmap bmp)
        {
            var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            var img = new BitmapImage();
            ms?.Position = 0;
            img.BeginInit();
            img.StreamSource = ms;
            img.EndInit();

            return img;
        }
    }
}