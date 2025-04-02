﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TruthOrDare_Contract.IRepository;
using TruthOrDare_Infrastructure;

namespace TruthOrDare_API.Controllers
{
    [Route("api/gamesessions")]
    [ApiController]
    public class GameSessionsController : ControllerBase
    {
        private readonly IGameSessionsRepository _gameSessionsRepository;
        public GameSessionsController(IGameSessionsRepository gameSessionsRepository)
        {
            _gameSessionsRepository = gameSessionsRepository;
        }

        [HttpGet("game-sessions")]
        public async Task<IActionResult> GetGameSessions([FromQuery] string? filters)
        {
            var gameSessions = await _gameSessionsRepository.GetGameSessions(filters);
            return Ok(new { message = "Get gamesession successfully.", data = gameSessions });
        }
    }
}
