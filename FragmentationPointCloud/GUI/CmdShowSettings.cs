using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Green3DScan;
using Serilog;
using System;
using System.IO;

namespace Revit.GUI
{
    [Transaction(TransactionMode.Manual)]
    public class CmdShowSettings : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!ExternalCommandHelper.GetProjectPath(commandData, out string projectPath, out _, out _))
            {
                TaskDialog.Show("Message", "The project file has not been saved yet.");
                return Result.Failed;
            }
            ExternalCommandHelper.InitLogger(projectPath);
            Log.Information("start CmdShowSettings");


            // settings json
            SettingsJson settings;
            try
            {
                settings = SettingsJson.ReadSettingsJson(Constants.pathSettings);
            }
            catch
            {
                settings = SettingsJson.ReadSettingsJson(Constants.readPathSettings);
            }

            try
            {
                var propUI = new WinSettings(settings);
                propUI.ShowDialog();

                if (propUI.SaveChanges)
                {
                    var modified = propUI.Data;
                    var j = modified["Green3DScan"];
                    var json = new SettingsJson
                    {
                        BBox_Buffer = double.Parse(j[0].AttributeValue),
                        OnlyPlanarFaces = bool.Parse(j[1].AttributeValue),
                        CoordinatesReduction = bool.Parse(j[2].AttributeValue),
                        PgmHeightOfLevel_Meter = double.Parse(j[3].AttributeValue),
                        PgmImageExpansion_Px = double.Parse(j[4].AttributeValue),
                        PgmImageResolution_Meter = double.Parse(j[5].AttributeValue),
                        VerbosityLevel = j[6].AttributeValue,
                        PathPointCloud = j[7].AttributeValue,
                        PathCloudCompare = j[8].AttributeValue,
                        PathDecap = j[9].AttributeValue,
                        ServerUuid = j[10].AttributeValue,
                        FragmentationVoxelResolution_Meter = double.Parse(j[11].AttributeValue),
                        StepsPerFullTurn = int.Parse(j[12].AttributeValue),
                        SphereDiameter_Meter = double.Parse(j[13].AttributeValue),
                        HeightOfScanner_Meter = double.Parse(j[14].AttributeValue),
                        NoiseOfScanner_Meter = double.Parse(j[15].AttributeValue),
                        Beta_Degree = double.Parse(j[16].AttributeValue),
                        MinDF_Meter = double.Parse(j[17].AttributeValue),
                        MaxDF_Meter = double.Parse(j[18].AttributeValue),
                        MaxPlaneDist_Meter = double.Parse(j[19].AttributeValue),
                        GridSpacing_Meter = double.Parse(j[20].AttributeValue),
                        GridColumns = int.Parse(j[21].AttributeValue),
                        GridRows = int.Parse(j[22].AttributeValue)
                    };
                    // overwrite updated json
                    SettingsJson.WriteSettingsJson(json, Constants.pathSettings);
                }
            }
            catch (Exception e)
            {
                TaskDialog.Show("Exception", e.ToString());
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}