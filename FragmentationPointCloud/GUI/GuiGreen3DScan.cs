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

            string tabName = "Green3DScan";
            application.CreateRibbonTab(tabName);

            RibbonPanel panel1 = application.CreateRibbonPanel(tabName, "Fragmentation");

            PushButton settings = panel1.AddItem(new PushButtonData("1", "Settings", thisAssemblyPath, "Revit.GUI.CmdShowSettings")) as PushButton;
            settings.ToolTip = "Management of the basic setting, such as bbox buffer.";
            settings.LargeImage = GetBitmapFromResx(ResourcePng.set);

            PushButton selectPointcloud = panel1.AddItem(new PushButtonData("2", "Select\npoint cloud", thisAssemblyPath, "Revit.Green3DScan.SelectPointcloud")) as PushButton;
            selectPointcloud.ToolTip = "Select the point cloud.";
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

            PushButton oBBoxComplete = panel1.AddItem(new PushButtonData("6", "OBBox\ncomplete", thisAssemblyPath, "Revit.Green3DScan.FragmentationBBoxComplete")) as PushButton;
            oBBoxComplete.ToolTip = "Export the Boudingboxes and oriented Boundingboxes.";
            oBBoxComplete.LargeImage = GetBitmapFromResx(ResourcePng.oBBoxXY);

            PushButton sectionBoxComplete = panel1.AddItem(new PushButtonData("7", "SectionBox\ncomplete", thisAssemblyPath, "Revit.Green3DScan.FragmentationSectionBoxComplete")) as PushButton;
            sectionBoxComplete.ToolTip = "Export the SectionBox and fragmented the point cloud.";
            sectionBoxComplete.LargeImage = GetBitmapFromResx(ResourcePng.oBBoxXY);

            PushButton fragmentationIFC = panel1.AddItem(new PushButtonData("8", "Fragmentation IFC", thisAssemblyPath, "Revit.Green3DScan.FragmentationIFC")) as PushButton;
            fragmentationIFC.ToolTip = "Use IFC to calculate the oriented bounding boxes and fragmented the point cloud.";
            fragmentationIFC.LargeImage = GetBitmapFromResx(ResourcePng.ifc);

            RibbonPanel panel2 = application.CreateRibbonPanel(tabName, "Routing");

            PushButton route = panel2.AddItem(new PushButtonData("9", "RoutePgm", thisAssemblyPath, "Revit.Green3DScan.RoutePgm")) as PushButton;
            route.ToolTip = "Export plan as Portable Grey Map.";
            route.LargeImage = GetBitmapFromResx(ResourcePng.routePgm);

            PushButton route2 = panel2.AddItem(new PushButtonData("10", "RoutePgmExport", thisAssemblyPath, "Revit.Green3DScan.RoutePgm2")) as PushButton;
            route2.ToolTip = "Export plan as Portable Grey Map.";
            route2.LargeImage = GetBitmapFromResx(ResourcePng.routePgmExport);

            RibbonPanel panel3 = application.CreateRibbonPanel(tabName, "Stations and PointClouds");

            PushButton route3 = panel3.AddItem(new PushButtonData("11", "BIM2FaceObjects", thisAssemblyPath, "Revit.Green3DScan.Revit2FaceObjects")) as PushButton;
            route3.ToolTip = "Export faces from BIM.";
            route3.LargeImage = GetBitmapFromResx(ResourcePng.bim2Face);

            PushButton stations = panel3.AddItem(new PushButtonData("12", "BIM2Stations", thisAssemblyPath, "Revit.Green3DScan.Revit2Stations")) as PushButton;
            stations.ToolTip = "Calculates stations in door centers and room centers.";
            stations.LargeImage = GetBitmapFromResx(ResourcePng.bim2Stations);

            PushButton AddStation = panel3.AddItem(new PushButtonData("13", "AddStation", thisAssemblyPath, "Revit.Green3DScan.AddStation")) as PushButton;
            AddStation.ToolTip = "Adds one or more stations to a floor plan.";
            AddStation.LargeImage = GetBitmapFromResx(ResourcePng.addStation);

            PushButton raster = panel3.AddItem(new PushButtonData("14", "Grid", thisAssemblyPath, "Revit.Green3DScan.Raster")) as PushButton;
            raster.ToolTip = "Creates a grid of stations according to the settings.";
            raster.LargeImage = GetBitmapFromResx(ResourcePng.grid);

            PushButton loadStations = panel3.AddItem(new PushButtonData("15", "LoadStations", thisAssemblyPath, "Revit.Green3DScan.LoadStations")) as PushButton;
            loadStations.ToolTip = "Loads stations from a CSV file.";
            loadStations.LargeImage = GetBitmapFromResx(ResourcePng.loadStations);

            PushButton Stations2NotVisibleFaces = panel3.AddItem(new PushButtonData("16", "Stations2NotVisibleFaces", thisAssemblyPath, "Revit.Green3DScan.Stations2NotVisibleFaces")) as PushButton;
            Stations2NotVisibleFaces.ToolTip = "Calculates the non-visible faces based on the stations of a floor plan.";
            Stations2NotVisibleFaces.LargeImage = GetBitmapFromResx(ResourcePng.stations2NotVisibleFaces);

            PushButton stationsPointClouds = panel3.AddItem(new PushButtonData("17", "Stations2PointClouds", thisAssemblyPath, "Revit.Green3DScan.Stations2Pointclouds")) as PushButton;
            stationsPointClouds.ToolTip = "Calculates the simulated point clouds based on the stations of a floor plan.";
            stationsPointClouds.LargeImage = GetBitmapFromResx(ResourcePng.stations2PointClouds);

            RibbonPanel panel4 = application.CreateRibbonPanel(tabName, "Server");

            PushButton server = panel4.AddItem(new PushButtonData("18", "Server", thisAssemblyPath, "Revit.Green3DScan.Server")) as PushButton;
            server.ToolTip = "Creates a connection to the server for point cloud fragments.";
            server.LargeImage = GetBitmapFromResx(ResourcePng.server);

            RibbonPanel panel5 = application.CreateRibbonPanel(tabName, "Tools");

            PushButton pcd2E57 = panel5.AddItem(new PushButtonData("19", "Pcd2E57", thisAssemblyPath, "Revit.Green3DScan.Pcd2E57")) as PushButton;
            pcd2E57.ToolTip = "Converts PCD to E57.";
            pcd2E57.LargeImage = GetBitmapFromResx(ResourcePng.tool);

            PushButton e572pcd = panel5.AddItem(new PushButtonData("20", "E572pcd", thisAssemblyPath, "Revit.Green3DScan.E572Pcd")) as PushButton;
            e572pcd.ToolTip = "Converts E57 to PCD.";
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

