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

namespace TruthOrDare_Core.Services
{
    public class RoomService : IRoomService
    {
        private readonly IMongoCollection<Room> _rooms;
        private readonly IPasswordHashingService _passwordHashingService;
        public RoomService(MongoDbContext dbContext, IPasswordHashingService passwordHashingService)
        {
            _rooms = dbContext.Rooms;
            _passwordHashingService = passwordHashingService;
        }

        public async Task<RoomCreateDTO> CreateRoom(string roomName, string playerName, string roomPassword, int maxPlayer)
        {
            var existingRoom = await _rooms
                .Find(r => r.RoomName == roomName && r.IsActive)
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
                MaxPlayer = maxPlayer,
                CreatedBy = playerName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Status = "Waiting",
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
            return Mapper.ToRoomCreateDTO(room);
        }

        public async Task<RoomCreateDTO> JoinRoom(string roomId, string playerName, string roomPassword = null)
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

            return Mapper.ToRoomCreateDTO(room);
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
                MaxPlayer = room.MaxPlayer,
                HasPassword = !string.IsNullOrEmpty(room.RoomPassword),
                IsActive = room.IsActive,
                IsDeleted = room.IsDeleted,
            }).ToList();
        }
        public async Task<Room> GetRoom(string roomId)
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
    }
}