using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GeometryLib.D3;
using Serilog;
using Except = Autodesk.Revit.Exceptions;
using Sys = System.Globalization.CultureInfo;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using Path = System.IO.Path;

namespace Revit.Green3DScan.Fragmentation
{
    [Transaction(TransactionMode.Manual)]
    public class FragmentationSectionBoxComplete : IExternalCommand
    {
        private const string CsvHeader = "Oriented;StateId;ObjectGuid;ElementId;" +
                                         "BBoxMinX;BBoxMinY;BBoxMinZ;" +
                                         "BBoxMaxX;BBoxMaxY;BBoxMaxZ;" +
                                         "OBoxCenterX;OBoxCenterY;OBoxCenterZ;" +
                                         "OBoxXDirX;OBoxXDirY;OBoxXDirZ;" +
                                         "OBoxYDirX;OBoxYDirY;OBoxYDirZ;" +
                                         "OBoxZDirX;OBoxZDirY;OBoxZDirZ;" +
                                         "OBoxXHSize;OBoxYHSize;OBoxZHSize";

        private string dateBimLastModified;

        private string path;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region setup

            // settings json
            var set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            try
            {
                path = Path.GetDirectoryName(doc.PathName);
                var fileInfo = new FileInfo(path ?? throw new InvalidOperationException());
                DateTime date = fileInfo.LastWriteTime;
                dateBimLastModified =
                    date.Year + "-" + date.Month + "-" + date.Day + "-" + date.Hour + "-" + date.Minute;
            }
            catch (Exception)
            {
                TaskDialog.Show("Message", "The file has not been saved yet.");
                return Result.Failed;
            }

            // logger
            string logsPath = Path.Combine(path, "00_Logs/");
            if (!Directory.Exists(logsPath)) Directory.CreateDirectory(logsPath);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(logsPath, "LogFile_"), rollingInterval: RollingInterval.Minute)
                .CreateLogger();
            Log.Information("start FragmentationSectionBoxComplete");
            Log.Information(set.BBox_Buffer.ToString(Sys.InvariantCulture));

            #endregion setup

            Transform trans = Helper.GetTransformation(doc, set, out CoordinateSystem _);
            Transform transInverse = trans.Inverse;

            string sectionBoxPath = Path.Combine(path, "09_FragmentationSectionBox\\");
            string rcpOutputPath = Path.Combine(path, "09_FragmentationSectionBox");
            if (!Directory.Exists(sectionBoxPath)) Directory.CreateDirectory(sectionBoxPath);
            string sectionBoxName = GetSectionBoxName(sectionBoxPath, "SectionBox");

            string csvBBoxesPath = Path.Combine(sectionBoxPath, sectionBoxName + ".csv");
            using StreamWriter csv = File.CreateText(csvBBoxesPath);
            csv.WriteLine(CsvHeader);
            var oBBoxes = new List<OrientedBoundingBox>();

            const bool oriented = true;
            // SectionBox can only be rotated horizontally
            var directionZ = new XYZ(0, 0, 1);

            try
            {
                if (!(uiDoc.ActiveView is View3D current3DView))
                {
                    TaskDialog.Show("Message", "You must be in a 3D view!");
                    return Result.Failed;
                }

                BoundingBoxXYZ sectionBox = current3DView.GetSectionBox();

                // SectionBox has its own transformation
                Transform transSectionBox = sectionBox.Transform;
                XYZ bBoxMin = trans.OfPoint(transSectionBox.OfPoint(sectionBox.Min)) * Constants.feet2Meter;
                XYZ bBoxMaxPfeil = trans.OfPoint(transSectionBox.OfPoint(sectionBox.Max)) * Constants.feet2Meter;

                // center SectionBox
                XYZ center3D = (bBoxMaxPfeil + bBoxMin) / 2;

                double halfLength = Math.Abs(sectionBox.Max.X - sectionBox.Min.X) / 2 * Constants.feet2Meter;
                double halfWidth = Math.Abs(sectionBox.Max.Y - sectionBox.Min.Y) / 2 * Constants.feet2Meter;
                double halfHeight = Math.Abs(sectionBox.Max.Z - sectionBox.Min.Z) / 2 * Constants.feet2Meter;

                // Angle of rotation around the z-axis
                XYZ xAxis = transSectionBox.BasisX;
                XYZ yAxis = transSectionBox.BasisY;

                double rotationAngle = Math.Atan2(yAxis.Y, xAxis.Y) * (180 / Math.PI) - 90;
                if (rotationAngle < 0) rotationAngle += 360;
                double rotationAngleRadians = rotationAngle * (Math.PI / 180.0);

                XYZ directionX = trans.OfVector(new XYZ(Math.Cos(rotationAngleRadians), -Math.Sin(rotationAngleRadians), 0));
                XYZ directionY = directionZ.CrossProduct(directionX);

                oBBoxes.Add(new OrientedBoundingBox(center3D, directionX, directionY, new XYZ(0, 0, 1), halfLength,
                    halfWidth, halfHeight));

                csv.WriteLine(oriented + ";" + sectionBoxName + ";" + sectionBoxName + ";" + sectionBoxName + ";"
                              + Math.Round(bBoxMin.X, 4).ToString(Sys.InvariantCulture) + ";" +
                              Math.Round(bBoxMin.Y, 4).ToString(Sys.InvariantCulture) + ";" +
                              Math.Round(bBoxMin.Z, 4).ToString(Sys.InvariantCulture) + ";"
                              + Math.Round(bBoxMaxPfeil.X, 4).ToString(Sys.InvariantCulture) + ";" +
                              Math.Round(bBoxMaxPfeil.Y, 4).ToString(Sys.InvariantCulture) + ";" +
                              Math.Round(bBoxMaxPfeil.Z, 4).ToString(Sys.InvariantCulture) + ";"
                              + Math.Round(center3D.X, 4).ToString(Sys.InvariantCulture) + ";" +
                              Math.Round(center3D.Y, 4).ToString(Sys.InvariantCulture) + ";" +
                              Math.Round(center3D.Z, 4).ToString(Sys.InvariantCulture) + ";"
                              + directionX.X.ToString(Sys.InvariantCulture) + ";" +
                              directionX.Y.ToString(Sys.InvariantCulture) + ";" +
                              directionX.Z.ToString(Sys.InvariantCulture) + ";"
                              + directionY.X.ToString(Sys.InvariantCulture) + ";" +
                              directionY.Y.ToString(Sys.InvariantCulture) + ";" +
                              directionY.Z.ToString(Sys.InvariantCulture) + ";"
                              + directionZ.X.ToString(Sys.InvariantCulture) + ";" +
                              directionZ.Y.ToString(Sys.InvariantCulture) + ";" +
                              directionZ.Z.ToString(Sys.InvariantCulture) + ";"
                              + Math.Round(halfLength, 4).ToString(Sys.InvariantCulture) + ";" +
                              Math.Round(halfWidth, 4).ToString(Sys.InvariantCulture) + ";" +
                              Math.Round(halfHeight, 4).ToString(Sys.InvariantCulture));

                csv.Close();

                WriteOBBoxToOBJFile(oBBoxes, Path.Combine(sectionBoxPath, sectionBoxName + ".obj"));

                // step 1: Fragmentation and saving of the small PCD
                string exeGreen3DPath = Constants.exeFragmentationBBox;

                var command = $"{set.PathPointCloud} {csvBBoxesPath} {sectionBoxPath} {dateBimLastModified}";
                if (!Helper.Fragmentation2Pcd(exeGreen3DPath, command))
                {
                    TaskDialog.Show("Message", "Fragmentation error");
                    return Result.Failed;
                }

                // step 2: CloudCompare PCD --> E57 
                if (!Helper.Pcd2e57(Path.Combine(sectionBoxPath, sectionBoxName + ".pcd"),
                        Path.Combine(sectionBoxPath, sectionBoxName + ".e57"), set))
                {
                    TaskDialog.Show("Message", "CloudCompare error");
                    return Result.Failed;
                }

                // step 3: DeCap E57 --> RCP
                if (!Helper.DeCap(rcpOutputPath, sectionBoxName,
                        Path.Combine(sectionBoxPath + sectionBoxName + ".e57")))
                {
                    TaskDialog.Show("Message", "DeCap error");
                    return Result.Failed;
                }

                // load rcp
                if (!LoadPointCloud(doc, Path.Combine(sectionBoxPath, sectionBoxName + ".rcp"), transInverse))
                {
                    TaskDialog.Show("Message", "RCP file not available");
                    return Result.Failed;
                }

                View view = uiDoc.ActiveView;
                try
                {
                    var tx = new Transaction(doc, "Load RCP");
                    tx.Start();
                    view.ArePointCloudsHidden = false;
                    tx.Commit();
                }
                catch (Exception)
                {
                    TaskDialog.Show("Message", "Error when setting the visibility of point clouds");
                }

                TaskDialog.Show("Message", "SectionBox Fragmentation successful!");
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

        private static string GetSectionBoxName(string path, string fileName)
        {
            var number = 1;
            string newName = fileName + number;
            string newFile = newName + ".csv";

            while (File.Exists(Path.Combine(path, newFile)))
            {
                number++;
                newName = fileName + number;
                newFile = newName + ".csv";
            }

            return newName;
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

        private bool LoadPointCloud(Document doc, string pointCloudFilePath, Transform trans)
        {
            try
            {
                var tx = new Transaction(doc, "Load RCP");
                tx.Start();
                var type = PointCloudType.Create(doc, "rcp", pointCloudFilePath);
                PointCloudInstance.Create(doc, type.Id, trans);
                tx.Commit();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private class OrientedBoundingBox
        {
            public OrientedBoundingBox(XYZ center, XYZ xDir, XYZ yDir, XYZ zDir, double halfLength, double halfWidth,
                double halfHeight)
            {
                Center = center;
                XDirection = xDir;
                YDirection = yDir;
                ZDirection = zDir;
                HalfLength = halfLength;
                HalfWidth = halfWidth;
                HalfHeight = halfHeight;
            }

            public XYZ Center { get; }
            public XYZ XDirection { get; }
            public XYZ YDirection { get; }
            public XYZ ZDirection { get; }
            public double HalfLength { get; }
            public double HalfWidth { get; }
            public double HalfHeight { get; }
        }
    }
}