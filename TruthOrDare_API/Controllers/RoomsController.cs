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
            string playerName = roomCreate.PlayerName;
            var room = await _roomService.CreateRoom(roomCreate.RoomName, playerName, roomCreate.RoomPassword, roomCreate.MaxPlayer);
            return Ok(room);
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinRoom([FromBody] JoinRoomDTO request)
        {
            bool isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            string playerName = request.PlayerName;

            if (isAuthenticated)
            {
                playerName = User.Identity.Name;
            }

            var room = await _roomService.JoinRoom(request.RoomId, playerName, request.RoomPassword);
            return Ok(room);

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
                var roomDto = Mapper.ToRoomCreateDTO(room);
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
            await _roomService.ChangePlayerName(request.RoomId, request.PlayerId, request.NewName);
            await _websocketHandler.BroadcastMessage(request.RoomId, $"Player {request.PlayerId} has changed their name to {request.NewName}");
            return Ok(new { Message = "Name changed successfully." });
        }
    }
}
