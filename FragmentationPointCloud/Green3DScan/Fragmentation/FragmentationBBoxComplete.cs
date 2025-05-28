//using System;
//using System.Collections.Generic;
//using System.IO;
//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.UI.Selection;
//using Serilog;
//using Except = Autodesk.Revit.Exceptions;
//using Sys = System.Globalization.CultureInfo;
//using TaskDialog = Autodesk.Revit.UI.TaskDialog;
//using Path = System.IO.Path;

//namespace Revit.Green3DScan.Fragmentation
//{
//    [Transaction(TransactionMode.Manual)]
//    public class FragmentationBBoxComplete : IExternalCommand
//    {
//        private const string CsvHeader = "Oriented;StateId;ObjectGuid;ElementId;" +
//                                         "BBoxMinX;BBoxMinY;BBoxMinZ;" +
//                                         "BBoxMaxX;BBoxMaxY;BBoxMaxZ;" +
//                                         "OBoxCenterX;OBoxCenterY;OBoxCenterZ;" +
//                                         "OBoxXDirX;OBoxXDirY;OBoxXDirZ;" +
//                                         "OBoxYDirX;OBoxYDirY;OBoxYDirZ;" +
//                                         "OBoxZDirX;OBoxZDirY;OBoxZDirZ;" +
//                                         "OBoxXHSize;OBoxYHSize;OBoxZHSize";

//        /// <summary>
//        ///     GeometryElement with their StateId and ObjectId is taken from the reference
//        /// </summary>
//        /// <param name="doc"></param>
//        /// <param name="reference"></param>
//        /// <param name="eleId"></param>
//        /// <param name="createStateId"></param>
//        /// <param name="demolishedStateId"></param>
//        /// <param name="objectId"></param>
//        /// <returns></returns>
//        private static bool GetGeometryElement(Document doc, Reference reference, out ElementId eleId, out string createStateId, out string demolishedStateId,
//            out string objectId)
//        {
//            Element ele = doc.GetElement(reference.ElementId);
//            var options = new Options
//            {
//                ComputeReferences = true
//            };

//            // category
//            GeometryElement geomElement = ele.get_Geometry(options);
//            if (geomElement is null)
//            {
//                eleId = null;
//                createStateId = null;
//                demolishedStateId = null;
//                objectId = null;
//                return false;
//            }

//            // stateId and objectId
//            createStateId = ele.CreatedPhaseId.Value.ToString();
//            demolishedStateId = ele.DemolishedPhaseId.Value.ToString();
//            objectId = ele.UniqueId;
//            eleId = reference.ElementId;
//            return true;
//        }

//        private bool ReadCsvBoxes(string csvPathBBoxes, out List<Helper.OrientedBoundingBox> listOBBox)
//        {
//            var list = new List<Helper.OrientedBoundingBox>();
//            try
//            {
//                using (var reader = new StreamReader(csvPathBBoxes))
//                {
//                    reader.ReadLine();
//                    while (reader.ReadLine() is { } line)
//                    {
//                        string[] columns = line.Split(';');

//                        if (columns.Length == 25)
//                            list.Add(new Helper.OrientedBoundingBox(bool.Parse(columns[0]), columns[1], columns[2],
//                                columns[3],
//                                new XYZ(double.Parse(columns[4]), double.Parse(columns[5]), double.Parse(columns[6])),
//                                new XYZ(double.Parse(columns[7]), double.Parse(columns[8]), double.Parse(columns[9])),
//                                new XYZ(double.Parse(columns[10]), double.Parse(columns[11]),
//                                    double.Parse(columns[12])),
//                                new XYZ(double.Parse(columns[13]), double.Parse(columns[14]),
//                                    double.Parse(columns[15])),
//                                double.Parse(columns[16]), double.Parse(columns[17]), double.Parse(columns[18])));
//                        else
//                            TaskDialog.Show("Message", "Incorrect line: " + line);
//                    }
//                }

//                listOBBox = list;
//                return true;
//            }
//            catch (Exception)
//            {
//                listOBBox = list;
//                return false;
//            }
//        }

//        private static bool LoadPointCloud(Document doc, string pointCloudFilePath, Transform trans)
//        {
//            try
//            {
//                var tx = new Transaction(doc, "Load RCP");
//                tx.Start();
//                var type = PointCloudType.Create(doc, "rcp", pointCloudFilePath);
//                PointCloudInstance.Create(doc, type.Id, trans);
//                tx.Commit();
//                return true;
//            }
//            catch (Exception)
//            {
//                return false;
//            }
//        }

//        private static void WriteOBBoxToOBJFile(List<Helper.OrientedBoundingBox> oboxes, string filePath)
//        {
//            using var objFile = new StreamWriter(filePath);
//            var indexOffset = 0;

//            foreach (Helper.OrientedBoundingBox obox in oboxes)
//            {
//                var points = new XYZ[8];
//                points[0] = obox.Center - obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth -
//                            obox.ZDirection * obox.HalfHeight; // Punkt 1
//                points[1] = obox.Center + obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth -
//                            obox.ZDirection * obox.HalfHeight; // Punkt 2
//                points[2] = obox.Center + obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth -
//                            obox.ZDirection * obox.HalfHeight; // Punkt 3
//                points[3] = obox.Center - obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth -
//                            obox.ZDirection * obox.HalfHeight; // Punkt 4
//                points[4] = obox.Center - obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth +
//                            obox.ZDirection * obox.HalfHeight; // Punkt 5
//                points[5] = obox.Center + obox.XDirection * obox.HalfLength - obox.YDirection * obox.HalfWidth +
//                            obox.ZDirection * obox.HalfHeight; // Punkt 6
//                points[6] = obox.Center + obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth +
//                            obox.ZDirection * obox.HalfHeight; // Punkt 7
//                points[7] = obox.Center - obox.XDirection * obox.HalfLength + obox.YDirection * obox.HalfWidth +
//                            obox.ZDirection * obox.HalfHeight; // Punkt 8

//                foreach (XYZ point in points)
//                {
//                    objFile.WriteLine(
//                        $"v {point.X.ToString(Sys.InvariantCulture)} {point.Y.ToString(Sys.InvariantCulture)} {point.Z.ToString(Sys.InvariantCulture)}");
//                }
//            }

//            foreach (Helper.OrientedBoundingBox unused in oboxes)
//            {
//                int v0 = 1 + indexOffset;
//                int v1 = 2 + indexOffset;
//                int v2 = 3 + indexOffset;
//                int v3 = 4 + indexOffset;
//                int v4 = 5 + indexOffset;
//                int v5 = 6 + indexOffset;
//                int v6 = 7 + indexOffset;
//                int v7 = 8 + indexOffset;

//                objFile.WriteLine("f " + v0 + " " + v1 + " " + v2 + " " + v3);
//                objFile.WriteLine("f " + v4 + " " + v5 + " " + v6 + " " + v7);
//                objFile.WriteLine("f " + v0 + " " + v1 + " " + v5 + " " + v4);
//                objFile.WriteLine("f " + v1 + " " + v2 + " " + v6 + " " + v5);
//                objFile.WriteLine("f " + v2 + " " + v3 + " " + v7 + " " + v6);
//                objFile.WriteLine("f " + v3 + " " + v0 + " " + v4 + " " + v7);

//                indexOffset += 8;
//            }
//        }

//        private static void WriteBBoxToOBJFile(List<Helper.BoundingBox> boxes, string filePath)
//        {
//            using var writer = new StreamWriter(filePath);
//            foreach (Helper.BoundingBox box in boxes)
//            {
//                writer.WriteLine("v " + box.Min.X.ToString(Sys.InvariantCulture) + " " +
//                                 box.Min.Y.ToString(Sys.InvariantCulture) + " " +
//                                 box.Min.Z.ToString(Sys.InvariantCulture));
//                writer.WriteLine("v " + box.Max.X.ToString(Sys.InvariantCulture) + " " +
//                                 box.Min.Y.ToString(Sys.InvariantCulture) + " " +
//                                 box.Min.Z.ToString(Sys.InvariantCulture));
//                writer.WriteLine("v " + box.Max.X.ToString(Sys.InvariantCulture) + " " +
//                                 box.Max.Y.ToString(Sys.InvariantCulture) + " " +
//                                 box.Min.Z.ToString(Sys.InvariantCulture));
//                writer.WriteLine("v " + box.Min.X.ToString(Sys.InvariantCulture) + " " +
//                                 box.Max.Y.ToString(Sys.InvariantCulture) + " " +
//                                 box.Min.Z.ToString(Sys.InvariantCulture));
//                writer.WriteLine("v " + box.Min.X.ToString(Sys.InvariantCulture) + " " +
//                                 box.Min.Y.ToString(Sys.InvariantCulture) + " " +
//                                 box.Max.Z.ToString(Sys.InvariantCulture));
//                writer.WriteLine("v " + box.Max.X.ToString(Sys.InvariantCulture) + " " +
//                                 box.Min.Y.ToString(Sys.InvariantCulture) + " " +
//                                 box.Max.Z.ToString(Sys.InvariantCulture));
//                writer.WriteLine("v " + box.Max.X.ToString(Sys.InvariantCulture) + " " +
//                                 box.Max.Y.ToString(Sys.InvariantCulture) + " " +
//                                 box.Max.Z.ToString(Sys.InvariantCulture));
//                writer.WriteLine("v " + box.Min.X.ToString(Sys.InvariantCulture) + " " +
//                                 box.Max.Y.ToString(Sys.InvariantCulture) + " " +
//                                 box.Max.Z.ToString(Sys.InvariantCulture));
//            }

//            // Schreiben der Flächen
//            var indexOffset = 0;
//            foreach (Helper.BoundingBox unused in boxes)
//            {
//                int v0 = 1 + indexOffset;
//                int v1 = 2 + indexOffset;
//                int v2 = 3 + indexOffset;
//                int v3 = 4 + indexOffset;
//                int v4 = 5 + indexOffset;
//                int v5 = 6 + indexOffset;
//                int v6 = 7 + indexOffset;
//                int v7 = 8 + indexOffset;

//                writer.WriteLine("f " + v0 + " " + v1 + " " + v2 + " " + v3);
//                writer.WriteLine("f " + v7 + " " + v6 + " " + v5 + " " + v4);
//                writer.WriteLine("f " + v3 + " " + v2 + " " + v6 + " " + v7);
//                writer.WriteLine("f " + v4 + " " + v5 + " " + v1 + " " + v0);
//                writer.WriteLine("f " + v5 + " " + v6 + " " + v2 + " " + v1);
//                writer.WriteLine("f " + v0 + " " + v3 + " " + v7 + " " + v4);

//                indexOffset += 8;
//            }
//        }

//        #region Execute

//        private string path;
//        private string dateBimLastModified;

//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            #region setup

//            // settings json
//            var set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

//            UIDocument uidoc = commandData.Application.ActiveUIDocument;
//            Document doc = uidoc.Document;
//            try
//            {
//                path = Path.GetDirectoryName(doc.PathName);
//                var fileInfo = new FileInfo(path ?? throw new InvalidOperationException());
//                DateTime date = fileInfo.LastWriteTime;
//                dateBimLastModified =
//                    date.Year + "-" + date.Month + "-" + date.Day + "-" + date.Hour + "-" + date.Minute;
//            }
//            catch (Exception)
//            {
//                TaskDialog.Show("Message", "The file has not been saved yet.");
//                return Result.Failed;
//            }

//            // logger
//            string logsPath = Path.Combine(path, "00_Logs/");
//            if (!Directory.Exists(logsPath)) Directory.CreateDirectory(logsPath);
//            Log.Logger = new LoggerConfiguration()
//                .MinimumLevel.Debug()
//                .WriteTo.File(Path.Combine(logsPath, "LogFile_"), rollingInterval: RollingInterval.Minute)
//                .CreateLogger();
//            Log.Information("start FragmentationBBoxComplete");
//            Log.Information(set.BBox_Buffer.ToString(Sys.InvariantCulture));

//            #endregion setup

//            string pcdPathPointcloud = set.PathPointCloud;
//            string bBoxPath = Path.Combine(path, "08_FragmentationBBoxComplete\\");
//            string rcpOutputPath = Path.Combine(path, "08_FragmentationBBoxComplete");

//            if (!Directory.Exists(bBoxPath)) Directory.CreateDirectory(bBoxPath);

//            Transform trans = Helper.GetTransformation(doc, set, out CoordinateSystem _);
//            Transform transInverse = trans.Inverse;

//            using StreamWriter csv = File.CreateText(Path.Combine(bBoxPath, "BIM_BBoxes.csv"));
//            csv.WriteLine(CsvHeader);
//            var bBoxes = new List<Helper.BoundingBox>();
//            var oBBoxes = new List<Helper.OrientedBoundingBox>();

//            try
//            {
//                var pickedObjects = uidoc.Selection.PickObjects(ObjectType.Element,
//                    "Select components whose oriented bounding boxes are to be output.");

//                foreach (Reference reference in pickedObjects)
//                {
//                    bool oriented;
//                    double halfLength = 0;
//                    double halfWidth = 0;
//                    double halfHeight = 0;
//                    var center3D = new XYZ(0, 0, 0);
//                    var directionX = new XYZ(0, 0, 0);
//                    var directionY = new XYZ(0, 0, 0);
//                    // orientated BBox is only rotated horizontally
//                    var directionZ = new XYZ(0, 0, 1);

//                    View currentView = doc.ActiveView;
//                    if (!GetGeometryElement(doc, reference, out ElementId eleId,
//                            out string createStateId,
//                            out string demolishedStateId, out string objectId))
//                    {
//                        Log.Information("skipped building component");
//                        continue;
//                    }

//                    Element element = doc.GetElement(eleId);
//                    // conversion to IFC GUID
//                    string ifcGuid = Helper.ToIfcGuid(Helper.ToGuid(element.UniqueId));

//                    // bbox
//                    BoundingBoxXYZ bBox = element.get_BoundingBox(currentView);
//                    XYZ bBoxMin = trans.OfPoint(bBox.Min) * Constants.feet2Meter;
//                    XYZ bBoxMax = trans.OfPoint(bBox.Max) * Constants.feet2Meter;
//                    // high of bbox
//                    double high = bBox.Max.Z - bBox.Min.Z;

//                    if (element is Wall wall)
//                    {
//                        if (element.Location is LocationCurve locationCurve)
//                        {
//                            Curve curve = locationCurve.Curve;
//                            if (curve is Line line)
//                            {
//                                // direction of line
//                                directionX = trans.OfVector(line.Direction).Normalize();
//                                directionY = directionX.CrossProduct(directionZ);
//                                halfLength = line.Length / 2 * Constants.feet2Meter + set.BBox_Buffer;
//                                halfHeight = high / 2 * Constants.feet2Meter + set.BBox_Buffer;
//                                // center of bbox
//                                XYZ center2D = (line.GetEndPoint(0) + line.GetEndPoint(1)) / 2;

//                                center3D = trans.OfPoint(center2D + new XYZ(0, 0, 0.5 * high)) * Constants.feet2Meter;

//                                if (doc.GetElement(wall.GetTypeId()) is WallType wallType)
//                                {
//                                    ParameterSet parameters = wallType.Parameters;
//                                    foreach (Parameter param in parameters)
//                                    {
//                                        string parameterName = param.Definition.Name;
//                                        if (parameterName == "Breite")
//                                        {
//                                            double paramValue = param.AsDouble();
//                                            halfWidth = paramValue / 2 * Constants.feet2Meter + set.BBox_Buffer;
//                                        }
//                                    }
//                                }

//                                oriented = true;
//                                oBBoxes.Add(new Helper.OrientedBoundingBox(oriented, "stateId", objectId, ifcGuid,
//                                    center3D, directionX, directionY, new XYZ(0, 0, 1), halfLength, halfWidth,
//                                    halfHeight));

//                                csv.WriteLine(oriented + ";" + createStateId + "|" + demolishedStateId + ";" + ifcGuid +
//                                              ";" + eleId + ";"
//                                              + Math.Round(bBoxMin.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(bBoxMin.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(bBoxMin.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                              + Math.Round(bBoxMax.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(bBoxMax.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(bBoxMax.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                              + Math.Round(center3D.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(center3D.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(center3D.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                              + directionX.X.ToString(Sys.InvariantCulture) + ";" +
//                                              directionX.Y.ToString(Sys.InvariantCulture) + ";" +
//                                              directionX.Z.ToString(Sys.InvariantCulture) + ";"
//                                              + directionY.X.ToString(Sys.InvariantCulture) + ";" +
//                                              directionY.Y.ToString(Sys.InvariantCulture) + ";" +
//                                              directionY.Z.ToString(Sys.InvariantCulture) + ";"
//                                              + directionZ.X.ToString(Sys.InvariantCulture) + ";" +
//                                              directionZ.Y.ToString(Sys.InvariantCulture) + ";" +
//                                              directionZ.Z.ToString(Sys.InvariantCulture) + ";"
//                                              + Math.Round(halfLength, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(halfWidth, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(halfHeight, 4).ToString(Sys.InvariantCulture));
//                            }
//                            else
//                            {
//                                Log.Information("Wall, but the LocationCurve is not a line.");
//                            }
//                        }
//                        else
//                        {
//                            Log.Information("Wall, but LocationCurve not existing.");
//                        }
//                    }
//                    else if (element.Category != null &&
//                             element.Category.Id.Value == (int)BuiltInCategory.OST_StructuralFraming)
//                    {
//                        if (element.Location is LocationCurve locationCurve)
//                        {
//                            Curve curve = locationCurve.Curve;
//                            if (curve is Line line)
//                            {
//                                // direction  der Line
//                                directionX = trans.OfVector(line.Direction).Normalize();
//                                directionY = directionX.CrossProduct(directionZ);
//                                halfLength = line.Length / 2 * Constants.feet2Meter + set.BBox_Buffer;
//                                halfHeight = high / 2 * Constants.feet2Meter + set.BBox_Buffer;
//                                // Zentrum der OBBox
//                                XYZ center2D = (line.GetEndPoint(0) + line.GetEndPoint(1)) / 2;

//                                center3D = trans.OfPoint(center2D - new XYZ(0, 0, 0.5 * high)) * Constants.feet2Meter;

//                                Element x = doc.GetElement(element.GetTypeId());
//                                ParameterSet parameters = x.Parameters;
//                                foreach (Parameter param in parameters)
//                                {
//                                    string parameterName = param.Definition.Name;
//                                    if (parameterName == "Width" || parameterName == "Breite")
//                                    {
//                                        double paramValue = param.AsDouble();
//                                        halfWidth = paramValue / 2 * Constants.feet2Meter + set.BBox_Buffer;
//                                    }
//                                }

//                                oriented = true;
//                                oBBoxes.Add(new Helper.OrientedBoundingBox(oriented, "stateId", objectId, ifcGuid,
//                                    center3D, directionX, directionY, new XYZ(0, 0, 1), halfLength, halfWidth,
//                                    halfHeight));

//                                csv.WriteLine(oriented + ";" + createStateId + "|" + demolishedStateId + ";" + ifcGuid +
//                                              ";" + eleId + ";"
//                                              + Math.Round(bBoxMin.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(bBoxMin.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(bBoxMin.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                              + Math.Round(bBoxMax.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(bBoxMax.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(bBoxMax.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                              + Math.Round(center3D.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(center3D.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(center3D.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                              + directionX.X.ToString(Sys.InvariantCulture) + ";" +
//                                              directionX.Y.ToString(Sys.InvariantCulture) + ";" +
//                                              directionX.Z.ToString(Sys.InvariantCulture) + ";"
//                                              + directionY.X.ToString(Sys.InvariantCulture) + ";" +
//                                              directionY.Y.ToString(Sys.InvariantCulture) + ";" +
//                                              directionY.Z.ToString(Sys.InvariantCulture) + ";"
//                                              + directionZ.X.ToString(Sys.InvariantCulture) + ";" +
//                                              directionZ.Y.ToString(Sys.InvariantCulture) + ";" +
//                                              directionZ.Z.ToString(Sys.InvariantCulture) + ";"
//                                              + Math.Round(halfLength, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(halfWidth, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(halfHeight, 4).ToString(Sys.InvariantCulture));
//                            }
//                            else
//                            {
//                                Log.Information("Wall, but the LocationCurve is not a line.");
//                                bBoxes.Add(new Helper.BoundingBox(bBoxMin, bBoxMax));
//                                oriented = false;
//                                csv.WriteLine(oriented + ";" + createStateId + "|" + demolishedStateId + ";" + ifcGuid +
//                                              ";" + eleId + ";"
//                                              + Math.Round(bBoxMin.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(bBoxMin.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(bBoxMin.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                              + Math.Round(bBoxMax.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(bBoxMax.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(bBoxMax.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                              + Math.Round(center3D.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(center3D.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(center3D.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                              + directionX.X.ToString(Sys.InvariantCulture) + ";" +
//                                              directionX.Y.ToString(Sys.InvariantCulture) + ";" +
//                                              directionX.Z.ToString(Sys.InvariantCulture) + ";"
//                                              + directionY.X.ToString(Sys.InvariantCulture) + ";" +
//                                              directionY.Y.ToString(Sys.InvariantCulture) + ";" +
//                                              directionY.Z.ToString(Sys.InvariantCulture) + ";"
//                                              + directionZ.X.ToString(Sys.InvariantCulture) + ";" +
//                                              directionZ.Y.ToString(Sys.InvariantCulture) + ";" +
//                                              directionZ.Z.ToString(Sys.InvariantCulture) + ";"
//                                              + Math.Round(halfLength, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(halfWidth, 4).ToString(Sys.InvariantCulture) + ";" +
//                                              Math.Round(halfHeight, 4).ToString(Sys.InvariantCulture));
//                            }
//                        }
//                        else
//                        {
//                            Log.Information("Wall, but LocationCurve not existing.");
//                            bBoxes.Add(new Helper.BoundingBox(bBoxMin, bBoxMax));
//                            oriented = false;
//                            csv.WriteLine(oriented + ";" + createStateId + "|" + demolishedStateId + ";" + ifcGuid +
//                                          ";" + eleId + ";"
//                                          + Math.Round(bBoxMin.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                          Math.Round(bBoxMin.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                          Math.Round(bBoxMin.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                          + Math.Round(bBoxMax.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                          Math.Round(bBoxMax.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                          Math.Round(bBoxMax.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                          + Math.Round(center3D.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                          Math.Round(center3D.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                          Math.Round(center3D.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                          + directionX.X.ToString(Sys.InvariantCulture) + ";" +
//                                          directionX.Y.ToString(Sys.InvariantCulture) + ";" +
//                                          directionX.Z.ToString(Sys.InvariantCulture) + ";"
//                                          + directionY.X.ToString(Sys.InvariantCulture) + ";" +
//                                          directionY.Y.ToString(Sys.InvariantCulture) + ";" +
//                                          directionY.Z.ToString(Sys.InvariantCulture) + ";"
//                                          + directionZ.X.ToString(Sys.InvariantCulture) + ";" +
//                                          directionZ.Y.ToString(Sys.InvariantCulture) + ";" +
//                                          directionZ.Z.ToString(Sys.InvariantCulture) + ";"
//                                          + Math.Round(halfLength, 4).ToString(Sys.InvariantCulture) + ";" +
//                                          Math.Round(halfWidth, 4).ToString(Sys.InvariantCulture) + ";" +
//                                          Math.Round(halfHeight, 4).ToString(Sys.InvariantCulture));
//                        }
//                    }
//                    else
//                    {
//                        Log.Information("Building segment is not a Wall or StructuralFraming, but: " +
//                                        element.Category);
//                        bBoxes.Add(new Helper.BoundingBox(bBoxMin, bBoxMax));
//                        oriented = false;
//                        csv.WriteLine(oriented + ";" + createStateId + "|" + demolishedStateId + ";" + ifcGuid + ";" +
//                                      eleId + ";"
//                                      + Math.Round(bBoxMin.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                      Math.Round(bBoxMin.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                      Math.Round(bBoxMin.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                      + Math.Round(bBoxMax.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                      Math.Round(bBoxMax.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                      Math.Round(bBoxMax.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                      + Math.Round(center3D.X, 4).ToString(Sys.InvariantCulture) + ";" +
//                                      Math.Round(center3D.Y, 4).ToString(Sys.InvariantCulture) + ";" +
//                                      Math.Round(center3D.Z, 4).ToString(Sys.InvariantCulture) + ";"
//                                      + directionX.X.ToString(Sys.InvariantCulture) + ";" +
//                                      directionX.Y.ToString(Sys.InvariantCulture) + ";" +
//                                      directionX.Z.ToString(Sys.InvariantCulture) + ";"
//                                      + directionY.X.ToString(Sys.InvariantCulture) + ";" +
//                                      directionY.Y.ToString(Sys.InvariantCulture) + ";" +
//                                      directionY.Z.ToString(Sys.InvariantCulture) + ";"
//                                      + directionZ.X.ToString(Sys.InvariantCulture) + ";" +
//                                      directionZ.Y.ToString(Sys.InvariantCulture) + ";" +
//                                      directionZ.Z.ToString(Sys.InvariantCulture) + ";"
//                                      + Math.Round(halfLength, 4).ToString(Sys.InvariantCulture) + ";" +
//                                      Math.Round(halfWidth, 4).ToString(Sys.InvariantCulture) + ";" +
//                                      Math.Round(halfHeight, 4).ToString(Sys.InvariantCulture));
//                    }
//                }

//                csv.Close();

//                WriteOBBoxToOBJFile(oBBoxes, Path.Combine(bBoxPath, "OBBoxes.obj"));
//                WriteBBoxToOBJFile(bBoxes, Path.Combine(bBoxPath, "BBoxes.obj"));

//                string csvPathBBoxes = Path.Combine(bBoxPath, "BIM_BBoxes.csv");

//                // CSV lesen
//                if (!ReadCsvBoxes(csvPathBBoxes, out var obboxes))
//                {
//                    TaskDialog.Show("Message", "Reading csv failed");
//                    return Result.Failed;
//                }

//                // step 2: fragmentation an save small pcd
//                string exeGreen3DPath = Constants.exeFragmentationBBox;

//                var command = $"{pcdPathPointcloud} {csvPathBBoxes} {bBoxPath} {dateBimLastModified}";
//                if (!Helper.Fragmentation2Pcd(exeGreen3DPath, command))
//                {
//                    TaskDialog.Show("Message", "Fragmentation error");
//                    return Result.Failed;
//                }

//                View view = uidoc.ActiveView;
//                try
//                {
//                    var tx = new Transaction(doc, "Load RCP");
//                    tx.Start();
//                    view.ArePointCloudsHidden = false;
//                    tx.Commit();
//                }
//                catch (Exception)
//                {
//                    TaskDialog.Show("Message", "Error change setting ArePointCloudsHidden");
//                }

//                foreach (Helper.OrientedBoundingBox obox in obboxes)
//                {
//                    try
//                    {
//                        // step 3: conversion PCD --> E57 
//                        if (!Helper.Pcd2e57(Path.Combine(bBoxPath, obox.ObjectGuid + ".pcd"),
//                                Path.Combine(bBoxPath, obox.ObjectGuid + ".e57"), set))
//                        {
//                            TaskDialog.Show("Message", "CloudCompare Fehler");
//                            return Result.Failed;
//                        }

//                        // step 4: conversion E57 --> RCP
//                        if (!Helper.DeCap(rcpOutputPath, obox.ObjectGuid,
//                                Path.Combine(bBoxPath, obox.ObjectGuid + ".e57")))
//                        {
//                            TaskDialog.Show("Message", "DeCap Fehler");
//                            return Result.Failed;
//                        }
//                    }

//                    #region catch

//                    catch (Except.OperationCanceledException)
//                    {
//                        TaskDialog.Show("Message", "Error 1: Command canceled.");
//                        return Result.Failed;
//                    }
//                    catch (Except.ForbiddenForDynamicUpdateException)
//                    {
//                        TaskDialog.Show("Message", "Error 2");
//                        return Result.Failed;
//                    }
//                    catch (Exception ex)
//                    {
//                        message += "Error message::" + ex;
//                        TaskDialog.Show("Message", message);
//                        return Result.Failed;
//                    }

//                    string rcpFilePathGuid =
//                        Path.Combine(path, "08_FragmentationBBoxComplete\\" + obox.ObjectGuid + ".rcp");

//                    // load rcp
//                    if (!LoadPointCloud(doc, rcpFilePathGuid, transInverse))
//                        TaskDialog.Show("Message", "File not available");

//                    #endregion catch
//                }

//                TaskDialog.Show("Message", "BBox Fragmentation successful!");
//                return Result.Succeeded;
//            }
//            catch (Except.OperationCanceledException)
//            {
//                TaskDialog.Show("Message", "Error 1: Command canceled.");
//                return Result.Failed;
//            }
//        }

//        #endregion execute
//    }
//}