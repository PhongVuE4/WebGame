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
using TruthOrDare_Contract.DTOs.Room;
using TruthOrDare_Contract.DTOs.Player;
using TruthOrDare_Common;
using TruthOrDare_Common.Exceptions.Room;
using TruthOrDare_Common.Exceptions;
using TruthOrDare_Common.Exceptions.Player;
using System.Text.Json;
using TruthOrDare_Infrastructure.Repository;
using TruthOrDare_Common.Exceptions.Question;
using MongoDB.Bson;
using Microsoft.AspNetCore.SignalR;
using TruthOrDare_Core.Hubs;

namespace TruthOrDare_Core.Services
{
    public class RoomService : IRoomService
    {
        private readonly IMongoCollection<Room> _rooms;
        private readonly IPasswordHashingService _passwordHashingService;
        private readonly IQuestionRepository _questionRepository;
        private readonly IMongoCollection<GameSession> _gameSessions;
        private readonly IHubContext<RoomHub> _hubContext;
        public RoomService(MongoDbContext dbContext, 
            IPasswordHashingService passwordHashingService, 
            IQuestionRepository questionRepository,
            IHubContext<RoomHub> hubContext)
        {
            _rooms = dbContext.Rooms;
            _passwordHashingService = passwordHashingService;
            _questionRepository = questionRepository;
            _gameSessions = dbContext.GameSessions;
            _hubContext = hubContext;
        }

        public async Task<RoomCreateDTO> CreateRoom(string roomName, string playerId, string playerName, string roomPassword, string ageGroup, string mode, int maxPlayer, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new RoomNameRequiredException();
            }
            var existingRoom = await _rooms
                .Find(r => r.RoomName == roomName && r.IsActive && !r.IsDeleted)
                .FirstOrDefaultAsync();
            if(string.IsNullOrWhiteSpace(playerId))
            {
                throw new PlayerIdCannotNull();
            }
            if (existingRoom != null)
            {
                throw new RoomAlreadyExistsException(roomName);
            }
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = NameGenerator.GenerateRandomName();
            }
            else if (playerName.Length > 50)
            {
                throw ValidationException.ForProperty("playerName", "Player name must not exceed 50 characters.", (int)ErrorCode.PlayerNameLength);
            }
            var agegroupLower = ageGroup?.ToLower();
            if (string.IsNullOrWhiteSpace(agegroupLower)
                || (agegroupLower != "kids" && agegroupLower != "teen" && agegroupLower != "adult" && agegroupLower != "all"))
            {
                throw new RoomAgeGroupException();
            }
            var modeLower = mode?.ToLower();
            if (string.IsNullOrWhiteSpace(modeLower) || (modeLower != "friends" && modeLower != "couples" && modeLower != "party"))
            {
                throw new RoomModeException();
            }
            var roomId = IdGenerator.GenerateRoomId(); 

            string hashedPassword = null;
            if (!string.IsNullOrWhiteSpace(roomPassword))
            {
                var salt = _passwordHashingService.GenerateSalt();
                hashedPassword = _passwordHashingService.HashPassword(roomPassword, salt);
            }

            var roomDTO = new RoomCreateDTO
            {
                RoomId = roomId,
                RoomName = roomName,
                RoomPassword = hashedPassword,
                PlayerCount = 1,
                MaxPlayer = maxPlayer > 0 ? maxPlayer : 2,
                CreatedBy = playerName,
                CreatedAt = DateTime.Now,
                IsActive = true,
                Status = "waiting",
                AgeGroup = agegroupLower,
                Mode = modeLower,
                
                Players = new List<PlayerCreateRoomDTO> {
                new PlayerCreateRoomDTO {
                    PlayerId = playerId,
                    PlayerName = playerName,
                    IsHost = true,
                    IsActive = true,
                    ConnectionId = connectionId,
                    }
                }
            };

            var room = Mapper.ToRoom(roomDTO);
            await _rooms.InsertOneAsync(room);
            // Thêm host vào nhóm SignalR
            if (!string.IsNullOrEmpty(connectionId))
            {
                await _hubContext.Groups.AddToGroupAsync(connectionId, roomId);
                Console.WriteLine($"Thêm host {playerName} vào nhóm SignalR {roomId}");

                // Gửi sự kiện PlayerListUpdated
                var players = roomDTO.Players.Select(p => new { p.PlayerId, p.PlayerName }).ToList();
                await _hubContext.Clients.Group(roomId).SendAsync("PlayerListUpdated", players);
            }
            return roomDTO;
        }

        public async Task<(string roomId,string playerId, string playerName)> JoinRoom(string roomId, string playerId, string playerName, string roomPassword, string connectionId)
        {
            Console.WriteLine($"JoinRoom called with roomId: {roomId}, playerId: {playerId}, playerName: {playerName}, connectionId: {connectionId}");
            var room = await _rooms
                .Find(r => r.RoomId == roomId && r.IsActive && r.IsDeleted ==  false)
                .FirstOrDefaultAsync();

            if (room == null)
            {
                throw new RoomNotExistException(roomId);
            }

            if (!string.IsNullOrEmpty(room.RoomPassword))
            {
                if (string.IsNullOrEmpty(roomPassword))
                {
                    throw new RoomPasswordRequired();
                }

                if (!_passwordHashingService.VerifyPassword(roomPassword, room.RoomPassword))
                {
                    throw new RoomPasswordWrong();
                }
            }
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new PlayerIdCannotNull();
            }
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = NameGenerator.GenerateRandomName();
            }

            var existingPlayer = room.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if(existingPlayer != null)
            {
                if (existingPlayer.PlayerName != playerName)
                {
                    throw new PlayerIdAlreadyInUseException(playerId);
                }
                if(room.PlayerCount >= room.MaxPlayer)
                {
                    throw new FullPlayerException(room.MaxPlayer);
                }
                if (!existingPlayer.IsActive)
                {
                    room.PlayerCount++;
                }
                existingPlayer.IsActive = true;
                existingPlayer.ConnectionId = connectionId;
            }
            else
            {
                if (room.Players.Any(p => p.PlayerName == playerName && p.PlayerId != playerId))
                {
                    throw new PlayerNameExisted(playerName);
                }
                if (room.PlayerCount >= room.MaxPlayer)
                {
                    throw new FullPlayerException(room.MaxPlayer);
                }
                var newPlayer = new Player
                {
                    PlayerId = playerId,
                    PlayerName = playerName,
                    IsHost = false,
                    IsActive = true,
                    ConnectionId = connectionId,
                };

                room.Players.Add(newPlayer);
                room.PlayerCount++;
            }
            
            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, room);

            return (roomId, playerId, playerName);
        }

        public async Task<string> LeaveRoom(string roomId, string playerId)
        {
            var room = await _rooms
                .Find(r => r.RoomId == roomId && r.IsActive)
                .FirstOrDefaultAsync();

            if (room == null)
            {
                throw new RoomNotExistException(roomId);
            }
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new PlayerIdCannotNull();
            }
            var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId && p.IsActive);
            if (player == null)
            {
                throw new PlayerIdNotFound(playerId);
            }
            player.IsActive = false; // Đánh dấu người chơi roi phong
            player.ConnectionId = null; // Xóa ConnectionId
            room.PlayerCount--;

            // Nếu người rời là CurrentPlayerIdTurn, chuyển lượt
            bool turnChanged = false;
            string nextPlayerId = null;
            if (room.Status == "playing" && room.CurrentPlayerIdTurn == playerId)
            {
                nextPlayerId = GetNextPlayer(room, playerId);
                room.CurrentPlayerIdTurn = nextPlayerId;
                turnChanged = true;
                if (room.CurrentPlayerIdTurn == null)
                {
                    room.Status = "ended";
                    room.IsActive = false;
                    await SaveGameSession(room);
                }
            }
            // Xử lý host
            if (player.IsHost && room.Players.Any(p => p.IsActive))
            {
                player.IsHost = false;
                var nextActivePlayer = room.Players.First(p => p.IsActive);
                nextActivePlayer.IsHost = true;
            }

            // Lưu trạng thái phòng
            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, room);

            // Nếu không còn người chơi, đánh dấu phòng kết thúc
            if (!room.Players.Any(p => p.IsActive))
            {
                room.Status = "ended";
                await SaveGameSession(room);
                await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, room);
            }

            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, room);
            return turnChanged ? nextPlayerId : "Leave room success";
            //return "Leave room success";
        }

        public async Task<List<RoomListDTO>> GetListRoom(string? filters)
        {
            var filter = Builders<Room>.Filter.And(
                Builders<Room>.Filter.Eq(a => a.IsDeleted, false),
                Builders<Room>.Filter.Eq(a => a.IsActive, true));


            if (!string.IsNullOrWhiteSpace(filters))
            {
                var filtersDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(filters);

                if (filtersDictionary != null)
                {
                    // Apply each filter from the dictionary
                    if (filtersDictionary.TryGetValue("roomId", out var roomId) && !string.IsNullOrWhiteSpace(roomId))
                    {
                        filter = Builders<Room>.Filter.And(
                            filter,
                            Builders<Room>.Filter.Eq(r => r.RoomId, roomId)
                        );
                    }
                }
            }
            var rooms = await _rooms
                .Find(filter)
                .ToListAsync(); 

            return rooms.Select(room => new RoomListDTO
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                HostName = room.Players.FirstOrDefault(a => a.IsHost).PlayerName ?? "No host",
                PlayerCount = room.PlayerCount,
                MaxPlayer = room.MaxPlayer,
                HasPassword = !string.IsNullOrEmpty(room.RoomPassword),
                Status = room.Status,
                IsActive = room.IsActive,
                IsDeleted = room.IsDeleted,
            }).ToList();
        }
        public async Task<Room> GetRoom(string roomId)
        {
            var room = await _rooms
                .Find(r => r.RoomId == roomId 
                                    && r.IsActive 
                                    && !r.IsDeleted)
                .FirstOrDefaultAsync();

            if (room == null)
            {
                throw new RoomNotExistException(roomId);
            }

            return room;
        }

        public async Task ChangePlayerName(string roomId, string playerId, string newName)
        {
            if(string.IsNullOrWhiteSpace(newName))
            {
                throw new PlayerNameRequiredException();
            }
            var room = await _rooms
                .Find(r => r.RoomId == roomId && r.IsActive && !r.IsDeleted)
                .FirstOrDefaultAsync();

            if (room == null)
            {
                throw new RoomNotExistException(roomId);
            }

            var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null)
            {
                throw new PlayerIdNotFound(playerId);
            }

            if (room.Players.Any(p => p.PlayerName == newName && p.PlayerId != playerId))
            {
                throw new PlayerNameExisted(newName);
            }

            player.PlayerName = newName;
            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, room);
        }
        public async Task<Room> GetRoomEntity(string roomId)
        {
            var room = await _rooms
                .Find(r => r.RoomId == roomId && r.IsActive && !r.IsDeleted)
                .FirstOrDefaultAsync();

            if (room == null)
            {
                throw new RoomNotExistException(roomId);
            }

            return room;
        }
        public async Task StartGame(string roomId, string playerId)
        {
            var room = await GetRoom(roomId);
            if (room.Status.ToLower() != "waiting")
            {
                throw new RoomStartStatusException();
            }
            var player = room.Players.FirstOrDefault(a => a.PlayerId == playerId && a.IsActive);
            if (player == null)
            {
                throw new RoomNotFoundPlayerIdException();
            }
            if (!player.IsHost)
            {
                throw new RoomRequiredHost();
            }

            // Kiểm tra có người chơi active
            var hostPlayer = room.Players.FirstOrDefault(p => p.IsHost && p.IsActive);
            if (hostPlayer == null)
            {
                throw new NoActivePlayersException(); // Hoặc xử lý trường hợp không có host active
            }
            // Cập nhật trạng thái phòng
            room.Status = "playing";
            room.LastTurnTimestamp = DateTime.Now;
            room.LastQuestionTimestamp = null;
            room.IsLastQuestionAssigned = false;
            room.CurrentPlayerIdTurn = hostPlayer.PlayerId; // Chọn người active đầu tiên
            room.UpdatedAt = DateTime.Now;
            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, room);
        }
        public async Task<(Question question, bool isLastQuestion, int totalQuestions, int usedQuestions)> GetRandomQuestionForRoom(string roomId, string playerId, string questionType)
        {
            var room = await GetRoom(roomId);
            if (room.Status.ToLower() != "playing")
            {
                throw new GameMustbePlaying();
            }

            var currentPlayer = room.Players.FirstOrDefault(p => p.PlayerId == room.CurrentPlayerIdTurn && p.IsActive);
            if (currentPlayer == null)
            {
                room.CurrentPlayerIdTurn = GetNextPlayer(room, room.CurrentPlayerIdTurn);
                if (room.CurrentPlayerIdTurn == null)
                {
                    room.Status = "ended";
                    room.UpdatedAt = DateTime.UtcNow;
                    await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, room);
                    await SaveGameSession(room);
                    return (null, false, 0, 0); // Không còn người chơi active
                }
                await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, room);
            }

            var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId && p.IsActive);
            if (player == null)
            {
                throw new RoomNotFoundPlayerIdException();
            }
            if (room.CurrentPlayerIdTurn != playerId)
            {
                throw new RoomNotYourTurn();
            }
            var roomEntity = await _rooms.Find(a => a.RoomId == roomId).FirstOrDefaultAsync();
            if (roomEntity == null)
            {
                throw new RoomNotExistException(roomId);
            }
            if (string.IsNullOrWhiteSpace(questionType.ToLower()) || (questionType.ToLower() != "dare" && questionType.ToLower() != "truth"))
            {

                throw new QuestionTypeWrong();
            }
            // Kiểm tra xem người chơi đã lấy câu hỏi trong lượt này chưa
            if (roomEntity.LastQuestionTimestamp.HasValue && roomEntity.CurrentPlayerIdTurn == playerId)
            {
                throw new RoomNotYourTurn();
            }
            // Đếm tổng số câu hỏi khả dụng
            int totalQuestions = await _questionRepository.GetTotalQuestionsAsync(questionType,room.Mode, room.AgeGroup);
            int usedQuestions = roomEntity.UsedQuestionIds.Count;
            int remainingQuestionsBefore = totalQuestions - usedQuestions;

            var question = await _questionRepository.GetRandomQuestionAsync(
                questionType,
                room.Mode,
                room.AgeGroup,
                roomEntity.UsedQuestionIds
            );

            if (question == null)
            {
                roomEntity.Status = "ended"; // Hết câu hỏi, kết thúc game
                roomEntity.UpdatedAt = DateTime.Now; 
                await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, roomEntity);
                await SaveGameSession(roomEntity);
                return (null, false, totalQuestions, usedQuestions); // Hết câu hỏi

            }

            roomEntity.History.Add(new SessionHistory
            {
                PlayerId = playerId,
                PlayerName = player.PlayerName,
                Questions = new List<QuestionDetail>
                {
                    new QuestionDetail
                    {
                        QuestionId = question.Id,
                        QuestionContent = question.Text,
                    }
                },
                Timestamp = DateTime.Now,
                Status = "assigned"
            });

            roomEntity.UsedQuestionIds.Add(question.Id);
            roomEntity.Players.FirstOrDefault(p => p.PlayerId == playerId).QuestionsAnswered++;
            roomEntity.LastQuestionTimestamp = DateTime.Now; // Lưu thời gian lấy câu hỏi
            roomEntity.LastTurnTimestamp = DateTime.Now; // Đặt thời gian lượt đầu tiên

            var result = await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, roomEntity);
           
            int usedQuestionsAfter = roomEntity.UsedQuestionIds.Count;
            int remainingQuestionsAfter = totalQuestions - roomEntity.UsedQuestionIds.Count;

            bool isLastQuestion = remainingQuestionsAfter == 0; // Đây là câu hỏi cuối

            roomEntity.IsLastQuestionAssigned = isLastQuestion; // xac dinh cau hoi cuoi
            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, roomEntity); // Cập nhật lại

            return (question, isLastQuestion, totalQuestions, usedQuestionsAfter);
        }
        public async Task<EndGameSummaryDTO> EndGame(string roomId, string playerId)
        {
            var roomDto = await GetRoom(roomId);
            if (roomDto.Status.ToLower() != "playing")
            {
                throw new RoomEndStatusException();
            }
            var roomEntity = await _rooms
                .Find(r => r.RoomId == roomId)
                .FirstOrDefaultAsync();

            if (roomEntity == null)
            {
                throw new RoomNotExistException(roomId);
            }
            var ishot = roomEntity.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (!ishot.IsHost)
            {
                throw new RoomRequiredHost();
            }
            roomEntity.Status = "ended";
            roomEntity.UpdatedAt = DateTime.Now;

            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, roomEntity);
            await SaveGameSession(roomEntity);

            return new EndGameSummaryDTO
            {
                RoomId = roomId,
                TotalQuestions = roomEntity.UsedQuestionIds.Count,
                PlayerStats = roomDto.Players.Select(p => new PlayerStatDTO
                {
                    PlayerId = p.PlayerId,
                    PlayerName = p.PlayerName,
                    QuestionsAnswered = p.QuestionsAnswered
                }).ToList()
            };
        }
        public async Task ResetGame(string roomId, string playerId)
        {
            var roomDto = await GetRoom(roomId);
            if (roomDto.Status.ToLower() != "ended")
            {
                throw new RoomResetStatusException();
            }

            var roomEntity = await _rooms
                .Find(r => r.RoomId == roomId)
                .FirstOrDefaultAsync();

            if (roomEntity == null)
            {
                throw new RoomNotExistException(roomId);
            }
            var playercheck = roomDto.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (playercheck == null)
            {
                throw new RoomNotFoundPlayerIdException();
            }
            if (!playercheck.IsHost)
            {
                throw new RoomRequiredHost();
            }
            roomEntity.Status = "waiting";
            roomEntity.UsedQuestionIds.Clear();
            foreach (var player in roomEntity.Players)
            {
                player.QuestionsAnswered = 0;
            }
            roomEntity.UpdatedAt = DateTime.Now;

            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, roomEntity);
        }
        public async Task<(string nextPlayerId, bool isGameEnded, string message)> NextPlayer(string roomId, string playerId)
        {
            var roomEntity = await _rooms.Find(a => a.RoomId == roomId).FirstOrDefaultAsync();
            if (roomEntity == null)
            {
                throw new RoomNotExistException(roomId);
            }
            if (roomEntity.Status.ToLower() != "playing")
            {
                throw new GameMustbePlaying();
            }
            var currentPlayer = roomEntity.Players.FirstOrDefault(p => p.PlayerId == playerId && p.IsActive);
            if(currentPlayer == null)
            {
                // Nếu currentPlayerIdTurn không active, tự động chuyển sang người tiếp theo
                roomEntity.CurrentPlayerIdTurn = GetNextPlayer(roomEntity, roomEntity.CurrentPlayerIdTurn);
                if (roomEntity.CurrentPlayerIdTurn == null)
                {
                    roomEntity.Status = "ended";
                    roomEntity.UpdatedAt = DateTime.UtcNow;
                    await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, roomEntity);
                    await SaveGameSession(roomEntity);
                    return (null, true, "Game has ended due to no active players.");
                }
            }
            var player = roomEntity.Players.FirstOrDefault(p => p.PlayerId == playerId && p.IsActive);
            if (player == null)
            {
                throw new RoomNotFoundPlayerIdException();
            }
            if (player.PlayerId != roomEntity.CurrentPlayerIdTurn)
            {
                throw new RoomNotYourTurn();
            }
            // Kiểm tra xem có ít nhất một trong hai timestamp
            if (!roomEntity.LastQuestionTimestamp.HasValue && !roomEntity.LastTurnTimestamp.HasValue)
            {
                throw new RoomNextPlayerException();
            }

            // Kiểm tra nếu đây là câu hỏi cuối
            if (roomEntity.IsLastQuestionAssigned == true)
            {
                roomEntity.Status = "ended";
                roomEntity.UpdatedAt = DateTime.UtcNow;
                await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, roomEntity);
                await SaveGameSession(roomEntity); // Lưu session nếu có
                return (null, true, "Game has ended.");
            }
            // Nếu LastQuestionTimestamp chưa có, cho phép chuyển lượt với người chơi đầu tiên
            double timeElapsed;
            if (roomEntity.LastQuestionTimestamp.HasValue)
            {
                timeElapsed = (DateTime.UtcNow - roomEntity.LastQuestionTimestamp.Value).TotalSeconds;
            }
            else if (roomEntity.LastTurnTimestamp.HasValue)
            {
                timeElapsed = (DateTime.UtcNow - roomEntity.LastTurnTimestamp.Value).TotalSeconds;
            }
            else
            {
                throw new RoomNoTimestampException();
            }

            // Yêu cầu 5 giây cho thao tác thủ công nếu đã lấy câu hỏi
            if (roomEntity.LastQuestionTimestamp.HasValue && timeElapsed < 1)
            {
                throw new RoomNeedMoreTimeException();
            }

            // Chuyển sang người chơi tiếp theo
            var currentPlayerId = roomEntity.CurrentPlayerIdTurn;
            roomEntity.CurrentPlayerIdTurn = GetNextPlayer(roomEntity, currentPlayerId);
            roomEntity.LastQuestionTimestamp = null; // Reset thời gian để bắt đầu lượt mới
            roomEntity.LastTurnTimestamp = DateTime.Now; // Đặt lại thời gian lượt mới

            var result = await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, roomEntity);

            
            return (roomEntity.CurrentPlayerIdTurn, false, null); // Trả về ID của người chơi tiếp theo
        }
        private string GetNextPlayer(Room room, string currentPlayerId)
        {
            var players = room.Players.Where(p => p.IsActive).ToList();
            if (players == null || players.Count == 0)
            {
                return null;
            }

            // Tìm chỉ số của currentPlayerId trong danh sách gốc (không chỉ active)
            var allPlayers = room.Players.ToList();
            int currentIndex = allPlayers.FindIndex(p => p.PlayerId == currentPlayerId);
            if (currentIndex == -1)
            {
                // Nếu không tìm thấy, chọn người active đầu tiên
                return players.FirstOrDefault()?.PlayerId;
            }

            // Tìm người chơi active tiếp theo trong danh sách gốc
            for (int i = 1; i <= allPlayers.Count; i++)
            {
                int nextIndex = (currentIndex + i) % allPlayers.Count;
                var nextPlayer = allPlayers[nextIndex];
                if (nextPlayer.IsActive)
                {
                    return nextPlayer.PlayerId;
                }
            }

            return null; // Không tìm thấy người active
        }
        private async Task SaveGameSession(Room roomEntity)
        {
            // Lấy danh sách playerId có trong History (tức là đã được gán câu hỏi)
            var activePlayerIds = roomEntity.History
                .Select(h => h.PlayerId)
                .Distinct()
                .ToList();

            // Lọc History chỉ giữ lại những người chơi có hoạt động
            var filteredHistory = roomEntity.History
                .Where(h => activePlayerIds.Contains(h.PlayerId))
                .ToList();
            var gameSession = new GameSession
            {
                Id = ObjectId.GenerateNewId().ToString(), // Dùng ObjectId cho MongoDB
                RoomId = roomEntity.RoomId,
                RoomName = roomEntity.RoomName,
                Mode = roomEntity.Mode,
                AgeGroup = roomEntity.AgeGroup,
                StartTime = roomEntity.CreatedAt,
                EndTime = DateTime.Now,
                History = roomEntity.History, // Chuyển toàn bộ lịch sử từ RoomEntity
                TotalQuestions = roomEntity.UsedQuestionIds.Count,
                IsDeleted = false
            };

            await _gameSessions.InsertOneAsync(gameSession);
        }
        public async Task<List<Room>> GetActiveRooms()
        {
            return await _rooms.Find(r => r.Status == "playing").ToListAsync();
        }
        public async Task NotifyPlayers(string roomId, string message)
        {
            await _hubContext.Clients.Group(roomId).SendAsync("ReceiveMessage", message);
        }
    }
}