using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TruthOrDare_Contract.DTOs.UploadImageAndVideo;
using TruthOrDare_Core.Services;

namespace TruthOrDare_API.Controllers
{
    [Route("api/upload-response")]
    [ApiController]
    public class UploadChallengeController : ControllerBase
    {
        private readonly GoogleDriveService _driveService;
        private readonly YouTubeService _youTubeService;

        public UploadChallengeController(GoogleDriveService driveService, YouTubeService youTubeService)
        {
            _driveService = driveService;
            _youTubeService = youTubeService;
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

                return Ok(new { mediaUrl, mediaType });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}");
                return StatusCode(500, new { message = $"Upload failed: {ex.Message}" });
            }
        }

    }
}
