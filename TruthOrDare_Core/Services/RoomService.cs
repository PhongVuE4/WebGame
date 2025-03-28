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
using static MongoDB.Driver.WriteConcern;
using TruthOrDare_Infrastructure.Repository;

namespace TruthOrDare_Core.Services
{
    public class RoomService : IRoomService
    {
        private readonly IMongoCollection<Room> _rooms;
        private readonly IPasswordHashingService _passwordHashingService;
        private readonly IQuestionRepository _questionRepository;
        public RoomService(MongoDbContext dbContext, IPasswordHashingService passwordHashingService, IQuestionRepository questionRepository)
        {
            _rooms = dbContext.Rooms;
            _passwordHashingService = passwordHashingService;
            _questionRepository = questionRepository;
        }
        public async Task<RoomCreateDTO> CreateRoom(string roomName, string playerName, string roomPassword, string ageGroup, string mode)
        {
            var existingRoom = await _rooms
                .Find(r => r.RoomName == roomName && r.IsActive)
                .FirstOrDefaultAsync();

            if (existingRoom != null)
            {
                throw new Exception($"Room with name '{roomName}' already exists.");
            }
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = NameGenerator.GenerateRandomName();
            }
            var agegroupLower = ageGroup?.ToLower();
            if (string.IsNullOrWhiteSpace(agegroupLower) 
                || (agegroupLower != "kids" && agegroupLower != "teen" && agegroupLower != "adult" && agegroupLower != "all"))
            {
                throw new Exception("Age group must be 'Kids', 'Teen', 'Adult' and 'All'");
            }
            var modeLower = mode?.ToLower();
            if (string.IsNullOrWhiteSpace(modeLower) || (modeLower != "friends" && modeLower != "couples" && modeLower != "party"))
            {
                throw new Exception("Mode must be 'Friends', 'Couples', or 'Party'.");
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
                CreatedBy = playerName,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsActive = true,
                Status = "Waiting",
                AgeGroup = ageGroup,
                Mode = mode,
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
            return Mapper.ToRoomCreate(room);
        }

        public async Task<RoomCreateDTO> JoinRoom(string roomId, string? playerName, string? roomPassword)
        {
            var room = await _rooms
                .Find(r => r.RoomId == roomId && r.IsActive && r.IsDeleted ==  false)
                .FirstOrDefaultAsync();

            if (room == null)
            {
                throw new Exception($"Room with ID '{roomId}' does not exist or is not active.");
            }

            bool requiresPassword = !string.IsNullOrEmpty(room.RoomPassword);
            if (requiresPassword)
            {
                if (string.IsNullOrEmpty(roomPassword))
                {
                    throw new Exception("Password is required to join this room.");
                }

                if (!_passwordHashingService.VerifyPassword(roomPassword, room.RoomPassword))
                {
                    throw new Exception("Incorrect password.");
                }
            }

            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = NameGenerator.GenerateRandomName();
            }

            if (room.Players.Any(p => p.PlayerName == playerName))
            {
                throw new Exception($"Player name '{playerName}' is already taken in the room. Please choose a different name.");
            }
            Console.WriteLine($"roomPassword: '{roomPassword}', room.RoomPassword: '{room.RoomPassword}'");
            Console.WriteLine($"VerifyPassword result: {_passwordHashingService.VerifyPassword(roomPassword, room.RoomPassword)}");
            var playerId = Guid.NewGuid().ToString();
            var newPlayer = new Player
            {
                PlayerId = playerId,
                PlayerName = playerName,
                IsHost = false
            };

            room.Players.Add(newPlayer);
            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, room);

            return Mapper.ToRoomCreate(room);
        }

        public async Task<Room> LeaveRoom(string roomId, string playerId)
        {
            var room = await _rooms
                .Find(r => r.RoomId == roomId && r.IsActive)
                .FirstOrDefaultAsync();

            if (room == null)
            {
                throw new Exception($"Room with ID '{roomId}' does not exist or is not active.");
            }

            var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null)
            {
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
            }
            return room;
        }

        public async Task<List<RoomListDTO>> GetListRoom(string? roomId)
        {
            var filter = Builders<Room>.Filter.And(
                Builders<Room>.Filter.Eq(a => a.IsDeleted, false),
                Builders<Room>.Filter.Eq(a => a.IsActive, true));
                
               
            if (!string.IsNullOrWhiteSpace(roomId))
            {
                filter = Builders<Room>.Filter.And(
                    filter,
                    Builders<Room>.Filter.Eq(r => r.RoomId, roomId)
                );
            }
            var rooms = await _rooms
                .Find(filter)
                .ToListAsync();

            return rooms.Select(room => new RoomListDTO
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                PlayerCount = room.Players.Count,
                HasPassword = !string.IsNullOrEmpty(room.RoomPassword),
                IsActive = room.IsActive,
                IsDeleted = room.IsDeleted,
            }).ToList();
        }
        public async Task<RoomDetailDTO> GetRoom(string roomId)
        {
            var room = await _rooms
                .Find(r => r.RoomId == roomId && r.IsActive)
                .FirstOrDefaultAsync();

            if (room == null)
            {
                throw new Exception($"Room with ID '{roomId}' does not exist or is not active.");
            }

            return new RoomDetailDTO
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                Status = room.Status,
                AgeGroup = room.AgeGroup,
                Mode = room.Mode,
                CreatedBy = room.CreatedBy,
                CreatedAt = room.CreatedAt,
                UpdatedAt = room.UpdatedAt,
                IsActive = room.IsActive,
                Players = room.Players.Select(p => new PlayerDTO
                {
                    PlayerId = p.PlayerId,
                    PlayerName = p.PlayerName,
                    AgeGroup = p.AgeGroup,
                    TotalPoints = p.TotalPoints,
                    CreatedAt = p.CreatedAt,
                    IsHost = p.IsHost,
                    QuestionsAnswered = p.QuestionsAnswered
                }).ToList()
            };
        }
        public async Task<Room> GetRoomEntity(string roomId)
        {
            var room = await _rooms
                .Find(r => r.RoomId == roomId && r.IsActive)
                .FirstOrDefaultAsync();

            if (room == null)
            {
                throw new Exception($"Room with ID '{roomId}' does not exist or is not active.");
            }

            return room;
        }
        public async Task ChangePlayerName(string roomId, string playerId, string newName)
        {
            var room = await _rooms
                .Find(r => r.RoomId == roomId && r.IsActive)
                .FirstOrDefaultAsync();

            if (room == null)
            {
                throw new Exception($"Room with ID '{roomId}' does not exist or is not active.");
            }

            var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null)
            {
                throw new Exception($"Player with ID '{playerId}' not found in the room.");
            }

            if (room.Players.Any(p => p.PlayerName == newName && p.PlayerId != playerId))
            {
                throw new Exception($"Player name '{newName}' is already taken in the room. Please choose a different name.");
            }

            player.PlayerName = newName;
            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, room);
        }

        public async Task StartGame(string roomId, string playerId)
        {
            var room = await GetRoom(roomId);
            if (room.Status != "Waiting")
            {
                throw new Exception("Game can only be started from Waiting status.");
            }
            var player = room.Players.FirstOrDefault(a => a.PlayerId == playerId);
            if (player == null)
            {
                throw new Exception($"Player with ID '{playerId}' is not in the room.");
            }
            if (!player.IsHost)
            {
                throw new Exception("Only the host can start the game.");
            }
            var roomUpdate = await _rooms.Find(r => r.RoomId == roomId).FirstOrDefaultAsync();
            roomUpdate.Status = "Playing";
            roomUpdate.UpdatedAt = DateTime.UtcNow;
            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, roomUpdate);
        }
        public async Task<Question> GetRandomQuestionForRoom(string roomId, string playerId, string questionType)
        {
            var room = await GetRoom(roomId);
            if (room.Status != "Playing")
            {
                throw new Exception("Game must be in Playing status to get questions.");
            }

            var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null)
            {
                throw new Exception($"Player with ID '{playerId}' not found in room.");
            }
            var roomEntity = await _rooms.Find(a => a.RoomId == roomId).FirstOrDefaultAsync();
            if(roomEntity == null)
            {
                throw new Exception($"Room with ID '{roomId}' not found in database.");
            }
            if (string.IsNullOrWhiteSpace(questionType.ToLower()) || (questionType.ToLower() != "dare" && questionType.ToLower() != "truth"))
            {
                throw new Exception($"Question type must be Truth or Dare");
            }
            var question = await _questionRepository.GetRandomQuestionAsync(
                questionType,
                room.AgeGroup,
                roomEntity.UsedQuestionIds
            );

            if (question == null)
            {
                roomEntity.Status = "Ended"; // Hết câu hỏi, kết thúc game
                await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, roomEntity);
                return null;
            }

            roomEntity.UsedQuestionIds.Add(question.Id);
            var playerEntity = roomEntity.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (playerEntity != null)
            {
                playerEntity.QuestionsAnswered++;
            }
            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, roomEntity);
            return question;
        }
        public async Task<EndGameSummaryDTO> EndGame(string roomId)
        {
            var roomDto = await GetRoom(roomId);
            if (roomDto.Status != "Playing")
            {
                throw new Exception("Game must be in Playing status to end.");
            }
            var roomEntity = await _rooms
                .Find(r => r.RoomId == roomId)
                .FirstOrDefaultAsync();

            if (roomEntity == null)
            {
                throw new Exception($"Room with ID '{roomId}' not found in database.");
            }
            roomEntity.Status = "Ended";
            roomEntity.UpdatedAt = DateTime.UtcNow;

            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, roomEntity);

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
            if (roomDto.Status != "Ended")
            {
                throw new Exception("Game can only be reset from Ended status.");
            }

            var roomEntity = await _rooms
                .Find(r => r.RoomId == roomId)
                .FirstOrDefaultAsync();

            if (roomEntity == null)
            {
                throw new Exception($"Room with ID '{roomId}' not found in database.");
            }
            var playercheck = roomDto.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (playercheck == null)
            {
                throw new Exception($"Player with ID '{playerId}' not found in room.");
            }
            if(!playercheck.IsHost)
            {
                throw new Exception($"Only the host can start the game.");
            }
            roomEntity.Status = "Waiting";
            roomEntity.UsedQuestionIds.Clear();
            foreach (var player in roomEntity.Players)
            {
                player.QuestionsAnswered = 0;
            }
            roomEntity.UpdatedAt = DateTime.UtcNow;

            await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, roomEntity);
        }
    }
}