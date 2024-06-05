using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using D2 = GeometryLib.Double.D2;
using D3 = GeometryLib.Double.D3;
using Serilog;
using Transform = Autodesk.Revit.DB.Transform;
using Sys = System.Globalization.CultureInfo;
using Path = System.IO.Path;
using Document = Autodesk.Revit.DB.Document;
using View = Autodesk.Revit.DB.View;
using Line = Autodesk.Revit.DB.Line;
using S = ScantraIO.Data;


namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class AddStation : IExternalCommand
    {
        string path;
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

            // Step 1: User clicks to select a point
            XYZ point;
            try
            {
                point = uidoc.Selection.PickPoint("Click to place a sphere");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            // Step 2: Create a sphere at the clicked point
            using (Transaction tx = new Transaction(doc, "Place Sphere"))
            {
                tx.Start();

                // Sphere parameters
                double radius = 5.0; // Set the desired radius

                // Create a profile curve for the sphere (a semi-circle)
                List<Curve> profileCurves = new List<Curve>();
                XYZ center = new XYZ(point.X, point.Y, point.Z);
                XYZ top = new XYZ(point.X, point.Y, point.Z + radius);
                XYZ bottom = new XYZ(point.X, point.Y, point.Z - radius);
                Arc arc = Arc.Create(top, bottom, center + new XYZ(radius, 0, 0));
                profileCurves.Add(Line.CreateBound(bottom, top));
                profileCurves.Add(arc);

                CurveLoop profile = CurveLoop.Create(profileCurves);

                // Axis of revolution (Y-axis through the center point)
                Line axis = Line.CreateBound(point, point + XYZ.BasisY);

                // Create a revolved solid using the profile and the axis
                Solid sphere = GeometryCreationUtilities.CreateRevolvedGeometry(
                    new Frame(point, XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ),
                    new CurveLoop[] { profile },
                    0,
                    2 * Math.PI);

                // Create a DirectShape element in Revit
                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.SetShape(new GeometryObject[] { sphere });

                tx.Commit();
            }

            return Result.Succeeded;

            //for (int i = 0; i < stations.Count; i++)
            //{

            //    // sphere
            //    List<Curve> profile = new List<Curve>();
            //    XYZ station = new XYZ(stations[i].x, stations[i].y, stations[i].z);
            //    var stationTransform = trans.OfVector(station);
            //    XYZ center = stationTransform * Constants.meter2Feet;
            //    double radius = 0.15 * Constants.meter2Feet;
            //    XYZ profile00 = center;
            //    XYZ profilePlus = center + new XYZ(0, radius, 0);
            //    XYZ profileMinus = center - new XYZ(0, radius, 0);

            //    profile.Add(Line.CreateBound(profilePlus, profileMinus));
            //    profile.Add(Arc.Create(profileMinus, profilePlus, center + new XYZ(radius, 0, 0)));

            //    CurveLoop curveLoop = CurveLoop.Create(profile);
            //    SolidOptions options = new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId);

            //    Frame frame = new Frame(center, XYZ.BasisX, -XYZ.BasisZ, XYZ.BasisY);
            //    if (Frame.CanDefineRevitGeometry(frame) == true)
            //    {
            //        Solid sphere = GeometryCreationUtilities.CreateRevolvedGeometry(frame, new CurveLoop[] { curveLoop }, 0, 2 * Math.PI, options);
            //        using Transaction t = new Transaction(doc, "Create sphere direct shape");
            //        t.Start();
            //        // create direct shape and assign the sphere shape
            //        DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            //        ds.ApplicationId = "Application id";
            //        ds.ApplicationDataId = "Geometry object id";
            //        ds.SetShape(new GeometryObject[] { sphere });
            //        t.Commit();
            //    }
            //}
            //TaskDialog.Show("Message", "Fertig");
            return Result.Succeeded;
        }

        private static List<D3.LineString> CurveLoops(Face face, Transform trans)
        {
            var rings = new List<D3.LineString>();
            IList<CurveLoop> curveLoops = face.GetEdgesAsCurveLoops();
            // exteriors and interiors
            for (int i = 0; i < curveLoops.Count; i++)
            {
                var vertices = new List<D3.Vector>();
                CurveLoop curveLoop = curveLoops[i];
                foreach (Curve curve in curveLoop)
                {
                    XYZ pntStart = trans.OfPoint(curve.GetEndPoint(0)) * Constants.feet2Meter;
                    vertices.Add(new D3.Vector(pntStart.X, pntStart.Y, pntStart.Z));
                }
                vertices.Add(vertices[0]);
                // List umkehren
                //vertices.Reverse();
                var linestr = new D3.LineString(vertices);
                rings.Add(linestr);
            }
            return rings;
        }
        private List<XYZ> GetRoomCenters(Document doc)
        {
            List<XYZ> roomCenters = new List<XYZ>();

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms).OfClass(typeof(SpatialElement));

            foreach (Element element in collector)
            {
                SpatialElement room = element as SpatialElement;
                if (room != null)
                {
                    LocationPoint locationPoint = room.Location as LocationPoint;
                    if (locationPoint != null)
                    {
                        roomCenters.Add(locationPoint.Point);
                    }
                }
            }

            return roomCenters;
        }

    }
}