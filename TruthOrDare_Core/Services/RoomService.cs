using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.IRepository;
using TruthOrDare_Contract.IServices;
using TruthOrDare_Contract;
using TruthOrDare_Contract.Models;
using TruthOrDare_Infrastructure;

namespace TruthOrDare_Core.Services
{
    public class RoomService : IRoomService
    {
        private readonly IMongoCollection<Room> _rooms;
        private readonly IQuestionRepository _questionRepository;
        private readonly IMongoCollection<GameSession> _gameSessions;
        private readonly IWebSocketHandler _wsHandler;

        public RoomService(MongoDbContext context, IQuestionRepository questionRepository, IWebSocketHandler wsHandler)
        {
            _rooms = context.Rooms;
            _questionRepository = questionRepository;
            _gameSessions = context.GameSessions;
            _wsHandler = wsHandler;
        }

        public async Task CreateRoomAsync(string roomId, List<string> players)
        {
            var room = new Room
            {
                RoomId = roomId,
                Players = players,
                CurrentPlayerTurn = players[0],
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                TtlExpiry = DateTime.UtcNow.AddHours(1)
            };
            await _rooms.InsertOneAsync(room);

            var session = new GameSession { RoomId = roomId, StartTime = DateTime.UtcNow };
            await _gameSessions.InsertOneAsync(session);

            await NextTurnAsync(roomId);
        }

        public async Task NextTurnAsync(string roomId)
        {
            var room = await _rooms.Find(r => r.RoomId == roomId).FirstOrDefaultAsync();
            if (room == null || room.Status != "active" || room.Players.Count == 0)
            {
                await _wsHandler.BroadcastToRoom(roomId, "Room not found, inactive, or empty!");
                return;
            }

            var session = await _gameSessions.Find(s => s.RoomId == roomId).FirstOrDefaultAsync();
            var usedQuestionIds = session?.History.Select(h => h.QuestionId).ToList() ?? new List<string>();

            var question = await _questionRepository.GetRandomQuestionAsync(usedQuestionIds);
            if (question == null)
            {
                await _wsHandler.BroadcastToRoom(roomId, "No more unique questions available!");
                return;
            }

            int currentIndex = room.Players.IndexOf(room.CurrentPlayerTurn);
            int nextIndex = (currentIndex + 1) % room.Players.Count;
            var nextPlayer = room.Players[nextIndex];

            var roomFilter = Builders<Room>.Filter.Eq(r => r.RoomId, roomId);
            var roomUpdate = Builders<Room>.Update
                .Set(r => r.CurrentPlayerTurn, nextPlayer)
                .Set(r => r.CurrentQuestionId, question.Id);
            await _rooms.UpdateOneAsync(roomFilter, roomUpdate);

            await _wsHandler.BroadcastToRoom(roomId, $"Turn: {nextPlayer}, Question: {question.Text}");
        }

        public async Task CompleteTurnAsync(string roomId, string playerId, string response, string responseUrl = null)
        {
            var room = await _rooms.Find(r => r.RoomId == roomId).FirstOrDefaultAsync();
            if (room != null && room.CurrentPlayerTurn == playerId)
            {
                var sessionFilter = Builders<GameSession>.Filter.Eq(s => s.RoomId, roomId);
                var historyEntry = new SessionHistory
                {
                    QuestionId = room.CurrentQuestionId,
                    PlayerId = playerId,
                    Status = "completed",
                    Response = response,
                    ResponseUrl = responseUrl,
                    PointsEarned = await _questionRepository.GetPointsForQuestionAsync(room.CurrentQuestionId),
                    Timestamp = DateTime.UtcNow
                };
                var update = Builders<GameSession>.Update.Push(s => s.History, historyEntry);
                await _gameSessions.UpdateOneAsync(sessionFilter, update);

                await NextTurnAsync(roomId);
            }
        }
    }
}