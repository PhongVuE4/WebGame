using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TruthOrDare_Contract.IServices;
using TruthOrDare_Contract;
using TruthOrDare_Infrastructure;
using MongoDB.Driver;

namespace TruthOrDare_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly MongoDbContext _dbContext;

        public GameController(IRoomService roomService, MongoDbContext dbContext)
        {
            _roomService = roomService;
            _dbContext = dbContext;
        }

        [HttpGet("create-room")]
        public async Task<IActionResult> CreateRoom()
        {
            var roomId = Guid.NewGuid().ToString();
            await _roomService.CreateRoomAsync(roomId, new List<string> { "player_001", "player_002" });
            return Ok(new { RoomId = roomId });
        }

        [HttpGet("complete-turn")]
        public async Task<IActionResult> CompleteTurn(string roomId, string playerId, string response)
        {
            await _roomService.CompleteTurnAsync(roomId, playerId, response);
            return Ok();
        }
        [HttpGet("questions")]
        public async Task<IActionResult> GetQuestions()
        {
            var questions = await _dbContext.Questions
                .Find(_ => true)
                .Limit(10) // Giới hạn 10 câu hỏi để test
                .ToListAsync();
            return Ok(questions);
        }

        [HttpGet("game-sessions")]
        public async Task<IActionResult> GetGameSessions()
        {
            var gameSessions = await _dbContext.GameSessions
                .Find(_ => true)
                .Limit(10) // Giới hạn 10 session để test
                .ToListAsync();
            return Ok(gameSessions);
        }
    }
}
