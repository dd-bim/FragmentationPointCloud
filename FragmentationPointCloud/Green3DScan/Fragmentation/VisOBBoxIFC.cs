using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Serilog;
using Except = Autodesk.Revit.Exceptions;
using Sys = System.Globalization.CultureInfo;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace Revit.Green3DScan.Fragmentation
{
    [Transaction(TransactionMode.Manual)]
    public class VisOBBoxIFC : IExternalCommand
    {
        private static bool ReadCsvBoxes(string csvPathBBoxes, out List<OrientedBoundingBox> listOBBox)
        {
            var list = new List<OrientedBoundingBox>();
            try
            {
                using (var reader = new StreamReader(csvPathBBoxes))
                {
                    reader.ReadLine();
                    while (reader.ReadLine() is { } line)
                    {
                        string[] columns = line.Split(';');

                        // Check if the number of columns is correct --> IFC requires 23 columns!!
                        if (columns.Length == 23)
                            list.Add(new OrientedBoundingBox(columns[0], columns[1], "0",
                                new XYZ(double.Parse(columns[8], Sys.InvariantCulture),
                                    double.Parse(columns[9], Sys.InvariantCulture),
                                    double.Parse(columns[10], Sys.InvariantCulture)),
                                new XYZ(double.Parse(columns[11], Sys.InvariantCulture),
                                    double.Parse(columns[12], Sys.InvariantCulture),
                                    double.Parse(columns[13], Sys.InvariantCulture)),
                                new XYZ(double.Parse(columns[14], Sys.InvariantCulture),
                                    double.Parse(columns[15], Sys.InvariantCulture),
                                    double.Parse(columns[16], Sys.InvariantCulture)),
                                new XYZ(double.Parse(columns[17], Sys.InvariantCulture),
                                    double.Parse(columns[18], Sys.InvariantCulture),
                                    double.Parse(columns[19], Sys.InvariantCulture)),
                                double.Parse(columns[20], Sys.InvariantCulture),
                                double.Parse(columns[21], Sys.InvariantCulture),
                                double.Parse(columns[22], Sys.InvariantCulture)));
                        else
                            TaskDialog.Show("Message", "Reading csv not successful " + line + " " + columns.Length);
                    }
                }

                listOBBox = list;
                return true;
            }
            catch (Exception)
            {
                listOBBox = list;
                return false;
            }
        }

        private static void WriteOBBoxToOBJFile(List<OrientedBoundingBox> oboxes, string filePath)
        {
            using var objFile = new StreamWriter(filePath);
            var indexOffset = 0;

            foreach (OrientedBoundingBox obox in oboxes)
            {
                var points = new XYZ[8];
                points[0] = obox.Center - obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth -
                            obox.ZDirection * obox.HalfHeight; // Punkt 1
                points[1] = obox.Center + obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth -
                            obox.ZDirection * obox.HalfHeight; // Punkt 2
                points[2] = obox.Center + obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth -
                            obox.ZDirection * obox.HalfHeight; // Punkt 3
                points[3] = obox.Center - obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth -
                            obox.ZDirection * obox.HalfHeight; // Punkt 4
                points[4] = obox.Center - obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth +
                            obox.ZDirection * obox.HalfHeight; // Punkt 5
                points[5] = obox.Center + obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth +
                            obox.ZDirection * obox.HalfHeight; // Punkt 6
                points[6] = obox.Center + obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth +
                            obox.ZDirection * obox.HalfHeight; // Punkt 7
                points[7] = obox.Center - obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth +
                            obox.ZDirection * obox.HalfHeight; // Punkt 8

                foreach (XYZ point in points)
                {
                    objFile.WriteLine(
                        $"v {point.X.ToString(Sys.InvariantCulture)} {point.Y.ToString(Sys.InvariantCulture)} {point.Z.ToString(Sys.InvariantCulture)}");
                }
            }

            foreach (OrientedBoundingBox unused in oboxes)
            {
                int v0 = 1 + indexOffset;
                int v1 = 2 + indexOffset;
                int v2 = 3 + indexOffset;
                int v3 = 4 + indexOffset;
                int v4 = 5 + indexOffset;
                int v5 = 6 + indexOffset;
                int v6 = 7 + indexOffset;
                int v7 = 8 + indexOffset;

                objFile.WriteLine("f " + v0 + " " + v1 + " " + v2 + " " + v3);
                objFile.WriteLine("f " + v4 + " " + v5 + " " + v6 + " " + v7);
                objFile.WriteLine("f " + v0 + " " + v1 + " " + v5 + " " + v4);
                objFile.WriteLine("f " + v1 + " " + v2 + " " + v6 + " " + v5);
                objFile.WriteLine("f " + v2 + " " + v3 + " " + v7 + " " + v6);
                objFile.WriteLine("f " + v3 + " " + v0 + " " + v4 + " " + v7);

                indexOffset += 8;
            }
        }

        private class OrientedBoundingBox(
            string stateId,
            string objectGuid,
            string elementId,
            XYZ center,
            XYZ xDir,
            XYZ yDir,
            XYZ zDir,
            double halfLength,
            double halfWidth,
            double halfHeight)
        {
            public XYZ Center { get; } = center;
            public XYZ XDirection { get; } = xDir;
            public XYZ YDirection { get; } = yDir;
            public XYZ ZDirection { get; } = zDir;
            public double HalfLength { get; } = halfLength;
            public double HalfWidth { get; } = halfWidth;
            public double HalfHeight { get; } = halfHeight;
        }

        #region Execute

        private string _path;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region setup

            // settings json
            var set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            try
            {
                _path = Path.GetDirectoryName(doc.PathName);
            }
            catch (Exception)
            {
                TaskDialog.Show("Message", "The file has not been saved yet.");
                return Result.Failed;
            }

            // logger
            string logsPath = Path.Combine(_path!, "00_Logs/");
            if (!Directory.Exists(logsPath)) Directory.CreateDirectory(logsPath);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(logsPath, "LogFile_"), rollingInterval: RollingInterval.Minute)
                .CreateLogger();
            Log.Information("start VisOBBoxIFC");
            Log.Information(set.BBox_Buffer.ToString(Sys.InvariantCulture));

            #endregion setup

            try
            {
                // BBoxes
                var fodBBox = new FileOpenDialog("CSV file (*.csv)|*.csv");
                fodBBox.Title = "Select CSV file with BBoxes from Revit!";
                if (fodBBox.Show() == ItemSelectionDialogResult.Canceled) return Result.Cancelled;
                string csvPathBBoxes = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodBBox.GetSelectedModelPath());

                // read CSV
                if (!ReadCsvBoxes(csvPathBBoxes, out var obboxes))
                    TaskDialog.Show("Message", "Reading csv not successful");
                TaskDialog.Show("Message", obboxes.Count + " OBBoxes were exported.");

                WriteOBBoxToOBJFile(obboxes, Path.Combine(_path, "OBBoxesIFC.obj"));
                return Result.Succeeded;
            }

            #region catch

            catch (Except.OperationCanceledException)
            {
                TaskDialog.Show("Message", "Error 1: Command canceled.");
                return Result.Failed;
            }
            catch (Except.ForbiddenForDynamicUpdateException)
            {
                TaskDialog.Show("Message", "Error 2");
                return Result.Failed;
            }
            catch (Exception ex)
            {
                message += "Error message::" + ex;
                TaskDialog.Show("Message", message);
                return Result.Failed;
            }

            #endregion catch
        }

        #endregion execute
    }
}