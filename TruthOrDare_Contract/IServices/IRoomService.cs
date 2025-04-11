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
        Task<RoomCreateDTO> CreateRoom(string roomName, string playerId, string playerName, string roomPassword, string ageGroup, string mode, int maxPlayer);
        Task<(string roomId, string playerId,string playerName)> JoinRoom(string roomId, string playerId, string playerName, string roomPassword = null);
        Task<string> LeaveRoom(string roomId, string playerId);
        Task<List<RoomListDTO>> GetListRoom(string? roomId);
        Task<Room> GetRoom(string roomId);
        Task ChangePlayerName(string roomId, string playerId, string newName);
        Task StartGame(string roomId, string playerId);
        Task<(Question question, bool isLastQuestion, int totalQuestions, int usedQuestions)> GetRandomQuestionForRoom(string roomId, string playerId, string questionType);
        Task<EndGameSummaryDTO> EndGame(string roomId, string playerId);
        Task ResetGame(string roomId, string playerId);
        Task<(string nextPlayerId, bool isGameEnded, string message)> NextPlayer(string roomId, string playerId);
        Task<List<Room>> GetActiveRooms();
    }

}

