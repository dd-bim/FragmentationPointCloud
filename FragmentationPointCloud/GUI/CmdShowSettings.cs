using System;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Serilog;

namespace Revit.GUI
{
    [Transaction(TransactionMode.Manual)]
    public class CmdShowSettings : IExternalCommand
    {
        private string _path = "";
        private SettingsJson _set;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region setup

            var uiDoc = commandData.Application.ActiveUIDocument;
            var doc = uiDoc.Document;
            try
            {
                _path = Path.GetDirectoryName(doc.PathName) ?? throw new NullReferenceException();
            }
            catch (Exception)
            {
                TaskDialog.Show("Message", "The file has not been saved yet.");
                return Result.Failed;
            }

            // logger
            string logsPath = Path.Combine(_path, ResourcePng.LogsFolderName);
            if (!Directory.Exists(logsPath)) Directory.CreateDirectory(logsPath);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(logsPath, ResourcePng.LogFile), rollingInterval: RollingInterval.Minute)
                .CreateLogger();
            Log.Information("start CmdShowSettings");

            #endregion setup

            // settings json
            try
            {
                _set = SettingsJson.ReadSettingsJson(Constants.pathSettings);
            }
            catch
            {
                _set = SettingsJson.ReadSettingsJson(Constants.readPathSettings);
            }

            try
            {
                var propUI = new WinSettings(_set);
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