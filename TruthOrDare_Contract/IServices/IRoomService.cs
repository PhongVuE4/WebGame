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
        Task<Question> GetRandomQuestionForRoom(string roomId, string playerId, string questionType);
        Task<RoomCreateDTO> CreateRoom(string roomName, string playerName, string roomPassword,string ageGroup, string mode);
        Task<RoomCreateDTO> JoinRoom(string roomId, string? playerName, string? roomPassword);
        Task<Room> LeaveRoom(string roomId, string playerId);
        Task<List<RoomListDTO>> GetListRoom(string? roomId);
        Task<RoomDetailDTO> GetRoom(string roomId);
        Task<Room> GetRoomEntity(string roomId);
        Task ChangePlayerName(string roomId, string playerId, string newName);
        Task StartGame(string roomId, string playerId);
        Task ResetGame(string roomId, string playerId);
    }

}

