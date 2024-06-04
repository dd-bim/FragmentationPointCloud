using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Except = Autodesk.Revit.Exceptions;
using Serilog;
using D3 = GeometryLib.Double.D3;
using Autodesk.Revit.UI.Selection;
using Sys = System.Globalization.CultureInfo;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using Path = System.IO.Path;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class FragmentationBBoxComplete : IExternalCommand
    {
        public const string CsvHeader = "Oriented;StateId;ObjectGuid;ElementId;" +
            "BBoxMinX;BBoxMinY;BBoxMinZ;" +
            "BBoxMaxX;BBoxMaxY;BBoxMaxZ;" +
            "OBoxCenterX;OBoxCenterY;OBoxCenterZ;" +
            "OBoxXDirX;OBoxXDirY;OBoxXDirZ;" +
            "OBoxYDirX;OBoxYDirY;OBoxYDirZ;" +
            "OBoxZDirX;OBoxZDirY;OBoxZDirZ;" +
            "OBoxXHSize;OBoxYHSize;OBoxZHSize";
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
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File(Path.Combine(path, "LogFile_"), rollingInterval: RollingInterval.Day)
               .CreateLogger();
            Log.Information("start FragmentationBBoxComplete");
            Log.Information(set.BBox_Buffer.ToString());
            #endregion setup

            string pcdPathPointcloud = set.PathPointCloud;
            string rcpFilePath = Path.Combine(path, "08_FragmentationBBoxComplete\\");
            if (!Directory.Exists(rcpFilePath))
            {
                Directory.CreateDirectory(rcpFilePath);
            }

            Transform trans = Helper.GetTransformation(doc, set, out var crs);
            Transform transInverse = trans.Inverse;

            using StreamWriter csv = File.CreateText(Path.Combine(rcpFilePath, "BIM_BBoxes.csv"));
            csv.WriteLine(CsvHeader);
            List<Helper.BoundingBox> bBoxes = new List<Helper.BoundingBox>();
            List<Helper.OrientedBoundingBox> oBBoxes = new List<Helper.OrientedBoundingBox>();

            try
            {
                IList<Reference> pickedObjects = uidoc.Selection.PickObjects(ObjectType.Element, "Select components whose oriented bounding boxes are to be output.");

                foreach (Reference reference in pickedObjects)
                {
                    bool oriented = false;
                    double halfLength = default;
                    double halfWidth = default;
                    double halfHeight = default;
                    string ifcGuid = default;
                    XYZ center3D = new XYZ(0, 0, 0);
                    XYZ directionX = new XYZ(0, 0, 0);
                    XYZ directionY = new XYZ(0, 0, 0);
                    XYZ bBoxMin = new XYZ(0, 0, 0);
                    XYZ bBoxMax = new XYZ(0, 0, 0);
                    // orientated BBox is only rotated horizontally
                    XYZ directionZ = new XYZ(0, 0, 1);

                    View currentView = doc.ActiveView;
                    if (!GetGeometryElement(doc, reference, out ElementId eleId, out GeometryElement geomElement, out string createStateId, out string demolishedStateId, out string objectId, out Category cat))
                    {
                        Log.Information("skipped building component");
                        continue;
                    }
                    Element element = doc.GetElement(eleId);
                    // conversion to IFC GUID
                    ifcGuid = Helper.ToIfcGuid(Helper.ToGuid(element.UniqueId));

                    // bbox
                    BoundingBoxXYZ bBox = element.get_BoundingBox(currentView);
                    bBoxMin = trans.OfPoint(bBox.Min) * Constants.feet2Meter;
                    bBoxMax = trans.OfPoint(bBox.Max) * Constants.feet2Meter;
                    // high of bbox
                    var high = bBox.Max.Z - bBox.Min.Z;

                    if (element is Wall wall)
                    {
                        if (element.Location is LocationCurve locationCurve)
                        {
                            Curve curve = locationCurve.Curve;
                            if (curve is Line line)
                            {
                                // direction of line
                                directionX = trans.OfVector(line.Direction).Normalize();
                                directionY = directionX.CrossProduct(directionZ);
                                halfLength = line.Length / 2 * Constants.feet2Meter + set.BBox_Buffer;
                                halfHeight = high / 2 * Constants.feet2Meter + set.BBox_Buffer;
                                // center of bbox
                                var center2D = (line.GetEndPoint(0) + line.GetEndPoint(1)) / 2;

                                center3D = trans.OfPoint(center2D + new XYZ(0, 0, 0.5 * high)) * Constants.feet2Meter;

                                if (doc.GetElement(wall.GetTypeId()) is WallType wallType)
                                {
                                    ParameterSet parameters = wallType.Parameters;
                                    foreach (Parameter param in parameters)
                                    {
                                        string parameterName = param.Definition.Name;
                                        if (parameterName == "Breite")
                                        {
                                            double paramValue = param.AsDouble();
                                            halfWidth = paramValue / 2 * Constants.feet2Meter + set.BBox_Buffer;
                                        }
                                    }
                                }
                                oriented = true;
                                oBBoxes.Add(new Helper.OrientedBoundingBox(oriented, "stateId", objectId, ifcGuid, center3D, directionX, directionY, new XYZ(0, 0, 1), halfLength, halfWidth, halfHeight));

                                csv.WriteLine(oriented.ToString() + ";" + createStateId + "|" + demolishedStateId + ";" + ifcGuid + ";" + eleId + ";"
                                + Math.Round(bBoxMin.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMin.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMin.Z, 4).ToString(Sys.InvariantCulture) + ";"
                                + Math.Round(bBoxMax.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMax.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMax.Z, 4).ToString(Sys.InvariantCulture) + ";"
                                + Math.Round(center3D.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(center3D.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(center3D.Z, 4).ToString(Sys.InvariantCulture) + ";"
                                + directionX.X.ToString(Sys.InvariantCulture) + ";" + directionX.Y.ToString(Sys.InvariantCulture) + ";" + directionX.Z.ToString(Sys.InvariantCulture) + ";"
                                + directionY.X.ToString(Sys.InvariantCulture) + ";" + directionY.Y.ToString(Sys.InvariantCulture) + ";" + directionY.Z.ToString(Sys.InvariantCulture) + ";"
                                + directionZ.X.ToString(Sys.InvariantCulture) + ";" + directionZ.Y.ToString(Sys.InvariantCulture) + ";" + directionZ.Z.ToString(Sys.InvariantCulture) + ";"
                                + Math.Round(halfLength, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(halfWidth, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(halfHeight, 4).ToString(Sys.InvariantCulture));
                            }
                            else
                            {
                                Log.Information("Wall, but the LocationCurve is not a line.");
                            }
                        }
                        else
                        {
                            Log.Information("Wall, but LocationCurve not existing.");
                        }
                    }
                    else if (element.Category != null && element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming)
                    {
                        if (element.Location is LocationCurve locationCurve)
                        {
                            Curve curve = locationCurve.Curve;
                            if (curve is Line line)
                            {
                                // direction  der Line
                                directionX = trans.OfVector(line.Direction).Normalize();
                                directionY = directionX.CrossProduct(directionZ);
                                halfLength = line.Length / 2 * Constants.feet2Meter + set.BBox_Buffer;
                                halfHeight = high / 2 * Constants.feet2Meter + set.BBox_Buffer;
                                // Zentrum der OBBox
                                var center2D = (line.GetEndPoint(0) + line.GetEndPoint(1)) / 2;

                                center3D = trans.OfPoint(center2D - new XYZ(0, 0, 0.5 * high)) * Constants.feet2Meter;

                                var x = doc.GetElement(element.GetTypeId());
                                ParameterSet parameters = x.Parameters;
                                foreach (Parameter param in parameters)
                                {
                                    string parameterName = param.Definition.Name;
                                    if (parameterName == "Width" || parameterName == "Breite")
                                    {
                                        double paramValue = param.AsDouble();
                                        halfWidth = paramValue / 2 * Constants.feet2Meter + set.BBox_Buffer;
                                    }
                                }
                                oriented = true;
                                oBBoxes.Add(new Helper.OrientedBoundingBox(oriented, "stateId", objectId, ifcGuid, center3D, directionX, directionY, new XYZ(0, 0, 1), halfLength, halfWidth, halfHeight));

                                csv.WriteLine(oriented.ToString() + ";" + createStateId + "|" + demolishedStateId + ";" + ifcGuid + ";" + eleId + ";"
                                + Math.Round(bBoxMin.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMin.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMin.Z, 4).ToString(Sys.InvariantCulture) + ";"
                                + Math.Round(bBoxMax.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMax.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMax.Z, 4).ToString(Sys.InvariantCulture) + ";"
                                + Math.Round(center3D.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(center3D.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(center3D.Z, 4).ToString(Sys.InvariantCulture) + ";"
                                + directionX.X.ToString(Sys.InvariantCulture) + ";" + directionX.Y.ToString(Sys.InvariantCulture) + ";" + directionX.Z.ToString(Sys.InvariantCulture) + ";"
                                + directionY.X.ToString(Sys.InvariantCulture) + ";" + directionY.Y.ToString(Sys.InvariantCulture) + ";" + directionY.Z.ToString(Sys.InvariantCulture) + ";"
                                + directionZ.X.ToString(Sys.InvariantCulture) + ";" + directionZ.Y.ToString(Sys.InvariantCulture) + ";" + directionZ.Z.ToString(Sys.InvariantCulture) + ";"
                                + Math.Round(halfLength, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(halfWidth, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(halfHeight, 4).ToString(Sys.InvariantCulture));
                            }
                            else
                            {
                                Log.Information("Wall, but the LocationCurve is not a line.");
                                bBoxes.Add(new Helper.BoundingBox(bBoxMin, bBoxMax));
                                oriented = false;
                                csv.WriteLine(oriented.ToString() + ";" + createStateId + "|" + demolishedStateId + ";" + ifcGuid + ";" + eleId + ";"
                                + Math.Round(bBoxMin.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMin.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMin.Z, 4).ToString(Sys.InvariantCulture) + ";"
                                + Math.Round(bBoxMax.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMax.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMax.Z, 4).ToString(Sys.InvariantCulture) + ";"
                                + Math.Round(center3D.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(center3D.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(center3D.Z, 4).ToString(Sys.InvariantCulture) + ";"
                                + directionX.X.ToString(Sys.InvariantCulture) + ";" + directionX.Y.ToString(Sys.InvariantCulture) + ";" + directionX.Z.ToString(Sys.InvariantCulture) + ";"
                                + directionY.X.ToString(Sys.InvariantCulture) + ";" + directionY.Y.ToString(Sys.InvariantCulture) + ";" + directionY.Z.ToString(Sys.InvariantCulture) + ";"
                                + directionZ.X.ToString(Sys.InvariantCulture) + ";" + directionZ.Y.ToString(Sys.InvariantCulture) + ";" + directionZ.Z.ToString(Sys.InvariantCulture) + ";"
                                + Math.Round(halfLength, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(halfWidth, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(halfHeight, 4).ToString(Sys.InvariantCulture));
                            }
                        }
                        else
                        {
                            Log.Information("Wall, but LocationCurve not existing.");
                            bBoxes.Add(new Helper.BoundingBox(bBoxMin, bBoxMax));
                            oriented = false;
                            csv.WriteLine(oriented.ToString() + ";" + createStateId + "|" + demolishedStateId + ";" + ifcGuid + ";" + eleId + ";"
                            + Math.Round(bBoxMin.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMin.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMin.Z, 4).ToString(Sys.InvariantCulture) + ";"
                            + Math.Round(bBoxMax.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMax.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMax.Z, 4).ToString(Sys.InvariantCulture) + ";"
                            + Math.Round(center3D.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(center3D.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(center3D.Z, 4).ToString(Sys.InvariantCulture) + ";"
                            + directionX.X.ToString(Sys.InvariantCulture) + ";" + directionX.Y.ToString(Sys.InvariantCulture) + ";" + directionX.Z.ToString(Sys.InvariantCulture) + ";"
                            + directionY.X.ToString(Sys.InvariantCulture) + ";" + directionY.Y.ToString(Sys.InvariantCulture) + ";" + directionY.Z.ToString(Sys.InvariantCulture) + ";"
                            + directionZ.X.ToString(Sys.InvariantCulture) + ";" + directionZ.Y.ToString(Sys.InvariantCulture) + ";" + directionZ.Z.ToString(Sys.InvariantCulture) + ";"
                            + Math.Round(halfLength, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(halfWidth, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(halfHeight, 4).ToString(Sys.InvariantCulture));
                        }
                    }
                    else
                    {
                        Log.Information("Building segment is not a Wall or StructuralFraming, but: " + element.Category.ToString());
                        bBoxes.Add(new Helper.BoundingBox(bBoxMin, bBoxMax));
                        oriented = false;
                        csv.WriteLine(oriented.ToString() + ";" + createStateId + "|" + demolishedStateId + ";" + ifcGuid + ";" + eleId + ";"
                        + Math.Round(bBoxMin.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMin.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMin.Z, 4).ToString(Sys.InvariantCulture) + ";"
                        + Math.Round(bBoxMax.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMax.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(bBoxMax.Z, 4).ToString(Sys.InvariantCulture) + ";"
                        + Math.Round(center3D.X, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(center3D.Y, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(center3D.Z, 4).ToString(Sys.InvariantCulture) + ";"
                        + directionX.X.ToString(Sys.InvariantCulture) + ";" + directionX.Y.ToString(Sys.InvariantCulture) + ";" + directionX.Z.ToString(Sys.InvariantCulture) + ";"
                        + directionY.X.ToString(Sys.InvariantCulture) + ";" + directionY.Y.ToString(Sys.InvariantCulture) + ";" + directionY.Z.ToString(Sys.InvariantCulture) + ";"
                        + directionZ.X.ToString(Sys.InvariantCulture) + ";" + directionZ.Y.ToString(Sys.InvariantCulture) + ";" + directionZ.Z.ToString(Sys.InvariantCulture) + ";"
                        + Math.Round(halfLength, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(halfWidth, 4).ToString(Sys.InvariantCulture) + ";" + Math.Round(halfHeight, 4).ToString(Sys.InvariantCulture));
                    }
                }

                csv.Close();

                WriteOBBoxToOBJFile(oBBoxes, System.IO.Path.Combine(rcpFilePath, "OBBoxes.obj"));
                WriteBBoxToOBJFile(bBoxes, Path.Combine(rcpFilePath, "BBoxes.obj"));

                string csvPathBBoxes = Path.Combine(rcpFilePath, "BIM_BBoxes.csv");

                // CSV lesen
                if (!ReadCsvBoxes(csvPathBBoxes, out List<Helper.OrientedBoundingBox> obboxes))
                {
                    TaskDialog.Show("Message", "Reading csv failed");
                    return Result.Failed;
                }

                // step 2: fragmentation an save small pcd
                string exeGreen3DPath = Constants.exeFragmentationBBox;

                string command = $"{pcdPathPointcloud} {csvPathBBoxes} {rcpFilePath} {dateBimLastModified}";
                if (!Helper.Fragmentation2Pcd(exeGreen3DPath, command))
                {
                    TaskDialog.Show("Message", "Fragmentation error");
                    return Result.Failed;
                }

                View view = uidoc.ActiveView;
                try
                {
                    Transaction tx = new Transaction(doc, "Load RCP");
                    tx.Start();
                    view.ArePointCloudsHidden = false;
                    tx.Commit();
                }
                catch (Exception)
                {
                    TaskDialog.Show("Message", "Error change setting ArePointCloudsHidden");
                }

                foreach (Helper.OrientedBoundingBox obox in obboxes)
                {
                    try
                    {
                        // step 3: conversion PCD --> E57 
                        if (!Helper.Pcd2e57(Path.Combine(rcpFilePath, obox.ObjectGuid + ".pcd"), Path.Combine(rcpFilePath, obox.ObjectGuid + ".e57")))
                        {
                            TaskDialog.Show("Message", "CloudCompare Fehler");
                            return Result.Failed;
                        }

                        // step 4: conversion E57 --> RCP
                        if (!Helper.DeCap(path, obox.ObjectGuid, Path.Combine(rcpFilePath, obox.ObjectGuid + ".e57")))
                        {
                            TaskDialog.Show("Message", "DeCap Fehler");
                            return Result.Failed;
                        }
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

                    string rcpFilePathGuid = Path.Combine(path, "08_FragmentationBBoxComplete\\" + obox.ObjectGuid + ".rcp");

                    // load rcp
                    if (!LoadPointCloud(doc, rcpFilePathGuid, transInverse))
                    {
                        TaskDialog.Show("Message", "File not available");
                    }

                    #endregion catch
                }
                TaskDialog.Show("Message", "BBox Fragmentation successful!");
                return Result.Succeeded;
            }
            catch (Except.OperationCanceledException)
            {
                TaskDialog.Show("Message", "Error 1: Command canceled.");
                return Result.Failed;
            }
        }
        #endregion execute
        
        /// <summary>
        /// GeometryElement with their StateId and ObjectId is taken from the reference
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="reference"></param>
        /// <param name="geomElement"></param>
        /// <param name="createStateId"></param>
        /// <param name="demolishedStateId"></param>
        /// <param name="objectId"></param>
        /// <param name="cat"></param>
        /// <returns></returns>
        public static bool GetGeometryElement(Document doc, Reference reference, out ElementId eleId, out GeometryElement geomElement, out string createStateId, out string demolishedStateId,
            out string objectId, out Category cat)
        {
            Element ele = doc.GetElement(reference.ElementId);
            Options options = new Options
            {
                ComputeReferences = true
            };

            // category
            cat = ele.Category;

            geomElement = ele.get_Geometry(options);
            if (geomElement is null)
            {
                eleId = default;
                geomElement = default;
                createStateId = default;
                demolishedStateId = default;
                objectId = default;
                return false;
            }
            // stateId and objectId
            createStateId = ele.CreatedPhaseId.IntegerValue.ToString();
            demolishedStateId = ele.DemolishedPhaseId.IntegerValue.ToString();
            objectId = ele.UniqueId;
            eleId = reference.ElementId;
            return true;
        }
        private bool ReadCsvBoxes(string csvPathBBoxes, out List<Helper.OrientedBoundingBox> listOBBox)
        {
            List<Helper.OrientedBoundingBox> list = new List<Helper.OrientedBoundingBox>();
            try
            {
                using (StreamReader reader = new StreamReader(csvPathBBoxes))
                {
                    reader.ReadLine();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] columns = line.Split(';');

                        if (columns.Length == 25)
                        {
                            list.Add(new Helper.OrientedBoundingBox(bool.Parse(columns[0]), columns[1], columns[2], columns[3],
                                new XYZ(double.Parse(columns[4]), double.Parse(columns[5]), double.Parse(columns[6])),
                                new XYZ(double.Parse(columns[7]), double.Parse(columns[8]), double.Parse(columns[9])),
                                new XYZ(double.Parse(columns[10]), double.Parse(columns[11]), double.Parse(columns[12])),
                                new XYZ(double.Parse(columns[13]), double.Parse(columns[14]), double.Parse(columns[15])),
                                double.Parse(columns[16]), double.Parse(columns[17]), double.Parse(columns[18])));
                        }
                        else
                        {
                            TaskDialog.Show("Message", "Incorrect line: " + line);
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
        
        private bool LoadPointCloud(Document doc, string path, Transform trans)
        {
            try
            {
                Transaction tx = new Transaction(doc, "Load RCP");
                tx.Start();
                PointCloudType type = PointCloudType.Create(doc, "rcp", path);
                PointCloudInstance.Create(doc, type.Id, trans);
                tx.Commit();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public void WriteOBBoxToOBJFile(List<Helper.OrientedBoundingBox> oboxes, string filePath)
        {
            using StreamWriter objFile = new StreamWriter(filePath);
            int indexOffset = 0;

            foreach (Helper.OrientedBoundingBox obox in oboxes)
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

            foreach (Helper.OrientedBoundingBox obox in oboxes)
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
        public void WriteBBoxToOBJFile(List<Helper.BoundingBox> boxes, string filePath)
        {
            using StreamWriter writer = new StreamWriter(filePath);
            foreach (Helper.BoundingBox box in boxes)
            {
                writer.WriteLine("v " + box.Min.X.ToString(Sys.InvariantCulture) + " " + box.Min.Y.ToString(Sys.InvariantCulture) + " " + box.Min.Z.ToString(Sys.InvariantCulture));
                writer.WriteLine("v " + box.Max.X.ToString(Sys.InvariantCulture) + " " + box.Min.Y.ToString(Sys.InvariantCulture) + " " + box.Min.Z.ToString(Sys.InvariantCulture));
                writer.WriteLine("v " + box.Max.X.ToString(Sys.InvariantCulture) + " " + box.Max.Y.ToString(Sys.InvariantCulture) + " " + box.Min.Z.ToString(Sys.InvariantCulture));
                writer.WriteLine("v " + box.Min.X.ToString(Sys.InvariantCulture) + " " + box.Max.Y.ToString(Sys.InvariantCulture) + " " + box.Min.Z.ToString(Sys.InvariantCulture));
                writer.WriteLine("v " + box.Min.X.ToString(Sys.InvariantCulture) + " " + box.Min.Y.ToString(Sys.InvariantCulture) + " " + box.Max.Z.ToString(Sys.InvariantCulture));
                writer.WriteLine("v " + box.Max.X.ToString(Sys.InvariantCulture) + " " + box.Min.Y.ToString(Sys.InvariantCulture) + " " + box.Max.Z.ToString(Sys.InvariantCulture));
                writer.WriteLine("v " + box.Max.X.ToString(Sys.InvariantCulture) + " " + box.Max.Y.ToString(Sys.InvariantCulture) + " " + box.Max.Z.ToString(Sys.InvariantCulture));
                writer.WriteLine("v " + box.Min.X.ToString(Sys.InvariantCulture) + " " + box.Max.Y.ToString(Sys.InvariantCulture) + " " + box.Max.Z.ToString(Sys.InvariantCulture));
            }

            // Schreiben der Flächen
            int indexOffset = 0;
            foreach (Helper.BoundingBox box in boxes)
            {
                int v0 = 1 + indexOffset;
                int v1 = 2 + indexOffset;
                int v2 = 3 + indexOffset;
                int v3 = 4 + indexOffset;
                int v4 = 5 + indexOffset;
                int v5 = 6 + indexOffset;
                int v6 = 7 + indexOffset;
                int v7 = 8 + indexOffset;

                writer.WriteLine("f " + v0 + " " + v1 + " " + v2 + " " + v3);
                writer.WriteLine("f " + v7 + " " + v6 + " " + v5 + " " + v4);
                writer.WriteLine("f " + v3 + " " + v2 + " " + v6 + " " + v7);
                writer.WriteLine("f " + v4 + " " + v5 + " " + v1 + " " + v0);
                writer.WriteLine("f " + v5 + " " + v6 + " " + v2 + " " + v1);
                writer.WriteLine("f " + v0 + " " + v3 + " " + v7 + " " + v4);

                indexOffset += 8;
            }
        }
    }
}