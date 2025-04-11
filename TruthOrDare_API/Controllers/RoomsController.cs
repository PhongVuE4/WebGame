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
using Microsoft.AspNetCore.Http.HttpResults;

namespace TruthOrDare_API.Controllers
{
    [Route("api/rooms")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly IWebSocketHandler _websocketHandler;

        public RoomsController(IRoomService roomService, IWebSocketHandler webSocketHandler)
        {
            _roomService = roomService;
            _websocketHandler = webSocketHandler;
        }

        [HttpPost("create")]
        public async Task<ActionResult> CreateRoom([FromBody] CreateRoomRequest roomCreate)

        {
            string playerName = roomCreate.PlayerName;
            var room = await _roomService.CreateRoom(roomCreate.RoomName, roomCreate.PlayerId, playerName, roomCreate.RoomPassword, roomCreate.AgeGroup, roomCreate.Mode, roomCreate.MaxPlayer);
            return Ok(room);
        }

        [HttpPatch("{roomId}/join")]
        public async Task<IActionResult> JoinRoom(string roomId, [FromBody] JoinRoomDTO request)
        {
            bool isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            string playerName = request.PlayerName;

            if (isAuthenticated)
            {
                playerName = User.Identity.Name;
            }
            var room = await _roomService.JoinRoom(roomId, request.PlayerId, playerName, request.RoomPassword);
            return Ok(new { room.roomId, room.playerId,room.playerName});

        }
        [HttpGet("list")]
        public async Task<IActionResult> GetListRoom(string? filters)
        {
            var rooms = await _roomService.GetListRoom(filters);
            return Ok(rooms);
        }
        [HttpGet("{roomId}")]
        public async Task<IActionResult> GetRoom(string roomId)
        {
            var room = await _roomService.GetRoom(roomId);
            var roomDto = Mapper.ToRoomDetailDTO(room);
            return Ok(roomDto);
        }
        [HttpPatch("{roomId}/leave-room")]
        public async Task<ActionResult> LeaveRoom(string roomId, [FromBody] RoomActionDTO action)
        {
            var room = await _roomService.LeaveRoom(roomId, action.PlayerId);
            return Ok(new { message = room });
        }
       
        [HttpPatch("{roomId}/change-name")]
        public async Task<IActionResult> ChangeName(string roomId, [FromBody] ChangeNameInRoomDTO request)
        {
            await _roomService.ChangePlayerName(roomId, request.PlayerId, request.NewName);
            await _websocketHandler.BroadcastMessage(roomId, $"Player {request.PlayerId} has changed their name to {request.NewName}");
            return Ok(new { message = "Name changed successfully." });
        }
        [HttpPatch("{roomId}/start")]
        public async Task<ActionResult> StartGame(string roomId, [FromBody] RoomActionDTO action)
        {
                await _roomService.StartGame(roomId, action.PlayerId);
                return Ok(new {  message = "Game is started."});
        }
        [HttpPatch("{roomId}/reset-game")]
        public async Task<ActionResult> ResetGame(string roomId, [FromBody] RoomActionDTO action)
        { 
                await _roomService.ResetGame(roomId, action.PlayerId);
                return Ok(new { message = "Reset game successful." });

        }
        [HttpPatch("{roomId}/get-question")]
        public async Task<ActionResult> GetRandomQuestion(string roomId, [FromBody] RoomGetQuestionDTO action)
        {
                var (_question, _isLastQuestion, _totalQuestions, _usedQuestions) = await _roomService.GetRandomQuestionForRoom(roomId, action.PlayerId, action.QuestionType);
                if (_question == null)
                {
                    return Ok(new { message = "No more questions available. Game has ended.", isGameEnded = true });
                }
                return Ok(new
                {
                    question = _question,
                    isLastQuestion = _isLastQuestion,
                    totalQuestions = _totalQuestions,
                    usedQuestions = _usedQuestions
                });
        }
        [HttpPatch("{roomId}/next-player")]
        public async Task<ActionResult> NextPlayer(string roomId, [FromBody] RoomActionDTO action)
        {
                var (_nextPlayerId, _isGameEnded, _message) = await _roomService.NextPlayer(roomId, action.PlayerId);
            if (_isGameEnded)
            {
                return Ok(new { IsGameEnded = true, Message = _message });
            }
            return Ok(new {  nextPlayerId = _nextPlayerId, isGameEnded = false });
        }
    }
}
