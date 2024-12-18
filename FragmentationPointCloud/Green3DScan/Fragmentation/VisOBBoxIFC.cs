﻿using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Except = Autodesk.Revit.Exceptions;
using Serilog;
using Sys = System.Globalization.CultureInfo;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class VisOBBoxIFC : IExternalCommand
    {
        #region Execute
        string path;
        string dateBimLastModified;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region setup
            // settings json
            SettingsJson set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

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
            string logsPath = Path.Combine(path, "00_Logs/");
            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File(Path.Combine(logsPath, "LogFile_"), rollingInterval: RollingInterval.Minute)
               .CreateLogger();
            Log.Information("start VisOBBoxIFC");
            Log.Information(set.BBox_Buffer.ToString());
            #endregion setup

            try
            {
                // BBoxes
                FileOpenDialog fodBBox = new FileOpenDialog("CSV file (*.csv)|*.csv");
                fodBBox.Title = "Select CSV file with BBoxes from Revit!";
                if (fodBBox.Show() == ItemSelectionDialogResult.Canceled)
                {
                    return Result.Cancelled;
                }
                string csvPathBBoxes = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodBBox.GetSelectedModelPath());

                // read CSV
                if (!ReadCsvBoxes(csvPathBBoxes, out List<OrientedBoundingBox> obboxes))
                {
                    TaskDialog.Show("Message", "Reading csv not successful");
                }
                TaskDialog.Show("Message", obboxes.Count + " OBBoxes were exported.");

                WriteOBBoxToOBJFile(obboxes, Path.Combine(path, "OBBoxesIFC.obj"));
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
                message += "Error message::" + ex.ToString();
                TaskDialog.Show("Message", message);
                return Result.Failed;
            }
            #endregion catch
        }
        #endregion execute
        public class OrientedBoundingBox
        {
            public string StateId { get; }
            public string ObjectGuid { get; }
            public string ElementId { get; }
            public XYZ Center { get; }
            public XYZ XDirection { get; }
            public XYZ YDirection { get; }
            public XYZ ZDirection { get; }
            public double HalfLength { get; }
            public double HalfWidth { get; }
            public double HalfHeight { get; }

            public OrientedBoundingBox(string stateId, string objectGuid, string elementId, XYZ center, XYZ xDir, XYZ yDir, XYZ zDir, double halfLength, double halfWidth, double halfHeight)
            {
                StateId = stateId;
                ObjectGuid = objectGuid;
                ElementId = elementId;
                Center = center;
                XDirection = xDir;
                YDirection = yDir;
                ZDirection = zDir;
                HalfLength = halfLength;
                HalfWidth = halfWidth;
                HalfHeight = halfHeight;
            }
        }
        private bool ReadCsvBoxes(string csvPathBBoxes, out List<OrientedBoundingBox> listOBBox)
        {
            List<OrientedBoundingBox> list = new List<OrientedBoundingBox>();
            try
            {
                using (StreamReader reader = new StreamReader(csvPathBBoxes))
                {
                    reader.ReadLine();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] columns = line.Split(';');

                        // Check if the number of columns is correct --> IFC requires 23 columns!!
                        if (columns.Length == 23)
                        {
                            list.Add(new OrientedBoundingBox(columns[0], columns[1], "0",
                                new XYZ(double.Parse(columns[8], CultureInfo.InvariantCulture), double.Parse(columns[9], CultureInfo.InvariantCulture), double.Parse(columns[10], CultureInfo.InvariantCulture)),
                                new XYZ(double.Parse(columns[11], CultureInfo.InvariantCulture), double.Parse(columns[12], CultureInfo.InvariantCulture), double.Parse(columns[13], CultureInfo.InvariantCulture)),
                                new XYZ(double.Parse(columns[14], CultureInfo.InvariantCulture), double.Parse(columns[15], CultureInfo.InvariantCulture), double.Parse(columns[16], CultureInfo.InvariantCulture)),
                                new XYZ(double.Parse(columns[17], CultureInfo.InvariantCulture), double.Parse(columns[18], CultureInfo.InvariantCulture), double.Parse(columns[19], CultureInfo.InvariantCulture)),
                                double.Parse(columns[20], CultureInfo.InvariantCulture), double.Parse(columns[21], CultureInfo.InvariantCulture), double.Parse(columns[22], CultureInfo.InvariantCulture)));
                        }
                        else
                        {
                            TaskDialog.Show("Message", "Reading csv not successful " + line + " " + columns.Length);
                        }
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
        public void WriteOBBoxToOBJFile(List<OrientedBoundingBox> oboxes, string filePath)
        {
            using StreamWriter objFile = new StreamWriter(filePath);
            int indexOffset = 0;

            foreach (OrientedBoundingBox obox in oboxes)
            {
                XYZ[] points = new XYZ[8];
                points[0] = obox.Center - obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth - obox.ZDirection * obox.HalfHeight; // Punkt 1
                points[1] = obox.Center + obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth - obox.ZDirection * obox.HalfHeight; // Punkt 2
                points[2] = obox.Center + obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth - obox.ZDirection * obox.HalfHeight; // Punkt 3
                points[3] = obox.Center - obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth - obox.ZDirection * obox.HalfHeight; // Punkt 4
                points[4] = obox.Center - obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth + obox.ZDirection * obox.HalfHeight; // Punkt 5
                points[5] = obox.Center + obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth + obox.ZDirection * obox.HalfHeight; // Punkt 6
                points[6] = obox.Center + obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth + obox.ZDirection * obox.HalfHeight; // Punkt 7
                points[7] = obox.Center - obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth + obox.ZDirection * obox.HalfHeight; // Punkt 8

                foreach (XYZ point in points)
                {
                    objFile.WriteLine($"v {point.X.ToString(Sys.InvariantCulture)} {point.Y.ToString(Sys.InvariantCulture)} {point.Z.ToString(Sys.InvariantCulture)}");
                }
            }

            foreach (OrientedBoundingBox obox in oboxes)
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
    }
}