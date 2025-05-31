using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using JetBrains.Annotations;

using Serilog;

using System;
using System.Collections.Generic;
using System.IO;

using RD = Revit.Data;

namespace Revit.Green3DScan.SimulatePointCloud;

[Transaction(TransactionMode.Manual)]
[UsedImplicitly]
public class Stations2NotVisibleFaces : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // Initialization
        if (!ExternalCommandHelper.GetProjectPath(commandData, out string projectPath, out var document, out _))
        {
            TaskDialog.Show("Message", "The project file has not been saved yet.");
            return Result.Failed;
        }
        ExternalCommandHelper.InitLogger(projectPath);
        var settings = SettingsJson.ReadSettingsJson(Constants.pathSettings);

        Log.Information("start Stations2NotVisibleFaces");
        Log.Information(settings.BBox_Buffer.ToString());

        // Get transformation
        var transform = Helper.GetTransformation(document!, settings);

        Log.Information("setup");

        #region select files

        // Faces
        var fodPfRevit = new FileOpenDialog(Stations.CsvFilter)
        {
            Title = "Select CSV file with BimFaces from Revit!"
        };
        if (fodPfRevit.Show() == ItemSelectionDialogResult.Canceled) return Result.Cancelled;
        string csvPathPfRevit = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodPfRevit.GetSelectedModelPath());

        var fodRpRevit = new FileOpenDialog(Stations.CsvFilter)
        {
            Title = "Select CSV file with BimFacesPlanes fromRevit!"
        };
        if (fodRpRevit.Show() == ItemSelectionDialogResult.Canceled) return Result.Cancelled;
        string csvPathRpRevit = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodRpRevit.GetSelectedModelPath());

        #endregion select files

        Log.Information("select files");

        #region read files

        if (!RD.PlanarFace.TryReadCsv(csvPathPfRevit, out var facesRevit)
         || !RD.ReferencePlane.TryReadCsv(csvPathRpRevit, out var referencePlanesRevit))
        {
            TaskDialog.Show("Error", "Failed to read faces or reference planes CSV. Please check the log for details.");
            return Result.Failed;
        }

        var stations = Stations.CollectFromFamilyInstances(document!, transform);

        #endregion read files

        Log.Information("read files");

        #region write stations to csv

        if (!Stations.WriteCsv(projectPath, stations))
        {
            Log.Error("Error writing stations to CSV.");
            TaskDialog.Show("Error", "Failed to write stations to CSV. Please check the log for details.");
            return Result.Failed;
        }

        Log.Information(stations.Count + " stations");

        #endregion read files

        Log.Information("write stations to csv");

        #region visible and not visible faces

        var rayCast = new RayCasting(facesRevit, referencePlanesRevit, settings);
        var visibleFacesId = rayCast.VisibleFaces(stations, out var test);

        //Test 
        int y = 0;
        foreach (var item in test)
            //Log.Information(item.Key.ToString());
            //Log.Information(item.Value.ToString());
            y += item.Value;
        Log.Information(y.ToString());

        var visibleFaceId = new HashSet<RD.Id>();
        var visibleFaces = new HashSet<RD.PlanarFace>();
        var visibleRefPlanes = new HashSet<RD.ReferencePlane>();

        for (int i = 0; i < stations.Count; i++)
        {
            foreach (var id in visibleFacesId[i])
            {
                visibleFaces.Add(facesRevit[id]);
                visibleRefPlanes.Add(referencePlanesRevit[facesRevit[id].ReferencePlaneId]);
                visibleFaceId.Add(id);
            }
        }

        // visible faces
        RD.PlanarFace.WriteCsv(Path.Combine(projectPath, Stations.VisibleFacesFileName), visibleFaces);
        RD.ReferencePlane.WriteCsv(Path.Combine(projectPath, Stations.VisibleRefPlanesFileName), visibleRefPlanes);

        var notVisibleFacesId = new List<RD.Id>();
        var notVisibleFaces = new List<RD.PlanarFace>();
        var notvisibleRefPlanes = new HashSet<RD.ReferencePlane>();

        // not visible faces
        foreach (var face in facesRevit.Values)
        {
            if (!visibleFaceId.Contains(face.Id))
            {
                notVisibleFacesId.Add(face.Id);
                notvisibleRefPlanes.Add(referencePlanesRevit[face.ReferencePlaneId]);
                notVisibleFaces.Add(face);
            }
        }

        RD.PlanarFace.WriteCsv(Path.Combine(projectPath, Stations.NotVisibleFacesFileName), notVisibleFaces);
        RD.ReferencePlane.WriteCsv(Path.Combine(projectPath, Stations.NotVisibleRefPlanesFileName), notvisibleRefPlanes);

        #endregion visible and not visible faces

        Log.Information("visible and not visible faces");

        #region color not visible faces

        ElementId[] matId;

        // add materials and save the ElementIds in a DataStorage
        try
        {
            matId = Helper.AddMaterials(document!);
        }
        catch (Exception)
        {
            matId = Helper.ReadMaterialsDS(document!);
        }

        //Helper.Paint.ColourFace(doc, notVisibleFacesId, matId[0]);
        int pMin = 1;
        // TODO Test with minimum number of points on face
        //var pMin = set.StepsPerFullTurn * set.StepsPerFullTurn * set.Beta_Degree / 25000;
        Log.Information(pMin.ToString() + " pMin");
        // Test if a minimum number has been reached
        var visibleWithPMin = new List<RD.Id>();
        foreach (var pf in test)
        {
            if (pf.Value >= pMin)
            {
                visibleWithPMin.Add(pf.Key);
                //Log.Information(pf.Key.ToString());
                //Log.Information(pf.Value.ToString());
            }
            //else
            //{
            //    //Log.Information(pf.Value.ToString());
            //}
        }

        Helper.Paint.ColourFace(document!, visibleWithPMin, matId[5]);

        #endregion color not visible faces

        Log.Information("color not visible faces");
        TaskDialog.Show("Message", "finish");
        Log.Information("end Stations2NotVisibleFaces");
        return Result.Succeeded;
    }
}