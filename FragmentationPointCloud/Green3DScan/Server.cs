//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using JetBrains.Annotations;
//using Serilog;
//using System;
//using System.Globalization;
//using System.IO;
//using System.Net.Http;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using System.Windows.Shapes;
//using Except = Autodesk.Revit.Exceptions;
//using Path = System.IO.Path;
//using TaskDialog = Autodesk.Revit.UI.TaskDialog;

//namespace Revit.Green3DScan
//{
//    [Transaction(TransactionMode.Manual)]
//    [UsedImplicitly]
//    public class Server : IExternalCommand
//    {
//        private const string RequestUri = "https://green3dscan.dd-bim.org/upload";
//        private const string UserUuid = "d5f3ce37-8537-45a8-a673-a1de6e6dedc1";
//        private const string OutputFolder = "10_FragmentationIFC";
//        private const string DownloadedFileName = "downloadedFile.zip";
 
//        private static readonly HttpClient Client = new();

//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            if (!ExternalCommandHelper.GetProjectPath(commandData, out string projectPath, out _, out _))
//            {
//                TaskDialog.Show("Message", "The project file has not been saved yet.");
//                return Result.Failed;
//            }
//            ExternalCommandHelper.InitLogger(projectPath);
//            var settings = SettingsJson.ReadSettingsJson(Constants.pathSettings);

//            Log.Information("start server");
//            Log.Information("BBox_Buffer: {BBox_Buffer}", settings.BBox_Buffer.ToString(CultureInfo.InvariantCulture));

//            return DoExecute(projectPath);
//        }

//        private static Result DoExecute(string projectPath)
//        {
//            string ifcBoxPath = Path.Combine(projectPath, OutputFolder);
//            if (!Directory.Exists(ifcBoxPath))
//                Directory.CreateDirectory(ifcBoxPath);

//            // Get the path to the PCD file from the user
//            if (!ExternalCommandHelper.GetFilePathDialog("Select PCD file!",
//                    "PCD file(*.pcd) | *.pcd", out string pcdPath))
//            {
//                TaskDialog.Show("Message", "No PCD file selected.");
//                return Result.Failed;
//            }

//            // Get the path to the CSV file from the user
//            if (!ExternalCommandHelper.GetFilePathDialog("Select CSV file!",
//                    "CSV file (*.csv)|*.csv", out string csvPath))
//            {
//                TaskDialog.Show("Message", "No CSV file selected.");
//                return Result.Failed;
//            }

//            UploadFileToWebApp(pcdPath, UserUuid).Wait();
//            UploadFileToWebApp(csvPath, UserUuid).Wait();

//            // Download the file from the web app
//            string localSavePath = Path.Combine(projectPath, DownloadedFileName);
//            DownloadFileFromWebApp(UserUuid, localSavePath).Wait();

//            TaskDialog.Show("Message", "Server successful!");
//            return Result.Succeeded;
//        }

//        private static async Task UploadFileToWebApp(string filePath, string userUuid)
//        {
//            try
//            {
//                var form = new MultipartFormDataContent();
//                HttpContent fileStreamContent = new StreamContent(File.OpenRead(filePath));
//                form.Add(fileStreamContent, "file", Path.GetFileName(filePath));

//                using var request = new HttpRequestMessage(HttpMethod.Post, RequestUri);
//                request.Content = form;
//                request.Headers.Add("Cookie", "userUuid=" + userUuid);

//                var response = await Client.SendAsync(request);
//                response.EnsureSuccessStatusCode();
//                string responseBody = await response.Content.ReadAsStringAsync();
//                TaskDialog.Show("Success", "File uploaded: " + responseBody);
//            }
//            catch (HttpRequestException e)
//            {
//                TaskDialog.Show("Error", $"File upload failed: {e.Message}");
//            }
//        }

//        private static async Task DownloadFileFromWebApp(string userUuid, string localSavePath)
//        {
//            try
//            {
//                using var request = new HttpRequestMessage(HttpMethod.Get, RequestUri);
//                request.Headers.Add("Cookie", "userUuid=" + userUuid);

//                var response = await Client.SendAsync(request);
//                response.EnsureSuccessStatusCode();

//                await using (var fileStream = new FileStream(localSavePath, FileMode.Create, FileAccess.Write, FileShare.None))
//                    await response.Content.CopyToAsync(fileStream);

//                TaskDialog.Show("Success", "File downloaded to: " + localSavePath);
//            }
//            catch (HttpRequestException e)
//            {
//                TaskDialog.Show("Error", $"File download failed: {e.Message}");
//            }
//        }

//    }
//}