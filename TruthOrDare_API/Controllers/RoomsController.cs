﻿using Microsoft.AspNetCore.Http;
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
        private readonly IWebSocketHandler _websocketHandler;

        public RoomsController(IRoomService roomService, IWebSocketHandler webSocketHandler)
        {
            _roomService = roomService;
            _websocketHandler = webSocketHandler;
        }

        [HttpPost("create")]
        public async Task<ActionResult> CreateRoom([FromBody] CreateRoomRequest roomCreate)
        {
            {
                string playerName = roomCreate.PlayerName;
                var room = await _roomService.CreateRoom(roomCreate.RoomName, playerName, roomCreate.RoomPassword, roomCreate.AgeGroup, roomCreate.Mode, roomCreate.MaxPlayer);
                return Ok(room);
            }
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

                var room = await _roomService.JoinRoom(roomId, playerName, request.RoomPassword);
                return Ok(new { room.RoomId, room.PlayerName });

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
        public async Task<IActionResult> GetListRoom(string? filters)
        {
            var rooms = await _roomService.GetListRoom(filters);
            return Ok(rooms);
        }
        [HttpGet("{filters}")]
        public async Task<IActionResult> GetRoom(string filters)
        {
            var room = await _roomService.GetRoom(filters);
            return Ok(room);
        }
        [HttpPatch("{roomId}/leave-room")]
        public async Task<ActionResult> LeaveRoom(string roomId, [FromBody] LeaveRoomDTO leave)
        {
            var room = await _roomService.LeaveRoom(roomId, leave.PlayerId);
            return Ok(new { Message = room });
        }
        [HttpPatch("{roomId}/change-name")]
        public async Task<IActionResult> ChangeName(string roomId, [FromBody] ChangeNameInRoomDTO request)
        {
            await _roomService.ChangePlayerName(roomId, request.PlayerId, request.NewName);
            await _websocketHandler.BroadcastMessage(roomId, $"Player {request.PlayerId} has changed their name to {request.NewName}");
            return Ok(new { Message = "Name changed successfully." });
        }
        
    }
}
