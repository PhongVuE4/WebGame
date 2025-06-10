using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TruthOrDare_Contract.DTOs.UploadImageAndVideo;
using TruthOrDare_Core.Hubs;
using TruthOrDare_Core.Services;

namespace TruthOrDare_API.Controllers
{
    [Route("api/upload-response")]
    [ApiController]
    public class UploadChallengeController : ControllerBase
    {
        private readonly GoogleDriveService _driveService;
        private readonly YouTubeService _youTubeService;
        private readonly IHubContext<RoomHub> _hubContext;
        private readonly HttpClient _httpClient = new HttpClient();

        public UploadChallengeController(GoogleDriveService driveService, YouTubeService youTubeService, IHubContext<RoomHub> hubContext)
        {
            _driveService = driveService;
            _youTubeService = youTubeService;
            _hubContext = hubContext;
        }

        [HttpPost("upload-image-video")]
        public async Task<IActionResult> UploadChallenge([FromForm] UploadChallengeRequest request)
        {
            if (request.File == null || string.IsNullOrEmpty(request.RoomId) || string.IsNullOrEmpty(request.PlayerId))
            {
                return BadRequest(new { message = "Missing file, roomId, or playerId" });
            }

            if (request.File.Length > 128 * 1024 * 1024)
            {
                return BadRequest(new { message = "File size exceeds 128MB limit" });
            }

            try
            {
                string mediaUrl;
                string mediaType = request.File.ContentType;

                using var stream = request.File.OpenReadStream();
                if (request.File.ContentType.StartsWith("image"))
                {
                    mediaUrl = await _driveService.UploadFile(stream, request.File.FileName, request.File.ContentType);
                }
                else if (request.File.ContentType.StartsWith("video"))
                {
                    mediaUrl = await _youTubeService.UploadVideo(
                        stream,
                        $"Challenge from {request.PlayerId} in room {request.RoomId}",
                        $"Submitted on {DateTime.UtcNow.ToString("o")}"
                    );
                }
                else
                {
                    return BadRequest(new { message = "Unsupported file type" });
                }
                // Gửi thông báo qua SignalR đến tất cả client trong phòng
                await _hubContext.Clients.Group(request.RoomId).SendAsync("ChallengeUploaded", new
                {
                    roomId = request.RoomId,
                    playerId = request.PlayerId,
                    mediaUrl,
                    fileName = request.File.FileName,
                    mediaType,
                    uploadTime = DateTime.UtcNow.ToString("o")
                });

                return Ok(new { mediaUrl, mediaType });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}");
                return StatusCode(500, new { message = $"Upload failed: {ex.Message}" });
            }
        }

        [HttpGet("proxy-image")]
        public async Task<IActionResult> ProxyImage(string id)
        {
            try
            {
                var url = $"https://drive.google.com/uc?id={id}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg"; // Giả định type mặc định
                return File(content, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Proxy failed: {ex.Message}" });
            }
        }
    }
}
