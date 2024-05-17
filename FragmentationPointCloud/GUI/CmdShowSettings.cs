﻿using System;
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
                        BBox_Buffer = double.Parse(j[2].AttributValue),
                        OnlyPlanarFaces = bool.Parse(j[4].AttributValue),
                        CoordinatesReduction = bool.Parse(j[5].AttributValue),
                        PgmHeightOfLevel_Meter = double.Parse(j[8].AttributValue),
                        PgmImageExpansion_Px = double.Parse(j[9].AttributValue),
                        PgmImageResolution_Meter = double.Parse(j[10].AttributValue),
                        VerbosityLevel = j[11].AttributValue,
                        PathPointCloud = j[12].AttributValue,
                        FragmentationVoxelResolution_Meter = double.Parse(j[13].AttributValue)
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
