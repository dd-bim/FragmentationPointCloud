using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JetBrains.Annotations;

using Revit.Green3DScan.SimulatePointCloud;

using Serilog;
using RD = Revit.Data;

namespace Revit.Green3DScan.SimulatePointCloud;

// TODO: Combine code with Bim2Stations.cs to avoid duplication of logic
[Transaction(TransactionMode.Manual)]
[UsedImplicitly]
public class Stations2PointClouds : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // Initialization
        if (!ExternalCommandHelper.GetProjectPath(commandData, out string projectPath, out var document, out var uiDocument))
        {
            TaskDialog.Show("Message", "The project file has not been saved yet.");
            return Result.Failed;
        }
        ExternalCommandHelper.InitLogger(projectPath);
        var settings = SettingsJson.ReadSettingsJson(Constants.pathSettings);

        Log.Information("start Stations2PointClouds");

        // Get transformation
        var transform = Helper.GetTransformation(document, settings);

        Log.Information("setup");

        #region select files

        if (uiDocument.ActiveView is View3D current3DView)
        {
            TaskDialog.Show("Message", "You must be in a 2D viewplan!");
            return Result.Failed;
        }

        // Revit
        var fodPfRevit = new FileOpenDialog(Stations.CsvFilter)
        {
            Title = "Select CSV file with BimFaces from Revit!"
        };
        if (fodPfRevit.Show() == ItemSelectionDialogResult.Canceled) return Result.Cancelled;
        string csvPathPfRevit = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodPfRevit.GetSelectedModelPath());

        var fodRpRevit = new FileOpenDialog(Stations.CsvFilter)
        {
            Title = "Select CSV file with BimFacesPlanes fromRevit!"
        };
        if (fodRpRevit.Show() == ItemSelectionDialogResult.Canceled) return Result.Cancelled;
        string csvPathRpRevit = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodRpRevit.GetSelectedModelPath());

        #endregion select files

        Log.Information("select files");

        #region read files

        var facesRevit = RD.PlanarFace.ReadCsv(csvPathPfRevit, out string[] lineErrors1, out string error1);

        var referencePlanesRevit =
            RD.ReferencePlane.ReadCsv(csvPathRpRevit, out string[] lineErrors2, out string error2);

        #endregion read files

        Log.Information("read files");

        #region stations

        var allStations = Stations.CollectFromFamilyInstances(document, transform);

        Log.Information("write stations csv");

        if(!Stations.WriteCsv(projectPath, allStations))
        {
            Log.Error("Error writing stations to CSV.");
            TaskDialog.Show("Error", "Error writing stations to CSV.");
            return Result.Failed;
        }

        #endregion stations

        Log.Information(allStations.Count + " stations");

        #region write pointcloud in XYZ

        _ = RayCasting.VisibleFaces(facesRevit, referencePlanesRevit, allStations, settings, out _, out var pointClouds, true);
        string csvPath = Stations.GetStationsDirectoryPath(projectPath);
        for (int i = 0; i < allStations.Count; i++)
        {
            var lines = new List<string>();

            // collect points of the current station
            for (int j = 0; j < pointClouds[i].Length; j++)
            {
                lines.Add(pointClouds[i][j].X.ToStringInvariant() + " "
                    + pointClouds[i][j].Y.ToStringInvariant() + " "
                    + pointClouds[i][j].Z.ToStringInvariant());
            }

            // create file for the current station and save points
            string xyzPath = Path.Combine(csvPath, $"Station_{i}.xyz");
            File.WriteAllLines(xyzPath, lines);

            // conversion with cloudcompare
            double tx = -allStations[i].X;
            double ty = -allStations[i].Y;
            double tz = -allStations[i].Z;

            // create the transformation matrix for station-centered point cloud
            string[] transformationLines =
            [
                "1 0 0 " + tx.ToStringInvariant(),
                "0 1 0 " + ty.ToStringInvariant(),
                "0 0 1 " + tz.ToStringInvariant(),
                "0 0 0 1"
            ];

            // path to transformation file
            string transformationFilePath = Path.Combine(csvPath, "transformation.txt");

            File.WriteAllLines(transformationFilePath, transformationLines);

            // path to E57
            string outputPointCloud = Path.Combine(csvPath, $"Station_{i}.e57");

            var cloudCompareProcess = new Process();

            // Configure the process object with the required arguments
            cloudCompareProcess.StartInfo.FileName = settings.PathCloudCompare;
            cloudCompareProcess.StartInfo.Arguments = "-SILENT -O \"" + xyzPath + "\" -APPLY_TRANS \"" +
                                                      transformationFilePath +
                                                      "\" -C_EXPORT_FMT E57 -SAVE_CLOUDS FILE \"" +
                                                      outputPointCloud + "\"";
            cloudCompareProcess.Start();
            cloudCompareProcess.WaitForExit();
        }

        #endregion write pointcloud in XYZ

        Log.Information("write point cloud in XYZ");
        TaskDialog.Show("Message", "finish");
        return Result.Succeeded;
    }
}