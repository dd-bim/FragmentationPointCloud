using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Except = Autodesk.Revit.Exceptions;
using Sys = System.Globalization.CultureInfo;
using Serilog;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using Line = Autodesk.Revit.DB.Line;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class Revit2OBBox : IExternalCommand
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
            Log.Information("start");
            Log.Information(set.BBox_Buffer.ToString());
            #endregion setup

            Transform trans = Helper.GetTransformation(doc, set, out var crs);

            string csvPath = Path.Combine(path, "06_BBox/");

            if (!Directory.Exists(csvPath))
            {
                Directory.CreateDirectory(csvPath);
            }

            using StreamWriter csv = File.CreateText(Path.Combine(csvPath, "BIM_BBoxes.csv"));
            csv.WriteLine(CsvHeader);
            List<BoundingBox> bBoxes = new List<BoundingBox>();
            List<Helper.OrientedBoundingBox> oBBoxes = new List<Helper.OrientedBoundingBox>();
            
            try
            {
                IList<Reference> pickedObjects = uidoc.Selection.PickObjects(ObjectType.Element, "Select components whose oriented bounding boxes are to be output.");
                
                foreach (Reference reference in pickedObjects)
                {
                    bool oriented = false;
                    string stateId = default;
                    string objectGuid = default;
                    string elementId = default;
                    double halfLength = default;
                    double halfWidth = default;
                    double halfHeight = default;
                    string ifcGuid = default;
                    XYZ center3D = new XYZ(0, 0, 0);
                    XYZ directionX = new XYZ(0, 0, 0);
                    XYZ directionY = new XYZ(0, 0, 0);
                    XYZ bBoxMin = new XYZ(0, 0, 0);
                    XYZ bBoxMax = new XYZ(0, 0, 0);
                    // oriented bbox, rotation around z-axis
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
                    // height of obbox
                    var high = bBox.Max.Z - bBox.Min.Z;

                    if (element is Wall wall)
                    {
                        if (element.Location is LocationCurve locationCurve)
                        {
                            Curve curve = locationCurve.Curve;
                            if (curve is Line line)
                            {
                                // direction of the line
                                directionX = trans.OfVector(line.Direction).Normalize();
                                directionY = directionX.CrossProduct(directionZ);
                                halfLength = line.Length / 2 * Constants.feet2Meter + set.BBox_Buffer;
                                halfHeight = high / 2 * Constants.feet2Meter + set.BBox_Buffer;
                                // center obbox
                                var center2D = (line.GetEndPoint(0) + line.GetEndPoint(1)) / 2;

                                // must be transformed if coordinatereduktion true
                                center3D = trans.OfPoint(center2D + new XYZ(0, 0, 0.5 * high)) * Constants.feet2Meter;

                                if (doc.GetElement(wall.GetTypeId()) is WallType wallType)
                                {
                                    ParameterSet parameters = wallType.Parameters;
                                    foreach (Parameter param in parameters)
                                    {
                                        string parameterName = param.Definition.Name;
                                        if (parameterName == "Width" || parameterName == "Breite")
                                        {
                                            double paramValue = param.AsDouble();
                                            halfWidth = paramValue / 2 * Constants.feet2Meter + set.BBox_Buffer;
                                        }
                                    }
                                }
                                oriented = true;
                                Element ele = doc.GetElement(reference.ElementId);
                                elementId = reference.ElementId.ToString();
                                stateId = ele.CreatedPhaseId.IntegerValue.ToString();
                                objectGuid = ele.UniqueId.ToString();

                                oBBoxes.Add(new Helper.OrientedBoundingBox(oriented, stateId, objectGuid, elementId, center3D, directionX, directionY, new XYZ(0, 0, 1), halfLength, halfWidth, halfHeight));

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
                                // direction of the line
                                directionX = trans.OfVector(line.Direction).Normalize();
                                directionY = directionX.CrossProduct(directionZ);
                                halfLength = line.Length / 2 * Constants.feet2Meter + set.BBox_Buffer;
                                halfHeight = high / 2 * Constants.feet2Meter + set.BBox_Buffer;
                                // center obbox
                                var center2D = (line.GetEndPoint(0) + line.GetEndPoint(1)) / 2;

                                // must be transformed if coordinatereduktion true
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
                                Element ele = doc.GetElement(reference.ElementId);
                                elementId = reference.ElementId.ToString();
                                stateId = ele.CreatedPhaseId.IntegerValue.ToString();
                                objectGuid = ele.UniqueId.ToString();
                                oBBoxes.Add(new Helper.OrientedBoundingBox(oriented, stateId,objectGuid,elementId,center3D, directionX, directionY, new XYZ(0, 0, 1), halfLength, halfWidth, halfHeight));

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
                                bBoxes.Add(new BoundingBox(bBoxMin, bBoxMax));
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
                            bBoxes.Add(new BoundingBox(bBoxMin, bBoxMax));
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
                        bBoxes.Add(new BoundingBox(bBoxMin, bBoxMax));
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
                
                WriteOBBoxToOBJFile(oBBoxes, System.IO.Path.Combine(path, "OBBoxes.obj"));
                WriteBBoxToOBJFile(bBoxes, Path.Combine(path, "BBoxes.obj"));

                TaskDialog.Show("Message", oBBoxes.Count + " OBBoxes and " + bBoxes.Count + " BBoxes were exported.");
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
        public class BoundingBox
        {
            public XYZ Min { get; set; }
            public XYZ Max { get; set; }

            public BoundingBox(XYZ min, XYZ max)
            {
                Min = min;
                Max = max;
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
        public void WriteBBoxToOBJFile(List<BoundingBox> boxes, string filePath)
        {
            using StreamWriter writer = new StreamWriter(filePath);
            foreach (BoundingBox box in boxes)
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

            // faces
            int indexOffset = 0;
            foreach (BoundingBox box in boxes)
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
    }
}