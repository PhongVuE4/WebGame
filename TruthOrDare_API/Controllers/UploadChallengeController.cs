using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using TruthOrDare_Contract.DTOs.UploadImageAndVideo;
using TruthOrDare_Contract.Models;
using TruthOrDare_Core.Hubs;
using TruthOrDare_Core.Services;
using TruthOrDare_Infrastructure;

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
        private readonly IMongoCollection<Room> _rooms;

        public UploadChallengeController(GoogleDriveService driveService, 
            YouTubeService youTubeService, 
            IHubContext<RoomHub> hubContext,
            MongoDbContext dbContext)
        {
            _driveService = driveService;
            _youTubeService = youTubeService;
            _hubContext = hubContext;
            _rooms = dbContext.Rooms;
        }
        [HttpPost("upload-image-video")]
        public async Task<IActionResult> UploadImageVideo([FromForm] UploadChallengeRequest request)
        {
            if (string.IsNullOrEmpty(request.RoomId) || string.IsNullOrEmpty(request.PlayerId) || string.IsNullOrEmpty(request.QuestionId) || request.File == null)
            {
                return StatusCode(422, new { message = "Room ID, player ID, question ID, and file are required" });
            }

            if (request.File.Length > 128 * 1024 * 1024) // Giới hạn 128MB
            {
                return BadRequest(new { message = "File size exceeds 128MB limit" });
            }

            var room = await _rooms.Find(r => r.RoomId == request.RoomId && !r.IsDeleted).FirstOrDefaultAsync();
            if (room == null)
            {
                return NotFound(new { message = "Room not found" });
            }
            if (room.Status.ToLower() != "playing")
            {
                return BadRequest(new { message = "Game must be playing" });
            }
            if (room.CurrentPlayerIdTurn != request.PlayerId)
            {
                return StatusCode(403, new { message = "Not your turn" });
            }

            var historyItem = room.History.LastOrDefault(h => h.PlayerId == request.PlayerId && h.Questions.Any(q => q.QuestionId == request.QuestionId) && h.Status == "assigned");
            if (historyItem == null)
            {
                return NotFound(new { message = "No assigned question found for this player" });
            }

            string mediaUrl;
            string responseType = request.File.ContentType.StartsWith("image/") ? "image" : request.File.ContentType.StartsWith("video/") ? "video" : null;
            if (responseType == null)
            {
                return StatusCode(422, new { message = "Unsupported file type. Only images and videos are allowed." });
            }

            try
            {
                using (var stream = request.File.OpenReadStream())
                {
                    if (responseType == "image")
                    {
                        mediaUrl = await _driveService.UploadFile(stream, request.File.FileName, request.File.ContentType);
                    }
                    else
                    {
                        mediaUrl = await _youTubeService.UploadVideo(
                            stream,
                            $"Challenge from {request.PlayerId} in room {request.RoomId}",
                            $"Submitted on {DateTime.UtcNow.ToString("o")}"
                        );
                    }
                }

                // Cập nhật SessionHistory
                historyItem.Status = "answered";
                historyItem.ResponseUrl = mediaUrl;
                historyItem.ResponseType = responseType;
                historyItem.Timestamp = DateTime.Now;

                room.LastQuestionTimestamp = null; // Cho phép lấy câu hỏi mới
                await _rooms.ReplaceOneAsync(r => r.RoomId == request.RoomId, room);

                // Thông báo qua SignalR
                await _hubContext.Clients.Group(request.RoomId).SendAsync("AnswerSubmitted", new
                {
                    roomId = request.RoomId,
                    playerId = request.PlayerId,
                    playerName = room.Players.FirstOrDefault(p => p.PlayerId == request.PlayerId)?.PlayerName,
                    questionId = request.QuestionId,
                    response = (string)null,
                    responseUrl = mediaUrl,
                    responseType,
                    timestamp = DateTime.UtcNow.ToString("o")
                });

                return Ok(new { mediaUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}");
                return StatusCode(500, new { message = $"Upload failed: {ex.Message}" });
            }
        }

        //private async Task<int> CalculatePoints(string questionId)
        //{
        //    var question = await _questionRepository.GetQuestionByIdAsync(questionId);
        //    return question?.Points ?? 0;
        //}
        [HttpPost("upload-image-video-message")]
        public async Task<IActionResult> UploadMessage([FromForm] UploadMessage request)
        
        {
            Console.WriteLine($"Incoming request: FileName={request.File?.FileName}, ContentType={request.File?.ContentType}");

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
                await _hubContext.Clients.Group(request.RoomId).SendAsync("MessageUploaded", new
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
