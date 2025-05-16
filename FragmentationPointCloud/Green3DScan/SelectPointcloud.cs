using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using OpenCvSharp;
using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.Net.Mail;
using Path = System.IO.Path;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class SelectPointCloud : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!ExternalCommandHelper.GetProjectPath(commandData, out string projectPath, out _, out _))
            {
                TaskDialog.Show("Message", "The project file has not been saved yet.");
                return Result.Failed;
            }
            ExternalCommandHelper.InitLogger(projectPath);
            var settings = SettingsJson.ReadSettingsJson(Constants.pathSettings);

            Log.Information("start SelectPointCloud");
            Log.Information("BBox_Buffer: {BBox_Buffer}", settings.BBox_Buffer.ToString(CultureInfo.InvariantCulture));

            return DoExecute(projectPath, settings);
        }

        private static Result DoExecute(string projectPath, SettingsJson settings)
        {
            // Get the path to the PCD file from the user
            if (!ExternalCommandHelper.GetFilePathDialog("Select PCD file with point cloud!",
                    "PCD file(*.pcd) | *.pcd", out string pcdPathPointCloud))
            {
                TaskDialog.Show("Message", "No PCD file selected.");
                return Result.Failed;
            }
            settings.PathPointCloud = pcdPathPointCloud;
            SettingsJson.WriteSettingsJson(settings, Constants.pathSettings);

            TaskDialog.Show("Message", "Selection of point cloud successful!");

            return Result.Succeeded;
        }

    }
}