using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.DTOs.Room;
using TruthOrDare_Contract.Models;

namespace TruthOrDare_Contract.IServices
{
    public interface IRoomService
    {
        Task<RoomCreateDTO> CreateRoom(string roomName, string playerName, string roomPassword);
        Task<RoomCreateDTO> JoinRoom(string roomId, string playerName, string roomPassword = null);
        Task<Room> LeaveRoom(string roomId, string playerId);
        Task<List<RoomListDTO>> GetListRoom();
        Task<Room> GetRoom(string roomId);
        Task ChangePlayerName(string roomId, string playerId, string newName);
    }

}

