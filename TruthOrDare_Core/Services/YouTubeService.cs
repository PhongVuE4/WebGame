using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Common.Exceptions.ImageAndVideo;

namespace TruthOrDare_Core.Services
{
    public class YouTubeService
    {
        private readonly IConfiguration _configuration;
        public YouTubeService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        private async Task<UserCredential> GetCredential()
        {
            var clientId = _configuration["GoogleAuth:ClientId"];
            var clientSecret = _configuration["GoogleAuth:ClientSecret"];
            var refreshToken = _configuration["GoogleAuth:RefreshToken"];

            Console.WriteLine($"ClientId: {clientId}");
            Console.WriteLine($"ClientSecret: {clientSecret}");
            Console.WriteLine($"RefreshToken: {refreshToken}");
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(refreshToken))
            {
                throw new Exception("Missing Google Auth configuration (ClientId, ClientSecret, or RefreshToken)");
            }

            // Tạo OAuth2 credential từ ClientSecrets
            var clientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            // Khởi tạo credential với refresh_token
            var initializer = new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = new[] { Google.Apis.YouTube.v3.YouTubeService.Scope.YoutubeUpload }
            };
            var flow = new GoogleAuthorizationCodeFlow(initializer);
            var token = new TokenResponse
            {
                RefreshToken = refreshToken
            };

            var credential = new UserCredential(flow, "user", token);

            // Làm mới access token
            var tokenRefreshed = await credential.RefreshTokenAsync(CancellationToken.None);
            if (!tokenRefreshed)
            {
                throw new Exception("Failed to refresh access token. Please verify RefreshToken validity.");
            }
            Console.WriteLine("Access token refreshed successfully.");
            return credential;

        }

        public async Task<string> UploadVideo(Stream fileStream, string title, string description)
        {
            var credential = await GetCredential();
            var youtubeService = new Google.Apis.YouTube.v3.YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "TruthOrDareGame"
            });

            var video = new Google.Apis.YouTube.v3.Data.Video
            {
                Snippet = new Google.Apis.YouTube.v3.Data.VideoSnippet
                {
                    Title = title,
                    Description = description,
                    CategoryId = "22" // People & Blogs
                },
                Status = new Google.Apis.YouTube.v3.Data.VideoStatus
                {
                    PrivacyStatus = "unlisted"
                }
            };

            using var fileStreamCopy = new MemoryStream();
            await fileStream.CopyToAsync(fileStreamCopy);
            fileStreamCopy.Position = 0;

            var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStreamCopy, "video/*");
            videosInsertRequest.ProgressChanged += (progress) =>
            {
                switch (progress.Status)
                {
                    case UploadStatus.Uploading:
                        Console.WriteLine($"Uploaded {progress.BytesSent} bytes");
                        break;
                    case UploadStatus.Completed:
                        Console.WriteLine("Upload completed!");
                        break;
                    case UploadStatus.Failed:
                        Console.WriteLine($"Upload failed: {progress.Exception?.Message}");
                        break;
                }
            };
            await videosInsertRequest.UploadAsync();
            var response = videosInsertRequest.ResponseBody;
            if (response == null || string.IsNullOrEmpty(response.Id))
            {
                Console.WriteLine($"Upload failed: {response}");
                throw new UploadVideoFailed();
            }

            return $"https://www.youtube.com/embed/{response.Id}";
        }
    }
}
