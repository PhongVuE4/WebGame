using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Common.Exceptions.ImageAndVideo;

namespace TruthOrDare_Core.Services
{
    public class GoogleDriveService
    {
        private readonly DriveService _driveService;
        private readonly string _folderId;

        public GoogleDriveService(IConfiguration configuration)
        {
            // Đường dẫn đến file JSON của Service Account
            //var credentialsPath = configuration["Drive:CredentialsPath"];
            var credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS");
            _folderId = configuration["Drive:FolderId"];

           //using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
            //var credential = GoogleCredential.FromFile(credentialsPath)
            var credential = GoogleCredential.FromJson(credentialsPath)
                .CreateScoped(DriveService.Scope.DriveFile);

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "TruthOrDareGame"
            });
        }

        public async Task<string> UploadFile(Stream fileStream, string fileName, string mimeType)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
                Parents = new List<string> { _folderId } // Thay bằng ID thư mục
            };

            var request = _driveService.Files.Create(fileMetadata, fileStream, mimeType);
            request.Fields = "id";

            var uploadProgress = await request.UploadAsync();
            if (uploadProgress.Status != UploadStatus.Completed)
            {
                Console.WriteLine($"Upload failed: {uploadProgress.Exception?.Message}");
                throw new UploadImageFailed();
            }

            var file = request.ResponseBody;
            if (file == null)
            {
                throw new Exception("Failed to retrieve file metadata");
            }
            // Cấp quyền public cho file
            var permission = new Google.Apis.Drive.v3.Data.Permission
            {
                Role = "reader",
                Type = "anyone"
            };

            await _driveService.Permissions.Create(permission, file.Id).ExecuteAsync();

            return $"https://drive.google.com/uc?id={file.Id}";
        }

    }
}
