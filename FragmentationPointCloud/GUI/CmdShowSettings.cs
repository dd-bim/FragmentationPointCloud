using System;
using System.IO;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Serilog;

namespace Revit.GUI
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class CmdShowSettings : IExternalCommand
    {
        string path;
        string dateBimLastModified;
        SettingsJson set;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region setup

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            try
            {
                path = Path.GetDirectoryName(doc.PathName);
                FileInfo fileInfo = new FileInfo(path);
                var date = fileInfo.LastWriteTime;
                dateBimLastModified = date.Year + "-" + date.Month + "-" + date.Day + "-" + date.Hour + "-" + date.Minute;
            }
            catch (Exception)
            {
                TaskDialog.Show("Message", "The file has not been saved yet.");
                return Result.Failed;
            }

            // logger
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File(Path.Combine(path, "LogFile_"), rollingInterval: RollingInterval.Day)
               .CreateLogger();
            Log.Information("start");
            #endregion setup
            // settings json
            try
            {
                set = SettingsJson.ReadSettingsJson(Constants.pathSettings);
            }
            catch
            {
                set = SettingsJson.ReadSettingsJson(Constants.readPathSettings);
            }

            try
            {
                var propUI = new WinSettings(set);
                propUI.ShowDialog();

                if (propUI.SaveChanges)
                {
                    var modified = propUI.Data;
                    var j = modified["Green3DScan"];
                    var json = new SettingsJson
                    {
                        BBox_Buffer = double.Parse(j[0].AttributValue),
                        OnlyPlanarFaces = bool.Parse(j[1].AttributValue),
                        CoordinatesReduction = bool.Parse(j[2].AttributValue),
                        PgmHeightOfLevel_Meter = double.Parse(j[3].AttributValue),
                        PgmImageExpansion_Px = double.Parse(j[4].AttributValue),
                        PgmImageResolution_Meter = double.Parse(j[5].AttributValue),
                        VerbosityLevel = j[6].AttributValue,
                        PathPointCloud = j[7].AttributValue,
                        PathCloudCompare = j[8].AttributValue,
                        PathDecap = j[9].AttributValue,
                        ServerUuid = j[10].AttributValue,
                        FragmentationVoxelResolution_Meter = double.Parse(j[11].AttributValue),
                        StepsPerFullTurn = int.Parse(j[12].AttributValue),
                        SphereDiameter_Meter = double.Parse(j[13].AttributValue),
                        HeightOfScanner_Meter = double.Parse(j[14].AttributValue),
                        NoiceOfScanner_Meter = double.Parse(j[15].AttributValue),
                        Beta_Degree = double.Parse(j[16].AttributValue),
                        MinDF_Meter = double.Parse(j[17].AttributValue),
                        MaxDF_Meter = double.Parse(j[18].AttributValue),
                        MaxPlaneDist_Meter = double.Parse(j[19].AttributValue),
                        GridSpacing_Meter = double.Parse(j[20].AttributValue),
                        GridColumns = int.Parse(j[21].AttributValue),
                        GridRows = int.Parse(j[22].AttributValue)
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
