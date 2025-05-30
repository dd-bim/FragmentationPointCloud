using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

using JetBrains.Annotations;

using Serilog;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using RD = Revit.Data;

namespace Revit.Green3DScan.SimulatePointCloud;

// Stations implementiert
[Transaction(TransactionMode.Manual)]
[UsedImplicitly]
public class Bim2Stations : IExternalCommand
{
    public const double MaxPlaneDist = 0.01; // meters, maximum distance for planar face

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // Initialization
        if (!ExternalCommandHelper.GetProjectPath(commandData, out string projectPath, out var document, out var uiDocument))
        {
            TaskDialog.Show("Message", "The project file has not been saved yet.");
            return Result.Failed;
        }
        ExternalCommandHelper.InitLogger(projectPath);
        var settings = SettingsJson.ReadSettingsJson(Constants.pathSettings);
        Log.Information("start Revit2Station");
        Log.Information("setup");

        if (uiDocument.ActiveView is View3D current3DView)
        {
            TaskDialog.Show("Message", "You must be in a 2D viewplan!");
            return Result.Failed;
        }

        // Get transformation
        var transform = Helper.GetTransformation(document, settings);

        #region room faces

        var activeView = document.ActiveView;

        // collect all rooms in active view
        var collRooms = new FilteredElementCollector(document, activeView.Id);
        ICollection<Element> rooms = collRooms
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .ToElements();

        var refPlanes = new Dictionary<string, RD.ReferencePlane>();
        var faces = new List<RD.PlanarFace>();
        int totalFailedFaces = 0;
        int faceId = 0;

        foreach (var roomElement in rooms)
        {
            if (roomElement is not Room room)
                continue;

            var calculator = new SpatialElementGeometryCalculator(document);
            var calcResult = calculator.CalculateSpatialElementGeometry(room);

            var geomSolid = calcResult.GetGeometry();
            string stateId = room.CreatedPhaseId.ToString();


            foreach (Face geomFace in geomSolid.Faces)
            {
                faceId += 1;
                if (geomFace is not PlanarFace planarFace)
                    continue;

                // The faces for the room cannot be coloured at the end of the analysis because the ID of the individual faces is not correct.
                string objectId = "x";
                // var faceId = "y";
                string createStateId = "TODO";

                //string convertRepresentation = e.ConvertToStableRepresentation(doc);
                //string[] tokenList = convertRepresentation.Split(new char[] { ':' });
                //var faceIdnew = Convert.ToInt64(tokenList[1]);

                // combine ID
                var id = new RD.Id(createStateId, objectId, faceId.ToString());

                var plane = Plane.CreateByOriginAndBasis(
                  transform.OfPoint(planarFace.Origin) * Constants.feet2Meter,
                  transform.OfVector(planarFace.XVector),
                  transform.OfVector(planarFace.YVector));

                var refPlane = RD.ReferencePlane.Create(plane, Constants.PlaneDigits);
                refPlanes.Add(refPlane.Id, refPlane);

                var mesh = planarFace.Triangulate();
                if (mesh == null
                    || mesh.NumTriangles < 1
                    || !RD.PlanarFace.Create(id, refPlane, mesh, transform, out var planarFaceIO, out double maxPlaneDist)
                    || (maxPlaneDist > MaxPlaneDist))
                {
                    // skipped faces
                    totalFailedFaces += 1;
                    Log.Information("new Planarface failed");
                    continue;
                }
                Log.Information("maxPlaneDist: " + maxPlaneDist);
                faces.Add(planarFaceIO!);
            }
        }

        // write faces
        string csvPlanarFaces = Path.Combine(projectPath, "roomFaces.csv");
        string csvReferencePlanes = Path.Combine(projectPath, "roomFacesRef.csv");
        RD.PlanarFace.WriteCsv(csvPlanarFaces, faces);
        RD.ReferencePlane.WriteCsv(csvReferencePlanes, refPlanes.Values);

        // write OBJ
        RD.PlanarFace.WriteObj(Path.Combine(projectPath, "roomFaces"), refPlanes, faces);


        Log.Information(faces.Count + " room faces");

        #endregion room faces

        Log.Information("room faces");

        #region stations

        var stations = new List<XYZ>();
        var listDoors = new List<ElementId>();

        // collect doors
        var collDoors = new FilteredElementCollector(document, activeView.Id);
        ICollection<Element> doors = collDoors
            .OfCategory(BuiltInCategory.OST_Doors)
            .WhereElementIsNotElementType()
            .ToElements();

        double heigth = settings.HeightOfScanner_Meter * Constants.meter2Feet;

        foreach (var door in doors)
        {
            var loc = door.Location;

            if (loc is LocationCurve locationCurve)
            {
                Log.Information("Door, but no LocationPoint.");
            }
            else if (loc is LocationPoint locationPoint)
            {
                stations.Add(new XYZ(locationPoint.Point.X, locationPoint.Point.Y, heigth));
            }
            else
            {
                listDoors.Add(door.Id);
                Log.Information(door.Id.ToString());
                Log.Information("loc null");
            }
        }

        Log.Information(stations.Count + " stations(doors)");
        Log.Information(doors.Count + " doors");

        // collect rooms
        foreach (var room in rooms)
        {
            if (room is SpatialElement spatialRoom && spatialRoom.Location is LocationPoint locationPoint)
                //var roomPoint = trans.OfPoint(locationPoint.Point) * Constants.feet2Meter;
                //stations.Add(new D3.Vector(roomPoint.X, roomPoint.Y, transformedHeight.Z));
                stations.Add(new XYZ(locationPoint.Point.X, locationPoint.Point.Y, heigth));
            else
                Log.Information("room is no SpatialElement or LocationPoint ");
        }
        Log.Information(rooms.Count + " rooms");

        #endregion stations

        Log.Information(stations.Count + " stations");

        #region ScanStation

        Stations.EnsureScanStationFamily(commandData.Application, projectPath, settings);

        if (!Stations.TryLoadAndPlaceSphereFamily(document, projectPath, stations))
        {
            TaskDialog.Show("Message", "Error loading and placing ScanStation family.");
            return Result.Failed;
        }

        #endregion ScanStation

        Log.Information("ScanStation");

        #region station to csv

        if (!Stations.WriteCsv(projectPath, stations, transform))
        {
            TaskDialog.Show("Message", "Error writing stations to CSV file.");
            return Result.Failed;
        }

        #endregion station to csv

        Log.Information("station to csv");
        var allStations = Stations.CollectFromFamilyInstances(document, transform);
        TaskDialog.Show("Message", $"{stations.Count} new ScanStations, total {allStations.Count} ScanStations.");
        Log.Information("end Revit2Station");
        return Result.Succeeded;
    }

}