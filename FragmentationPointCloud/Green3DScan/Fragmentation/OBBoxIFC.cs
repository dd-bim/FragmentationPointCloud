//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using Serilog;
//using Except = Autodesk.Revit.Exceptions;
//using Sys = System.Globalization.CultureInfo;
//using TaskDialog = Autodesk.Revit.UI.TaskDialog;


//namespace Revit.Green3DScan.Fragmentation
//{
//    [Transaction(TransactionMode.Manual)]
//    public abstract class OBBoxIFC : IExternalCommand
//    {
//        private readonly string ifcToolPath = @"D:\000_Green3DScan\pyIFC\IFCFaceBoxExtractor.exe";

//        public void WriteOBBoxToOBJFile(List<OrientedBoundingBox> oboxes, string filePath)
//        {
//            using var objFile = new StreamWriter(filePath);
//            var indexOffset = 0;

//            foreach (OrientedBoundingBox obox in oboxes)
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

//            foreach (OrientedBoundingBox unused in oboxes)
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

//        public abstract class OrientedBoundingBox(
//            string stateId,
//            string objectGuid,
//            string elementId,
//            XYZ center,
//            XYZ xDir,
//            XYZ yDir,
//            XYZ zDir,
//            double halfLength,
//            double halfWidth,
//            double halfHeight)
//        {
//            public string StateId { get; } = stateId;
//            public string ObjectGuid { get; } = objectGuid;
//            public string ElementId { get; } = elementId;
//            public XYZ Center { get; } = center;
//            public XYZ XDirection { get; } = xDir;
//            public XYZ YDirection { get; } = yDir;
//            public XYZ ZDirection { get; } = zDir;
//            public double HalfLength { get; } = halfLength;
//            public double HalfWidth { get; } = halfWidth;
//            public double HalfHeight { get; } = halfHeight;
//        }

//        #region Execute

//        private string _path;

//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            #region setup

//            // settings json
//            var set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

//            UIDocument uiDocument = commandData.Application.ActiveUIDocument;
//            Document doc = uiDocument.Document;
//            try
//            {
//                _path = Path.GetDirectoryName(doc.PathName);
//            }
//            catch (Exception)
//            {
//                TaskDialog.Show("Message", "The file has not been saved yet.");
//                return Result.Failed;
//            }

//            // logger
//            string logsPath = Path.Combine(_path!, "00_Logs/");
//            if (!Directory.Exists(logsPath)) Directory.CreateDirectory(logsPath);
//            Log.Logger = new LoggerConfiguration()
//                .MinimumLevel.Debug()
//                .WriteTo.File(Path.Combine(logsPath, "LogFile_"), rollingInterval: RollingInterval.Minute)
//                .CreateLogger();
//            Log.Information("start OBBoxIFC");
//            Log.Information(set.BBox_Buffer.ToString(Sys.InvariantCulture));

//            #endregion setup

//            try
//            {
//                // IFC
//                var fodBBox = new FileOpenDialog("IFC file (*.ifc)|*.ifc");
//                fodBBox.Title = "Select IFC file!";
//                if (fodBBox.Show() == ItemSelectionDialogResult.Canceled) return Result.Cancelled;
//                string csvPathIfc = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodBBox.GetSelectedModelPath());

//                //EXE
//                var arg1 = "-i";
//                string arg2 = csvPathIfc;
//                var arg3 = "-faces";
//                var arg4 = "-faceFile";
//                var arg5 = "D:\\000_Green3DScan\\05_Datensaetze\\1_LTV_Eibenstock\\IFC\\AusgabeIFC.csv";
//                var arg6 = "-boxes";
//                var arg7 = "-boxFile";
//                var arg8 = "D:\\000_Green3DScan\\05_Datensaetze\\1_LTV_Eibenstock\\IFC\\boxes.csv";
//                var arg9 = "-entityList";
//                var arg10 = "D:\\000_Green3DScan\\pyIFC\\basicList.json";
//                var arg11 = "-boxBuffer";
//                var arg12 = set.BBox_Buffer.ToString(Sys.InvariantCulture);
//                var arguments =
//                    $"{arg1} \"{arg2}\" {arg3} {arg4} \"{arg5}\" {arg6} {arg7} \"{arg8}\" {arg9} {arg10} {arg11} {arg12}";

//                var startInfo = new ProcessStartInfo
//                {
//                    FileName = ifcToolPath,
//                    Arguments = arguments,
//                    UseShellExecute = false,
//                    RedirectStandardOutput = true
//                };

//                var process = new Process();
//                process.StartInfo = startInfo;

//                process.Start();
//                process.StandardOutput.ReadToEnd();
//                process.WaitForExit();

//                //WriteOBBoxToOBJFile(obboxes, Path.Combine(path, "OBBoxesIFC.obj"));
//                return Result.Succeeded;
//            }

//            #region catch

//            catch (Except.OperationCanceledException)
//            {
//                TaskDialog.Show("Message", "Error 1: Command canceled.");
//                return Result.Failed;
//            }
//            catch (Except.ForbiddenForDynamicUpdateException)
//            {
//                TaskDialog.Show("Message", "Error 2");
//                return Result.Failed;
//            }
//            catch (Exception ex)
//            {
//                message += "Error message::" + ex;
//                TaskDialog.Show("Message", message);
//                return Result.Failed;
//            }

//            #endregion catch
//        }

//        #endregion execute
//    }
//}