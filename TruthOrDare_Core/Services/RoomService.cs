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

namespace TruthOrDare_Core.Services
{
    public class RoomService : IRoomService
    {
        private readonly IMongoCollection<Room> _rooms;
        private readonly IPasswordHashingService _passwordHashingService;
        private readonly IQuestionRepository _questionRepository;
        private readonly IMongoCollection<GameSession> _gameSessions;
        public RoomService(MongoDbContext dbContext, IPasswordHashingService passwordHashingService, IQuestionRepository questionRepository)
        {
            _rooms = dbContext.Rooms;
            _passwordHashingService = passwordHashingService;
            _questionRepository = questionRepository;
            _gameSessions = dbContext.GameSessions;
        }

        public async Task<RoomCreateDTO> CreateRoom(string roomName, string playerName, string roomPassword, string ageGroup, string mode, int maxPlayer)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new RoomNameRequiredException();
            }
            var existingRoom = await _rooms
                .Find(r => r.RoomName == roomName && r.IsActive && !r.IsDeleted)
                .FirstOrDefaultAsync();

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
            var playerId = Guid.NewGuid().ToString();

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
                    IsHost = true
                    }
                }
            };

            var room = Mapper.ToRoom(roomDTO);
            await _rooms.InsertOneAsync(room);
            return roomDTO;
        }

        public async Task<(string roomId,string playerId, string playerName)> JoinRoom(string roomId, string playerName, string roomPassword)
        {
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

            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = NameGenerator.GenerateRandomName();
            }

            if (room.Players.Any(p => p.PlayerName == playerName))
            {
                throw new PlayerNameExisted(playerName);
            }
            if(room.Players.Count >= room.MaxPlayer)
            {
                throw new FullPlayerException(room.MaxPlayer);
            }
            var playerId = Guid.NewGuid().ToString();
            var newPlayer = new Player
            {
                PlayerId = playerId,
                PlayerName = playerName,
                IsHost = false
            };

            room.Players.Add(newPlayer);
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
            var player = room.Players.SingleOrDefault(p => p.PlayerId == playerId);
            if (player == null)
            {
                throw new PlayerIdNotFound(playerId);
            }
            room.Players.Remove(player);

            if (!room.Players.Any())
            {
                room.IsActive = false; 
            }
            else if (player.IsHost && room.Players.Any())
            {
                room.Players.First().IsHost = true; 
            }

            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, room);

            return "Leave room success";
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
                PlayerCount = room.Players.Count,
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
        .Find(r => r.RoomId == roomId && r.IsActive && !r.IsDeleted)
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
            var player = room.Players.FirstOrDefault(a => a.PlayerId == playerId);
            if (player == null)
            {
                throw new RoomNotFoundPlayerIdException();
            }
            if (!player.IsHost)
            {
                throw new RoomRequiredHost();
            }
            var roomUpdate = await _rooms.Find(r => r.RoomId == roomId).FirstOrDefaultAsync();
            roomUpdate.Status = "playing";
            roomUpdate.LastTurnTimestamp = DateTime.Now;
            roomUpdate.LastQuestionTimestamp = null;
            roomUpdate.IsLastQuestionAssigned = false;
            roomUpdate.CurrentPlayerIdTurn = roomUpdate.Players[0].PlayerId;
            roomUpdate.UpdatedAt = DateTime.Now;
            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, roomUpdate);
        }
        public async Task<(Question question, bool isLastQuestion, int totalQuestions, int usedQuestions)> GetRandomQuestionForRoom(string roomId, string playerId, string questionType)
        {
            var room = await GetRoom(roomId);
            if (room.Status.ToLower() != "playing")
            {
                throw new GameMustbePlaying();
            }

            var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId);
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
            var player = roomEntity.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if(player == null)
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
            var players = room.Players;
            if (players == null || players.Count == 0)
            {
                return null;
            }

            // Tìm chỉ số của người chơi hiện tại
            int currentIndex = players.FindIndex(p => p.PlayerId == currentPlayerId);
            if (currentIndex == -1)
            {
                return players[0].PlayerId; // Nếu không tìm thấy, chọn người đầu tiên
            }

            // Chuyển sang người tiếp theo, nếu hết danh sách thì quay lại đầu
            int nextIndex = (currentIndex + 1) % players.Count;
            return players[nextIndex].PlayerId;
        }
        private async Task SaveGameSession(Room roomEntity)
        {
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
    }
}
