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
                        ConstructionTolerance_Meter = double.Parse(j[0].AttributValue),
                        AngleDeviation_Degree = double.Parse(j[1].AttributValue),
                        BBox_Buffer = double.Parse(j[2].AttributValue),

                        VisibilityAnalysis = bool.Parse(j[3].AttributValue),
                        OnlyPlanarFaces = bool.Parse(j[4].AttributValue),
                        CoordinatesReduction = bool.Parse(j[5].AttributValue),

                        CalculateShift = bool.Parse(j[6].AttributValue),
                        CalculateVertexDistance = bool.Parse(j[7].AttributValue),

                        PgmHeightOfLevel_Meter = double.Parse(j[8].AttributValue),
                        PgmImageExpansion_Px = double.Parse(j[9].AttributValue),
                        PgmImageResolution_Meter = double.Parse(j[10].AttributValue),

                        VerbosityLevel = j[11].AttributValue,
                        PathPointCloud = j[12].AttributValue,
                        FragmentationVoxelResolution_Meter = double.Parse(j[13].AttributValue),

                        CsvR = j[14].AttributValue,
                        CsvRRef = j[15].AttributValue,
                        CsvVisibleFaces = j[16].AttributValue,
                        CsvVisibleFacesRef = j[17].AttributValue,
                        CsvS = j[18].AttributValue,
                        CsvSRef = j[19].AttributValue,
                        CsvMatchesR = j[20].AttributValue,
                        CsvMatchesRRef = j[21].AttributValue,
                        CsvMatchesS = j[22].AttributValue,
                        CsvMatchesSRef = j[23].AttributValue,
                        ObjR = j[24].AttributValue,
                        ObjS = j[25].AttributValue,
                        StepsPerFullTurn = double.Parse(j[26].AttributValue),
                        Pointcloud = j[27].AttributValue,

                        FilterAngle_Degree = double.Parse(j[28].AttributValue),
                        FilterD_Meter = double.Parse(j[29].AttributValue),
                        FilterBuffer_Meter = double.Parse(j[30].AttributValue),

                        MaxPlaneTol_Degree = double.Parse(j[31].AttributValue),
                        MaxPlaneDist_Meter = double.Parse(j[32].AttributValue),
                        MinDistToStation_Meter = double.Parse(j[33].AttributValue),
                        MaxDistToStation_Meter = double.Parse(j[34].AttributValue),
                        MinCoverage_Percent = double.Parse(j[35].AttributValue),
                        MinPatchLength_Meter = double.Parse(j[36].AttributValue),
                        MaxPatchLength_Meter = double.Parse(j[37].AttributValue),

                        MaxDistToArc_Meter = double.Parse(j[38].AttributValue),
                        MaxRayPlaneDiffCos = double.Parse(j[39].AttributValue),

                        QuantilAlpha2_5 = double.Parse(j[40].AttributValue),
                        QuantilAlpha5 = double.Parse(j[41].AttributValue),

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
