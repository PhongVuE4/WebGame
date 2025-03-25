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

        public async Task<RoomCreateDTO> CreateRoom(string roomName, string playerName, string roomPassword)
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
                throw new Exception($"Room with ID '{roomId}' does not exist or is not active.");
            }

            if (!string.IsNullOrEmpty(room.RoomPassword))
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

        public async Task<List<RoomListDTO>> GetListRoom()
        {
            var rooms = await _rooms
                .Find(r => r.IsActive) // Chỉ lấy các phòng đang hoạt động
                .ToListAsync();

            return rooms.Select(room => new RoomListDTO
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                PlayerCount = room.Players.Count,
                HasPassword = !string.IsNullOrEmpty(room.RoomPassword)
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
    }
}