using System.IO;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
/*

using Serilog;
using System;

namespace Revit.GUI
{
    [Transaction(TransactionMode.Manual)]
    public class CmdIfc2Ccv : IExternalCommand
    {
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
            Log.Information(set.ConstructionTolerance_Meter.ToString());
            #endregion setup
            #region conversion
            // conversion Tims-CSV in CPM-CSV
            var fodPfRevit = new FileOpenDialog("CSV file (*.csv)|*.csv");
            fodPfRevit.Title = "Select CSV file with IFC Faces and Planes!";
            if (fodPfRevit.Show() == ItemSelectionDialogResult.Canceled)
            {
                return Result.Cancelled;
            }
            var csvPathPfRevit = ModelPathUtils.ConvertModelPathToUserVisiblePath(fodPfRevit.GetSelectedModelPath());

            var ifcFaces = S.PlanarFace.ReadCsv(csvPathPfRevit, out Dictionary<string, S.ReferencePlane> dicIfcPlanes, out Dictionary<S.Id, double> maxPlaneDists, out string[] t, out string error);
            var ifcPlanes = new List<S.ReferencePlane>();
            foreach (var item in dicIfcPlanes)
            {
                ifcPlanes.Add(item.Value);
            }
            S.PlanarFace.WriteCsv(Path.Combine(path, "IFCFaces.csv"), ifcFaces);
            S.ReferencePlane.WriteCsv(Path.Combine(path, "IFCPlanes.csv"), ifcPlanes);
            S.PlanarFace.WriteObj(Path.Combine(path, "IFCObject"), dicIfcPlanes, ifcFaces);
            #endregion
            return Result.Succeeded;
        }
    }
}
*/