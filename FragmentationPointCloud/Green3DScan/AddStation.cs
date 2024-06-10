﻿using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Serilog;
using Transform = Autodesk.Revit.DB.Transform;
using Path = System.IO.Path;
using Document = Autodesk.Revit.DB.Document;
using Line = Autodesk.Revit.DB.Line;

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
            
            XYZ transformedHeight = trans.Inverse.OfPoint(new XYZ(0, 0, set.HeightOfSphere_Meter)) * Constants.meter2Feet; 

            double radius = set.SphereDiameter_Meter/2 * Constants.meter2Feet;

            try
            {
                using (TransactionGroup tg = new TransactionGroup(doc, "Place Spheres"))
                {
                    tg.Start();

                    while (true)
                    {
                        // Step 1: User clicks to select a point
                        XYZ point;
                        try
                        {
                            point = uidoc.Selection.PickPoint("Click to place a sphere or press ESC to finish");
                        }
                        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                        {
                            break; // Exit the loop when ESC is pressed
                        }

                        using (Transaction tx = new Transaction(doc, "Place Sphere"))
                        {
                            tx.Start();

                            // Create a profile curve for the sphere (a semi-circle)
                            List<Curve> profileCurves = new List<Curve>();
                            XYZ center = new XYZ(point.X, point.Y, transformedHeight.Z);
                            XYZ top = new XYZ(point.X, point.Y, transformedHeight.Z + radius);
                            XYZ bottom = new XYZ(point.X, point.Y, transformedHeight.Z - radius);
                            Arc arc = Arc.Create(top, bottom, center + new XYZ(radius, 0, 0));
                            profileCurves.Add(Line.CreateBound(bottom, top));
                            profileCurves.Add(arc);

                            CurveLoop profile = CurveLoop.Create(profileCurves);

                            // Axis of revolution (Y-axis through the center point)
                            Line axis = Line.CreateBound(point, point + XYZ.BasisY);

                            // Create a revolved solid using the profile and the axis
                            Solid sphere = GeometryCreationUtilities.CreateRevolvedGeometry(
                                new Frame(point, XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ),
                                new CurveLoop[] { profile },0,2 * Math.PI);

                            // Create a DirectShape element in Revit
                            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                            ds.SetShape(new GeometryObject[] { sphere });

                            tx.Commit();
                        }
                    }

                    tg.Assimilate(); // Commit the transaction group
                }

                Level currentLevel = doc.ActiveView.GenLevel;
                string levelName = currentLevel.Name;

                // Beispielkoordinate, wo die Kugel eingefügt wird
                XYZ sphereCoordinate = new XYZ(10, 20, 30);

                //Helper.CreateAndStoreSphereData(doc, sphereCoordinate, levelName);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}