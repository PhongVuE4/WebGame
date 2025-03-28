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
        public static Room ToRoom(RoomCreateDTO dto)
        {
            return new Room
            {
                RoomId = dto.RoomId,
                RoomName = dto.RoomName,
                RoomPassword = dto.RoomPassword,
                MaxPlayer = dto.MaxPlayer,
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
                RoomPassword = room.RoomPassword,
                MaxPlayer = room.MaxPlayer,
                CreatedBy = room.CreatedBy,
                CreatedAt = room.CreatedAt,
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
            };
        }

        public static PlayerCreateRoomDTO ToPlayerCreateRoomDTO(Player player)
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
