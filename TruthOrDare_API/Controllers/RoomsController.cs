using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TruthOrDare_Contract.IServices;
using TruthOrDare_Contract;
using TruthOrDare_Infrastructure;
using MongoDB.Driver;
using TruthOrDare_Contract.DTOs.Room;
using TruthOrDare_Core.Services;
using TruthOrDare_Contract.Models;
using TruthOrDare_Common;

namespace TruthOrDare_API.Controllers
{
    [Route("api/rooms")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly MongoDbContext _dbContext;
        private readonly IWebSocketHandler _websocketHandler;

        public RoomsController(IRoomService roomService, MongoDbContext dbContext, IWebSocketHandler webSocketHandler)
        {
            _roomService = roomService;
            _dbContext = dbContext;
            _websocketHandler = webSocketHandler;
        }

        [HttpPost("create")]
        public async Task<ActionResult> CreateRoom([FromBody] CreateRoomRequest roomCreate)
        {
            try
            {
                string playerName = roomCreate.PlayerName;
                var room = await _roomService.CreateRoom(roomCreate.RoomName, playerName, roomCreate.RoomPassword, roomCreate.AgeGroup, roomCreate.Mode);
                return Ok(room);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
        [HttpPost("join")]
        public async Task<IActionResult> JoinRoom(string roomId, string? playerName, string? roomPassword)
        {
            try
            {
                bool isAuthenticated = User.Identity?.IsAuthenticated ?? false;
                string playerNameRandom = playerName;

                if (isAuthenticated)
                {
                    playerNameRandom = User.Identity.Name;
                }

                var room = await _roomService.JoinRoom(roomId, playerNameRandom, roomPassword);
                return Ok(room);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
        [HttpPost("start")]
        public async Task<ActionResult> StartGame(string roomId, string playerId)
        {
            try
            {
                await _roomService.StartGame(roomId, playerId);
                return Ok("Game is started.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }

        }
        [HttpPost("reset-game")]
        public async Task<ActionResult> ResetGame(string roomId, string playerId)
        {
            try
            {
                await _roomService.ResetGame(roomId, playerId);
                return Ok("Reset game successful.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }

        }
        [HttpPost("get-question")]
        public async Task<ActionResult> GetRandomQuestion(string roomId, string playerId, string questionType)
        {
            try
            {
                var question = await _roomService.GetRandomQuestionForRoom(roomId, playerId, questionType);
                if (question == null)
                {
                    return Ok(new { Message = "No more questions available. Game has ended." });
                }
                return Ok(question);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
        //[HttpGet("complete-turn")]
        //public async Task<IActionResult> CompleteTurn(string roomId, string playerId, string response)
        //{
        //    await _roomService.CompleteTurnAsync(roomId, playerId, response);
        //    return Ok();
        //}
        [HttpGet("list")]
        public async Task<IActionResult> GetListRoom(string? roomId)
        {
            try
            {
                var rooms = await _roomService.GetListRoom(roomId);
                return Ok(rooms);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
        [HttpGet("{roomId}")]
        public async Task<IActionResult> GetRoom(string roomId)
        {
            try
            {
                var room = await _roomService.GetRoom(roomId);
                var roomDto = Mapper.ToRoomCreateDTO(room);
                return Ok(roomDto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
        [HttpPost("leave-room")]
        public async Task<ActionResult> LeaveRoom(string roomId, string playerId)
        {
            try
            {
                var room = await _roomService.LeaveRoom(roomId, playerId);
                var roomDto = Mapper.ToRoomCreate(room);
                return Ok(roomDto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
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
        [HttpPost("rooms/change-name")]
        public async Task<IActionResult> ChangeName([FromBody] ChangeNameInRoomDTO request)
        {
            try
            {
                await _roomService.ChangePlayerName(request.RoomId, request.PlayerId, request.NewName);
                await _websocketHandler.BroadcastMessage(request.RoomId, $"Player {request.PlayerId} has changed their name to {request.NewName}");
                return Ok(new { Message = "Name changed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
        
    }
}
