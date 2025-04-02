using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TruthOrDare_Common.Exceptions.Player;
using TruthOrDare_Common.Exceptions.Question;
using TruthOrDare_Contract.IRepository;
using TruthOrDare_Contract.Models;

namespace TruthOrDare_Infrastructure.Repository
{
    public class GameSessionsRepository : IGameSessionsRepository
    {
        private readonly IMongoCollection<GameSession> _gamesessions;
        public GameSessionsRepository(MongoDbContext context)
        {
           _gamesessions = context.GameSessions;
        }

        public async Task<List<GameSession>> GetGameSessions(string? filters)
        {
            if (string.IsNullOrWhiteSpace(filters))
            {
                throw new ArgumentException("PlayerId is required to filter game sessions.");
            }

            Dictionary<string, string> filterDict;
            try
            {
                filterDict = JsonSerializer.Deserialize<Dictionary<string, string>>(filters);
            }
            catch (JsonException)
            {
                throw new InvalidFiltersException();
            }

            if (!filterDict.TryGetValue("playerId", out var playerId) || string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentException("PlayerId is required to filter game sessions.");
            }

            var baseFilter = Builders<GameSession>.Filter.Eq(q => q.IsDeleted, false);
            var playerFilter = Builders<GameSession>.Filter.ElemMatch(q => q.History,
                Builders<SessionHistory>.Filter.Eq(h => h.PlayerId, playerId));

            var combinedFilter = Builders<GameSession>.Filter.And(baseFilter, playerFilter);

            // Projection để chỉ lấy các field cần thiết
            var projection = Builders<GameSession>.Projection
                .Include(q => q.Id)
                .Include(q => q.RoomId) // Lấy RoomId từ db
                .Include(q => q.StartTime)
                .Include(q => q.EndTime)
                .Include(q => q.TotalQuestions)
                .Include(q => q.IsDeleted);

            return await _gamesessions.Find(combinedFilter)
                .Project<GameSession>(projection)
                .ToListAsync();
        }
    }
}
