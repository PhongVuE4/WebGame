using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.DTOs.Player;
using TruthOrDare_Contract.DTOs.Room;
using TruthOrDare_Contract.IRepository;
using TruthOrDare_Contract.Models;

namespace TruthOrDare_Common
{
    public static class Mapper
    {
        private static readonly IQuestionRepository questionRepository;
        public static Room ToRoom(RoomCreateDTO dto)
        {
            return new Room
            {
                RoomId = dto.RoomId,
                RoomName = dto.RoomName,
                RoomPassword = dto.RoomPassword,
                PlayerCount = dto.PlayerCount,
                MaxPlayer = dto.MaxPlayer,
                Status = dto.Status,
                Mode = dto.Mode,
                AgeGroup = dto.AgeGroup,
                CreatedBy = dto.CreatedBy,
                CreatedAt = dto.CreatedAt,
                IsActive = dto.IsActive,
                Players = dto.Players.Select(p => ToPlayer(p)).ToList()
            };
        }

        public static RoomCreateDTO ToRoomCreateDTO(Room room)
        {
            return new RoomCreateDTO
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                MaxPlayer = room.MaxPlayer,
                CreatedBy = room.CreatedBy,
                CreatedAt = room.CreatedAt,
                IsActive = room.IsActive,
                Players = room.Players.Select(p => ToPlayerCreateRoomDTO(p)).ToList()
            };
        }
        public static async Task<RoomDetailDTO> ToRoomDetailDTO(Room room, IQuestionRepository questionRepository)
        {
            string currentQuestionText = null;
            if (room.CurrentQuestionId != null)
            {
                 var question = await questionRepository.GetQuestionById(room.CurrentQuestionId);
                currentQuestionText = question?.Text;
            }

            return new RoomDetailDTO
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                PlayerCount = room.PlayerCount,
                MaxPlayer = room.MaxPlayer,
                CurrentPlayerIdTurn = room.CurrentPlayerIdTurn,
                CurrentQuestionId = room.CurrentQuestionId,
                CurrentQuestionText = currentQuestionText,
                HasPassword = room.HasPassword,
                Status = room.Status,
                Mode = room.Mode,
                AgeGroup = room.AgeGroup,
                IsActive = room.IsActive,
                Players = room.Players.Select(p => ToPlayerCreateRoomDTO(p)).ToList()
            };
        }

        public static Player ToPlayer(PlayerCreateRoomDTO dto)
        {
            return new Player
            {
                PlayerId = dto.PlayerId,
                PlayerName = dto.PlayerName,
                IsHost = dto.IsHost,
                IsActive = dto.IsActive,
                ConnectionId = dto.ConnectionId,
            };
        }

        public static PlayerCreateRoomDTO ToPlayerCreateRoomDTO(Player player)
        {
            return new PlayerCreateRoomDTO
            {
                PlayerId = player.PlayerId,
                PlayerName = player.PlayerName,
                IsHost = player.IsHost,
                IsActive = player.IsActive,
                ConnectionId = player.ConnectionId
                
            };
        }
    }
}
