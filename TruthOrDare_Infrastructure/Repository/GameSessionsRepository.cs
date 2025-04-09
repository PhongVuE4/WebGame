using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TruthOrDare_Common.Exceptions.GameSession;
using TruthOrDare_Common.Exceptions.Player;
using TruthOrDare_Common.Exceptions.Question;
using TruthOrDare_Contract.DTOs.GameSession;
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
                throw new PlayerIdCannotNull();
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
                throw new PlayerIdNotFound(playerId);
            }

            var conditions = new List<FilterDefinition<GameSession>>();

            // Base filter: IsDeleted = false
            var baseFilter = Builders<GameSession>.Filter.Eq(q => q.IsDeleted, false);
            conditions.Add(baseFilter);

            // Filter theo playerId (bắt buộc)
            var playerFilter = Builders<GameSession>.Filter.ElemMatch(q => q.History,
                Builders<SessionHistory>.Filter.Eq(h => h.PlayerId, playerId));
            conditions.Add(playerFilter);

            // Kết hợp các filter
            var combinedFilter = Builders<GameSession>.Filter.And(conditions);

            // Projection để chỉ lấy các field cần thiết
            var projection = Builders<GameSession>.Projection
                .Include(q => q.Id)
                .Include(q => q.RoomId) // Lấy RoomId từ db
                .Include(q => q.RoomName)
                .Include(q => q.Mode)
                .Include(q => q.AgeGroup)
                .Include(q => q.StartTime)
                .Include(q => q.EndTime)
                .Include(q => q.TotalQuestions)
                .Include(q => q.IsDeleted);

            return await _gamesessions.Find(combinedFilter)
                .Project<GameSession>(projection)
                .ToListAsync();
        }
        public async Task<GameSessionDetailDTO> GetGameSessionDetail(string filters)
        {
            if (string.IsNullOrWhiteSpace(filters))
            {
                throw new PlayerIdCannotNull();
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
                throw new PlayerIdCannotNull();
            }

            if (!filterDict.TryGetValue("gamesessionId", out var gamesessionId) || string.IsNullOrWhiteSpace(gamesessionId))
            {
                throw new GameSessionRequired();
            }

            // Kết hợp các filter ngay từ đầu
            var combinedFilter = Builders<GameSession>.Filter.And(
                Builders<GameSession>.Filter.Eq(q => q.IsDeleted, false),
                Builders<GameSession>.Filter.Eq(q => q.Id, gamesessionId),
                Builders<GameSession>.Filter.ElemMatch(q => q.History,
                    Builders<SessionHistory>.Filter.Eq(h => h.PlayerId, playerId))
            );

            // Lấy trực tiếp bản ghi đầu tiên (không cần ToListAsync trước)
            var gameSession = await _gamesessions.Find(combinedFilter).FirstOrDefaultAsync();
            if (gameSession == null)
            {
                return null; // Hoặc throw exception tùy yêu cầu
            }
            // Nhóm history theo playerId
            var groupedHistory = gameSession.History
                .GroupBy(h => h.PlayerId)
                .Select(g => new SessionHistoryDTO
                {
                    PlayerId = g.Key,
                    PlayerName = g.First().PlayerName, // Lấy PlayerName từ bản ghi đầu tiên
                    Questions = g.Select(h => new QuestionDetailDTO
                    {
                        QuestionId = h.Questions.First().QuestionId, // Giả sử mỗi history chỉ có 1 question
                        QuestionContent = h.Questions.First().QuestionContent
                    }).ToList(),
                    Status = g.Last().Status, // Lấy status của bản ghi cuối cùng
                    Response = g.Last().Response, // Lấy response của bản ghi cuối cùng
                    ResponseUrl = g.Last().ResponseUrl, // Lấy responseUrl của bản ghi cuối cùng
                    PointsEarned = g.Sum(h => h.PointsEarned), // Tổng hợp points nếu cần
                    Timestamp = g.Last().Timestamp // Lấy timestamp của bản ghi cuối cùng
                }).ToList();

            return new GameSessionDetailDTO
            {
                Id = gameSession.Id,
                RoomId = gameSession.RoomId,
                RoomName = gameSession.RoomName,
                Mode = gameSession.Mode,
                AgeGroup = gameSession.AgeGroup,
                StartTime = gameSession.StartTime,
                EndTime = gameSession.EndTime,
                History = groupedHistory, // Sử dụng history đã nhóm
                TotalQuestions = gameSession.TotalQuestions,
                IsDeleted = gameSession.IsDeleted
            };
        }
    }
}
