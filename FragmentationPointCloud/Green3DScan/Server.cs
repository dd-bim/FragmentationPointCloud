using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Except = Autodesk.Revit.Exceptions;
using Serilog;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class Server : IExternalCommand
    {
        private static readonly HttpClient client = new HttpClient();

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

            string pcdPath;
            string csvPath;

            string ifcBoxPath = Path.Combine(path, "10_FragmentationIFC\\");
            string rcpOutputPath = Path.Combine(path, "10_FragmentationIFC");
            if (!Directory.Exists(ifcBoxPath))
            {
                Directory.CreateDirectory(ifcBoxPath);
            }

            try
            {
                // step 1: select pcd
                FileOpenDialog pcd = new FileOpenDialog("PCD file (*.pcd)|*.pcd");
                pcd.Title = "Select PCD file!";
                if (pcd.Show() == ItemSelectionDialogResult.Canceled)
                {
                    return Result.Cancelled;
                }
                pcdPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(pcd.GetSelectedModelPath());

                // step 2: select csv
                FileOpenDialog csv = new FileOpenDialog("CSV file (*.csv)|*.csv");
                csv.Title = "Select CSV file!";
                if (csv.Show() == ItemSelectionDialogResult.Canceled)
                {
                    return Result.Cancelled;
                }
                csvPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(csv.GetSelectedModelPath());
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

            try
            {
                var userUuid = "d5f3ce37-8537-45a8-a673-a1de6e6dedc1";

                UploadFileToWebApp(pcdPath, userUuid).Wait();
                UploadFileToWebApp(csvPath, userUuid).Wait();
            }
            catch (Exception)
            {
                throw;
            }

            try
            {
                var userUuid = "d5f3ce37-8537-45a8-a673-a1de6e6dedc1"; // UUID setzen
                string localSavePath = Path.Combine(path, "downloadedFile.zip"); // Lokalen Speicherpfad setzen

                // Warte synchron auf den Abschluss des Downloads
                DownloadFileFromWebApp(userUuid, localSavePath).Wait();

                TaskDialog.Show("Message", "Datei erfolgreich heruntergeladen und gespeichert: " + localSavePath);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Fehler", $"Fehler beim Herunterladen: {ex.Message}");
                return Result.Failed;
            }

            TaskDialog.Show("Message", "Server successful!");
            return Result.Succeeded;
            #endregion catch
        }
        #endregion execute
        public async Task UploadFileToWebApp(string filePath, string userUuid)
        {
            try
            {
                MultipartFormDataContent form = new MultipartFormDataContent();
                HttpContent fileStreamContent = new StreamContent(File.OpenRead(filePath));
                form.Add(fileStreamContent, "file", Path.GetFileName(filePath));

                client.DefaultRequestHeaders.Add("Cookie", "userUuid=" + userUuid);

                HttpResponseMessage response = await client.PostAsync("https://green3dscan.dd-bim.org/upload", form);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                TaskDialog.Show("Erfolg", "Datei hochgeladen: " + responseBody);
            }
            catch (HttpRequestException e)
            {
                TaskDialog.Show("Fehler", $"Fehler beim Hochladen: {e.Message}");
            }
        }

        public async Task DownloadFileFromWebApp(string userUuid, string localSavePath)
        {
            try
            {
                // Füge den Cookie mit der UUID hinzu, falls erforderlich
                client.DefaultRequestHeaders.Add("Cookie", "userUuid=" + userUuid);

                // Sende die GET-Anfrage an den Download-Endpunkt
                HttpResponseMessage response = await client.GetAsync("https://green3dscan.dd-bim.org/download");

                // Sicherstellen, dass die Anfrage erfolgreich war
                response.EnsureSuccessStatusCode();

                // Lade den Inhalt der Antwort als Stream herunter
                using (var fileStream = new FileStream(localSavePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // Schreibe den Stream in die lokale Datei
                    await response.Content.CopyToAsync(fileStream);
                }

                TaskDialog.Show("Erfolg", "Datei erfolgreich heruntergeladen: " + localSavePath);
            }
            catch (HttpRequestException e)
            {
                TaskDialog.Show("Fehler", $"Fehler beim Herunterladen: {e.Message}");
            }
        }
    }
}