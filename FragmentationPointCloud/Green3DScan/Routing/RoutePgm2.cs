using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Except = Autodesk.Revit.Exceptions;
using Serilog;
using YamlDotNet.Serialization;
using OpenCvSharp;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using Document = Autodesk.Revit.DB.Document;
using Sys = System.Globalization.CultureInfo;


namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class RoutePgm2 : IExternalCommand
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
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File(Path.Combine(path, "LogFile_"), rollingInterval: RollingInterval.Day)
               .CreateLogger();
            Log.Information("start");
            Log.Information(set.BBox_Buffer.ToString());
            #endregion setup

            string routePath = Path.Combine(path, "05_Route_PGM\\");
            if (!Directory.Exists(routePath))
            {
                Directory.CreateDirectory(routePath);
            }
            string pathPng = Path.Combine(routePath, "RoutePNG.png");
            string pathPgm = Path.Combine(routePath, "RoutePGM.pgm");
            string pathPgmWithoutExtension = Path.Combine(routePath, "RoutePNGwithoutExtension.pgm");
            string pathYaml = Path.Combine(routePath, "RoutePGM.yaml");

            Transform trans = Helper.GetTransformation(doc, set, out var crs);

            try
            {
                Result resGetUIView = GetUIView(uidoc, set, trans, out double pngWidthInMeter, out XYZ bottomLeft);
                if (resGetUIView != Result.Succeeded)
                {
                    TaskDialog.Show("Message", "Determine image width/scale not successful!");
                    return Result.Failed;
                }

                Result resExportToPng = ExportToPng(doc, pathPng, pngWidthInMeter, set);
                if (resExportToPng != Result.Succeeded)
                {
                    TaskDialog.Show("Message", "Creating png not successful!");
                    return Result.Failed;
                }

                // extension to square?
                if (set.PgmImageExpansion_Px == 0)
                {
                    Result resConvertToPgm2 = ConvertToPgmWithouExtension(pathPng, pathPgmWithoutExtension, set);
                    if (resConvertToPgm2 != Result.Succeeded)
                    {
                        TaskDialog.Show("Message", "Conversion png to pgm not successful!");
                        return Result.Failed;
                    }
                }
                else
                {
                    Result resConvertToPgm = ConvertToPgm(pathPng, pathPgm, set);
                    if (resConvertToPgm != Result.Succeeded)
                    {
                        TaskDialog.Show("Message", "Conversion png to pgm  was not successful!");
                        return Result.Failed;
                    }
                }

                Result resWriteYaml = WriteYaml(pathYaml, set, bottomLeft);
                if (resWriteYaml != Result.Succeeded)
                {
                    TaskDialog.Show("Message", "Create YAML was not successful!");
                    return Result.Failed;
                }
                TaskDialog.Show("Message", "Process successful!");
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
                message += "Error message:" + ex.ToString();
                TaskDialog.Show("Message", message);
                return Result.Failed;
            }
            #endregion catch
        }
        #endregion execute
        static Result GetUIView(UIDocument uidoc, SettingsJson set, Transform trans, out double pngWidthInMeter, out XYZ bottomLeft)
        {
            pngWidthInMeter = 0;
            bottomLeft = null;
            try
            {
                View view = uidoc.ActiveView;

                if (view.ViewType == ViewType.FloorPlan)
                {
                    UIView uIView = uidoc.Application.ActiveUIDocument.GetOpenUIViews().FirstOrDefault(viewUi => viewUi.ViewId == view.Id);

                    if (uIView != null)
                    {
                        IList<XYZ> corners = uIView.GetZoomCorners();
                        XYZ min = trans.OfPoint(new XYZ(corners[0].X, corners[0].Y, corners[0].Z)) * Constants.feet2Meter;
                        XYZ max = trans.OfPoint(new XYZ(corners[1].X, corners[1].Y, corners[1].Z)) * Constants.feet2Meter;
                        pngWidthInMeter = max.X - min.X;
                        if (set.PgmImageExpansion_Px == 0)
                        {
                            bottomLeft = min;
                            return Result.Succeeded;
                        }
                        else 
                        {
                            XYZ centrum = new XYZ((min.X + max.X) / 2, (min.Y + max.Y) / 2, (min.Z + max.Z) / 2);
                            bottomLeft = centrum - new XYZ(0.5 * set.PgmImageExpansion_Px * set.PgmImageResolution_Meter, 0.5 * set.PgmImageExpansion_Px * set.PgmImageResolution_Meter, 0);
                        }
                    }
                    
                    else
                    {
                        TaskDialog.Show("Message", "UIView of current view not found!");
                    }
                }
                else
                {
                    TaskDialog.Show("Message", "Active view is not a FloorPlan!");
                }

                double maxImageExpansion = set.PgmImageExpansion_Px * set.PgmImageResolution_Meter;
                if (pngWidthInMeter > set.PgmImageExpansion_Px * set.PgmImageResolution_Meter)
                {
                    int minExpansion = (int)Math.Round(pngWidthInMeter / set.PgmImageResolution_Meter);
                    TaskDialog.Show("Message", "meters! This value results from the settings and the current zoom. Set at least " + minExpansion + " pixels for this section.");
                    return Result.Failed;
                }
                else if (pngWidthInMeter == 0)
                {
                    TaskDialog.Show("Message", "Zoom range could not be captured.");
                    return Result.Failed;
                }
                return Result.Succeeded;
            }
            catch (Exception)
            {
                return Result.Failed;
            }
        }
        public class ViewCreation
        {
            public static IEnumerable<ViewFamilyType> FindViewTypes(Document doc, ViewType viewType)
            {
                IEnumerable<ViewFamilyType> ret = new FilteredElementCollector(doc).WherePasses(new ElementClassFilter(typeof(ViewFamilyType), false)).Cast<ViewFamilyType>();

                return viewType switch
                {
                    ViewType.AreaPlan => ret.Where(e => e.ViewFamily == ViewFamily.AreaPlan),
                    ViewType.CeilingPlan => ret.Where(e => e.ViewFamily == ViewFamily.CeilingPlan),
                    ViewType.CostReport => ret.Where(e => e.ViewFamily == ViewFamily.CostReport),
                    ViewType.Detail => ret.Where(e => e.ViewFamily == ViewFamily.Detail),
                    ViewType.DraftingView => ret.Where(e => e.ViewFamily == ViewFamily.Drafting),
                    ViewType.DrawingSheet => ret.Where(e => e.ViewFamily == ViewFamily.Sheet),
                    ViewType.Elevation => ret.Where(e => e.ViewFamily == ViewFamily.Elevation),
                    ViewType.FloorPlan => ret.Where(e => e.ViewFamily == ViewFamily.FloorPlan),
                    ViewType.Legend => ret.Where(e => e.ViewFamily == ViewFamily.Legend),
                    ViewType.LoadsReport => ret.Where(e => e.ViewFamily == ViewFamily.LoadsReport),
                    ViewType.PanelSchedule => ret.Where(e => e.ViewFamily == ViewFamily.PanelSchedule),
                    ViewType.PresureLossReport => ret.Where(e => e.ViewFamily == ViewFamily.PressureLossReport),
                    ViewType.Rendering => ret.Where(e => e.ViewFamily == ViewFamily.ImageView),
                    ViewType.Schedule => ret.Where(e => e.ViewFamily == ViewFamily.Schedule),
                    ViewType.Section => ret.Where(e => e.ViewFamily == ViewFamily.Section),
                    ViewType.ThreeD => ret.Where(e => e.ViewFamily == ViewFamily.ThreeDimensional),
                    ViewType.Walkthrough => ret.Where(e => e.ViewFamily == ViewFamily.Walkthrough),
                    _ => ret,
                };
            }
            public static ViewPlan NewViewPlan(Level level, ViewType viewType)
            {
                ElementId viewTypeId = FindViewTypes(level.Document, viewType).First().Id;
                ViewPlan view = ViewPlan.Create(level.Document, viewTypeId, level.Id);
                return view;
            }
        }
        static Result ExportToPng(Document doc, string filepath, double modelSizeInMeter, SettingsJson set)
        {
            try
            {
                using Transaction tx = new Transaction(doc, "Export Image");
                tx.Start();
                View view = doc.ActiveView;

                // Calculate the number of pixels per centimetre based on the desired scale
                double pixelSize = modelSizeInMeter / set.PgmImageResolution_Meter;

                ImageExportOptions img = new ImageExportOptions
                {
                    ZoomType = ZoomFitType.FitToPage,
                    Zoom = 100,
                    PixelSize = (int)pixelSize,
                    ImageResolution = ImageResolution.DPI_72,
                    FitDirection = FitDirectionType.Horizontal,
                    ExportRange = ExportRange.VisibleRegionOfCurrentView,
                    HLRandWFViewsFileType = ImageFileType.PNG,
                    FilePath = filepath,
                    ShadowViewsFileType = ImageFileType.PNG
                };
                doc.ExportImage(img);

                tx.Commit();
                return Result.Succeeded;
            }
            catch (Exception)
            {
                return Result.Failed;
            }
        }
        static Result ConvertToPgm(string pathPng, string pathPgm, SettingsJson set)
        {
            try
            {
                byte black = 0;
                byte white = 254;
                byte whiteRevit = 255;
                byte grey = 205;
                byte room = 150;

                Mat originalImage = Cv2.ImRead(pathPng, ImreadModes.Grayscale);

                int targetWidth = (int)set.PgmImageExpansion_Px;
                int targetHeight = (int)set.PgmImageExpansion_Px;

                // target image with grey background
                Mat targetImage = new Mat(targetHeight, targetWidth, MatType.CV_8UC1, new Scalar(205));

                // determine position of image center
                int offsetX = (targetWidth - originalImage.Cols) / 2;
                int offsetY = (targetHeight - originalImage.Rows) / 2;

                // change greyValues
                // all pixels not grey or white to black(0)
                for (int y = 0; y < originalImage.Rows; y++)
                {
                    for (int x = 0; x < originalImage.Cols; x++)
                    {
                        byte pixelValue = originalImage.Get<byte>(y, x);
                        if (pixelValue != whiteRevit && pixelValue != grey && pixelValue != room)
                        {
                            originalImage.Set(y, x, black);
                        }
                    }
                }

                // all grey(150 rooms) pixels to white(254)
                Mat change2 = ChancheGreyValue(originalImage, room, white);
                // all white(255) pixels to grey(205)
                Mat imageAfterChange = ChancheGreyValue(change2, whiteRevit, grey);

                imageAfterChange.CopyTo(targetImage[new Rect(offsetX, offsetY, originalImage.Cols, originalImage.Rows)]);
                Cv2.ImWrite(pathPgm, targetImage);
                return Result.Succeeded;
            }
            catch (Exception)
            {
                return Result.Failed;
            }

            static Mat ChancheGreyValue(Mat image, byte oldValue, byte newValue)
            {
                for (int y = 0; y < image.Rows; y++)
                {
                    for (int x = 0; x < image.Cols; x++)
                    {
                        byte pixelValue = image.Get<byte>(y, x);
                        if (pixelValue == oldValue)
                        {
                            image.Set(y, x, newValue);
                        }
                    }
                }
                return image;
            }
        }
        static Result ConvertToPgmWithouExtension(string pathPng, string pathPgm, SettingsJson set)
        {
            try
            {
                byte black = 0;
                byte white = 254;
                byte whiteRevit = 255;
                byte grey = 205;
                byte room = 150;

                Mat originalImage = Cv2.ImRead(pathPng, ImreadModes.Grayscale);

                // change greyValues
                // all pixels not grey or white to black(0)
                for (int y = 0; y < originalImage.Rows; y++)
                {
                    for (int x = 0; x < originalImage.Cols; x++)
                    {
                        byte pixelValue = originalImage.Get<byte>(y, x);
                        if (pixelValue != whiteRevit && pixelValue != grey && pixelValue != room)
                        {
                            originalImage.Set(y, x, black);
                        }
                    }
                }

                // all grey(150 rooms) pixels to white(254)
                Mat change2 = ChancheGreyValue(originalImage, room, white);
                // all white(255) pixels to grey(205)
                Mat imageAfterChange = ChancheGreyValue(change2, whiteRevit, grey);

                Cv2.ImWrite(pathPgm, change2);
                return Result.Succeeded;
            }
            catch (Exception)
            {
                return Result.Failed;
            }

            static Mat ChancheGreyValue(Mat image, byte oldValue, byte newValue)
            {
                for (int y = 0; y < image.Rows; y++)
                {
                    for (int x = 0; x < image.Cols; x++)
                    {
                        byte pixelValue = image.Get<byte>(y, x);
                        if (pixelValue == oldValue)
                        {
                            image.Set(y, x, newValue);
                        }
                    }
                }
                return image;
            }
        }
        static Result WriteYaml(string pathYaml, SettingsJson set, XYZ bottomLeft)
        {
            string fileName = Path.GetFileNameWithoutExtension(pathYaml);
            try
            {
                string wert = $"[{bottomLeft.X.ToString(Sys.InvariantCulture)}, {bottomLeft.Y.ToString(Sys.InvariantCulture)}, 0.000000]";
                TaskDialog.Show("Message", wert);

                var yamlData = new Dictionary<string, object>
                {
                    { "image", fileName + ".pgm" },
                    { "resolution", set.PgmImageResolution_Meter.ToString(Sys.InvariantCulture) },
                    { "origin", wert },
                    { "negate", 0 },
                    { "occupied_thresh", 0.65 },
                    { "free_thresh", 0.196 }
                };
                SerializeToYaml(yamlData, pathYaml);

                return Result.Succeeded;
            }
            catch (Exception)
            {
                return Result.Failed;
            }

            void SerializeToYaml(Dictionary<string, object> data, string pathYaml)
            {
                var serializer = new SerializerBuilder().Build();

                using (var writer = new StreamWriter(pathYaml))
                {
                    serializer.Serialize(writer, data);
                }
            }
        }
    }
}
