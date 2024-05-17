using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Except = Autodesk.Revit.Exceptions;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using Document = Autodesk.Revit.DB.Document;
using Serilog;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.ApplicationServices;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class RouteStations : IExternalCommand
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

            Transform trans = Helper.GetTransformation(doc, set);

            // room centers
            FilteredElementCollector roomCollector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType();

            // list for storing the room centers
            List<XYZ> roomCenters = new List<XYZ>();

            // loop through all room elements to analyse their geometry and obtain the center
            foreach (var room in roomCollector)
            {
                //var boundaryOptions = new SpatialElementBoundaryOptions();
                //var roomBoundaries = room.get_Geometry(boundaryOptions);
                Options opt = new Options();
                var boundaries = room.get_Geometry(opt);
                var x = boundaries.GetType();
            }
            List<List<Curve>> allBoundaries = new List<List<Curve>>();

            //// process the resulting room contour lines
            //foreach (var boundary
            //in roomBoundaries)
            //{
            //    List<Curve> boundaryCurves = new List<Curve>();

            //    foreach (var curveInfo in boundarySegment)
            //    {
            //        Curve curve = curveInfo.GetCurve();
            //        boundaryCurves.Add(curve);
            //    }

            //    allBoundaries.Add(boundaryCurves);
            //}

            //TaskDialog.Show("Message", "Start Stations");

            //ElementCategoryFilter doorCategoryFilter = new ElementCategoryFilter(BuiltInCategory.OST_Doors);

            //// Filter, um alle Tür-Elemente im Dokument zu erhalten
            //FilteredElementCollector doorCollector = new FilteredElementCollector(doc)
            //    .WherePasses(doorCategoryFilter)
            //    .WhereElementIsNotElementType();

            //List<XYZ> doorCenters = new List<XYZ>();

            //Options options = new Options();
            //options.ComputeReferences = true;
            //options.IncludeNonVisibleObjects = true;
            //var currentView = doc.ActiveView;n
            //foreach (Element door in doorCollector)
            //{
            //    if (door.Location is LocationPoint)
            //    {
            //        LocationPoint centerLoc = (LocationPoint)door.Location;
            //        var w = trans.OfPoint(centerLoc.Point) * Constants.feet2Meter;
            //        TaskDialog.Show("door middlepoint", "X = " + w.X + ", Y = " + w.Y + ", Z = " + w.Z);
            //    }

            //    var bbox = door.get_BoundingBox(currentView);
            //    var center = ((bbox.Max+bbox.Min) / 2) *Constants.feet2Meter;
            //    doorCenters.Add(center);
            //}

            //foreach (XYZ center in doorCenters)
            //{
            //    TaskDialog.Show("door middlepoint", "X = " + center.X + ", Y = " + center.Y + ", Z = " + center.Z);
            //}



            try
            {
                
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
        
    }
}