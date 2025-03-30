using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TruthOrDare_Infrastructure;

namespace TruthOrDare_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GgameSessionsController : ControllerBase
    {
        private readonly MongoDbContext _dbContext;

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
