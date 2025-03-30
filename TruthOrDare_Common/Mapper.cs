using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.DTOs.Player;
using TruthOrDare_Contract.DTOs.Room;
using TruthOrDare_Contract.Models;

namespace TruthOrDare_Common
{
    public static class Mapper
    {
        // RoomCreateDTO -> Room (entity)
        public static Room ToRoom(RoomCreateDTO dto)
        {
            return new Room
            {
                RoomId = dto.RoomId,
                RoomName = dto.RoomName,
                RoomPassword = dto.RoomPassword,
                CreatedBy = dto.CreatedBy,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                IsActive = dto.IsActive,
                Status = dto.Status ?? "Waiting",
                AgeGroup = dto.AgeGroup,
                Mode = dto.Mode,
                Players = dto.Players.Select(p => ToPlayer(p)).ToList(),
                UsedQuestionIds = new List<string>()
            };
        }
        // Room (entity) -> RoomCreateDTO
        public static RoomCreateDTO ToRoomCreate(Room room)
        {
            return new RoomCreateDTO
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                RoomPassword = room.RoomPassword,
                CreatedBy = room.CreatedBy,
                CreatedAt = room.CreatedAt,
                IsActive = room.IsActive,
                Status = room.Status,
                AgeGroup = room.AgeGroup,
                Mode = room.Mode,
                Players = room.Players.Select(p => ToPlayerCreateRoomDTO(p)).ToList()
            };
        }
        // RoomDetailDTO -> RoomCreateDTO
        public static RoomCreateDTO ToRoomCreateDTO(RoomDetailDTO room)
        {
            return new RoomCreateDTO
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                CreatedBy = room.CreatedBy,
                IsActive = room.IsActive,
                Status = room.Status,
                AgeGroup = room.AgeGroup,
                Mode = room.Mode,
                Players = room.Players.Select(p => ToPlayerCreateRoomDTO(p)).ToList()
            };
        }
        public static RoomDetailDTO ToRoomDetailDTO(Room room)
        {
            return new RoomDetailDTO
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                MaxPlayer = room.MaxPlayer,
                HasPassword = room.HasPassword,
                Status = room.Status,
                IsActive = room.IsActive,
                Players = room.Players.Select(p => ToPlayerDTO(p)).ToList()
            };
        }
        // PlayerCreateRoomDTO -> Player (entity)
        public static Player ToPlayer(PlayerCreateRoomDTO dto)
        {
            return new Player
            {
                PlayerId = dto.PlayerId,
                PlayerName = dto.PlayerName,
                IsHost = dto.IsHost,
                TotalPoints = 0, // Giá trị mặc định
                CreatedAt = DateTime.Now, // Giá trị mặc định
                QuestionsAnswered = 0, // Giá trị mặc định
            };
        }
        // Player -> PlayerDTO (entity)

        public static PlayerDTO ToPlayerDTO(Player player)
        {
            return new PlayerDTO
            {
                PlayerId = player.PlayerId,
                PlayerName = player.PlayerName,
                IsHost = player.IsHost,
            };
        }
        // Player (entity) -> PlayerCreateRoomDTO
        public static PlayerCreateRoomDTO ToPlayerCreateRoomDTO(Player player)
        {
            return new PlayerCreateRoomDTO
            {
                PlayerId = player.PlayerId,
                PlayerName = player.PlayerName,
                IsHost = player.IsHost
            };
        }
        // PlayerDTO (từ RoomDetailDTO) -> PlayerCreateRoomDTO
        public static PlayerCreateRoomDTO ToPlayerCreateRoomDTO(PlayerDTO player)
        {
            return new PlayerCreateRoomDTO
            {
                PlayerId = player.PlayerId,
                PlayerName = player.PlayerName,
                IsHost = player.IsHost
            };
        }
    }
}
