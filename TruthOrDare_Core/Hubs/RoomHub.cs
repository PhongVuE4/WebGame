using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using TruthOrDare_Common.Exceptions.Player;
using TruthOrDare_Common.Exceptions.Room;
using TruthOrDare_Contract.IServices;
using TruthOrDare_Contract.Models;
using TruthOrDare_Infrastructure;

namespace TruthOrDare_Core.Hubs
{
    public class RoomHub : BaseHub
    {
        private readonly IRoomService _roomService;
        private readonly IMongoCollection<Room> _rooms;
        public RoomHub(IRoomService roomService, MongoDbContext dbContext)
        {
            _roomService = roomService;
            _rooms = dbContext.Rooms;
        }
        public async Task SendMessage(string roomId, string message)
        {
            Console.WriteLine($"Gửi tin nhắn đến phòng {roomId}: {message}");
            await Clients.Group(roomId).SendAsync("ReceiveMessage", message);
        }
        public async Task CreateRoom(string roomName, string playerId, string playerName, string roomPassword, string ageGroup, string mode, int maxPlayer)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                // Gọi RoomService.CreateRoom
                var roomDTO = await _roomService.CreateRoom(roomName, playerId, playerName, roomPassword, ageGroup, mode, maxPlayer, Context.ConnectionId);

                // Gửi thông báo thành công tới client
                await Clients.Caller.SendAsync("CreateRoomSuccess", new
                {
                    roomId = roomDTO.RoomId,
                    roomName = roomDTO.RoomName,
                    message = $"Phòng {roomDTO.RoomName} đã được tạo thành công."
                });

                // Gửi sự kiện PlayerJoined cho nhóm
                await Clients.Group(roomDTO.RoomId).SendAsync("PlayerJoined", roomDTO.Players.First().PlayerName);
            });
        }
        public async Task JoinRoom(string roomId, string playerId, string playerName, string password)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                // Gọi RoomService.JoinRoom để xử lý logic kiểm tra và cập nhật
                var (returnedRoomId, returnedPlayerId, returnedPlayerName) = await _roomService.JoinRoom(
                    roomId,
                    playerId,
                    playerName,
                    password,
                    Context.ConnectionId
                );

                // Thêm người chơi vào nhóm SignalR
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

                // Lấy thông tin phòng để gửi danh sách người chơi
                var room = await _rooms
                    .Find(r => r.RoomId == roomId && r.IsActive && !r.IsDeleted)
                    .FirstOrDefaultAsync();

                // Gửi thông báo và cập nhật danh sách người chơi
                await Clients.Group(roomId).SendAsync("PlayerJoined", returnedPlayerName);
                var players = room.Players != null
                    ? room.Players.Select(p => (dynamic)new { p.PlayerId, p.PlayerName }).ToList()
                    : new List<dynamic>();
                await Clients.Group(roomId).SendAsync("PlayerListUpdated", players);
                await Clients.Caller.SendAsync("JoinRoomSuccess", $"Đã vào phòng {roomId} thành công với tên {returnedPlayerName}");
            });
            
        }
        public async Task ReconnectPlayer(string roomId, string playerId, string playerName)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                var room = await _rooms.Find(r => r.RoomId == roomId).FirstOrDefaultAsync();
                if (room == null)
                {
                    throw new RoomNotExistException(roomId);
                }

                // Kiểm tra xem playerId đã trong phòng, khớp playerName, và IsActive
                var player = room.Players?.FirstOrDefault(p => p.PlayerId == playerId && p.PlayerName == playerName);
                if (player == null)
                {
                    throw new PlayerIdNotFound(playerId);
                }
                if (!player.IsActive)
                {
                    throw new PlayerNotActiveException();
                }

                // Cập nhật connectionId
                var update = Builders<Room>.Update
                        .Set("Players.$[p].ConnectionId", Context.ConnectionId)
                        .Set("Players.$[p].IsActive", true);
                var filter = Builders<Room>.Filter.Eq(r => r.RoomId, roomId);
                var options = new UpdateOptions
                {
                    ArrayFilters = new List<ArrayFilterDefinition>
                {
                    new JsonArrayFilterDefinition<Room>("{ 'p.PlayerId': '" + playerId + "' }")
                }
                };

                await _rooms.UpdateOneAsync(filter, update, options);

                // Thêm lại vào nhóm SignalR
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

                // Gửi thông tin phòng để đồng bộ client
                var players = room.Players != null ? room.Players.Select(p => new { p.PlayerId, p.PlayerName })
                      .Cast<dynamic>()
                      .ToList() : new List<dynamic>();

                await Clients.Caller.SendAsync("ReconnectSuccess", new
                {
                    message = $"Đã kết nối lại vào phòng {roomId}",
                    roomStatus = room.Status,
                    currentPlayerId = room.CurrentPlayerIdTurn,
                    players
                });
                await Clients.Group(roomId).SendAsync("PlayerReconnected", new
                {
                    playerId,
                    playerName,
                    message = $"{playerName} đã kết nối lại vào phòng."
                });
            });
        }
        public async Task LeaveRoom(string roomId, string playerId)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                // Lấy phòng và tìm người chơi dựa trên connectionId
                var room = await _roomService.GetRoom(roomId);
                if (room == null)
                {
                    throw new RoomNotExistException(roomId);
                }
                var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId && p.IsActive);
                if (player == null)
                {
                    throw new PlayerIdNotFound(playerId);
                }

                // Gọi RoomService.LeaveRoom
                await _roomService.LeaveRoom(roomId, playerId);

                // Lấy lại phòng sau khi rời
                room = await _roomService.GetRoom(roomId);

                // Gửi thông báo tới client
                var players = room.Players
                    .Where(p => p.IsActive)
                    .Select(p => (dynamic)new { p.PlayerId, p.PlayerName })
                    .ToList();
                await Clients.Group(roomId).SendAsync("PlayerListUpdated", players);
                await Clients.Group(roomId).SendAsync("PlayerLeft", player.PlayerName);
                await Clients.Caller.SendAsync("LeaveRoomSuccess", $"Đã rời phòng {roomId}");
            });
        }
        public async Task ChangePlayerName(string roomId, string playerId, string newName)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                await _roomService.ChangePlayerName(roomId, playerId, newName);

                // Cập nhật danh sách người chơi cho nhóm
                var room = await _roomService.GetRoom(roomId);
                var players = room.Players
                    .Where(p => p.IsActive)
                    .Select(p => new { p.PlayerId, p.PlayerName })
                    .ToList();
                await Clients.Group(roomId).SendAsync("PlayerListUpdated", players);
                await Clients.Caller.SendAsync("ChangePlayerNameSuccess", $"Đã đổi tên thành {newName}");
            });
        }
        public async Task StartGame(string roomId, string playerId)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                await _roomService.StartGame(roomId, playerId);

                // Thông báo game bắt đầu
                var room = await _roomService.GetRoom(roomId);
                await Clients.Group(roomId).SendAsync("GameStarted", new
                {
                    roomId,
                    currentPlayerId = room.CurrentPlayerIdTurn,
                    message = "Game has started!"
                });
                await Clients.Caller.SendAsync("StartGameSuccess", "Game đã bắt đầu thành công!");
            });
        }
        public async Task GetRandomQuestionForRoom(string roomId, string playerId, string questionType)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                var (question, isLastQuestion, totalQuestions, usedQuestions) = await _roomService.GetRandomQuestionForRoom(roomId, playerId, questionType);

                var room = await _roomService.GetRoom(roomId);
                if (question == null)
                {
                    // Game kết thúc do hết câu hỏi hoặc không còn người chơi
                    await Clients.Group(roomId).SendAsync("GameEnded", new
                    {
                        roomId,
                        message = "Game has ended due to no more questions or active players."
                    });
                    return;
                }

                // Thông báo câu hỏi mới
                await Clients.Group(roomId).SendAsync("QuestionAssigned", new
                {
                    questionId = question.Id,
                    questionText = question.Text,
                    questionType,
                    playerId,
                    playerName = room.Players.FirstOrDefault(p => p.PlayerId == playerId)?.PlayerName,
                    isLastQuestion,
                    totalQuestions,
                    usedQuestions
                });
                await Clients.Caller.SendAsync("GetQuestionSuccess", new
                {
                    questionId = question.Id,
                    questionText = question.Text,
                    isLastQuestion,
                    totalQuestions,
                    usedQuestions
                });
            });
        }

        public async Task EndGame(string roomId, string playerId)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                var summary = await _roomService.EndGame(roomId, playerId);

                // Thông báo game kết thúc
                await Clients.Group(roomId).SendAsync("GameEnded", new
                {
                    roomId,
                    summary.TotalQuestions,
                    summary.PlayerStats,
                    message = "Game has ended."
                });
                await Clients.Caller.SendAsync("EndGameSuccess", "Game đã kết thúc thành công!");
            });
        }

        public async Task ResetGame(string roomId, string playerId)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                await _roomService.ResetGame(roomId, playerId);

                // Thông báo game được reset
                var room = await _roomService.GetRoom(roomId);
                var players = room.Players
                    .Where(p => p.IsActive)
                    .Select(p => new { p.PlayerId, p.PlayerName, QuestionsAnswered = 0 })
                    .ToList();
                await Clients.Group(roomId).SendAsync("GameReset", new
                {
                    roomId,
                    players,
                    message = "Game has been reset."
                });
                await Clients.Caller.SendAsync("ResetGameSuccess", "Game đã được reset thành công!");
            });
        }

        public async Task NextPlayer(string roomId, string playerId)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                var (nextPlayerId, isGameEnded, message) = await _roomService.NextPlayer(roomId, playerId);

                if (isGameEnded)
                {
                    // Thông báo game kết thúc
                    await Clients.Group(roomId).SendAsync("GameEnded", new
                    {
                        roomId,
                        message
                    });
                    return;
                }

                // Thông báo lượt tiếp theo
                var room = await _roomService.GetRoom(roomId);
                var nextPlayerName = room.Players.FirstOrDefault(p => p.PlayerId == nextPlayerId)?.PlayerName;
                await Clients.Group(roomId).SendAsync("NextPlayerTurn", new
                {
                    nextPlayerId,
                    nextPlayerName,
                    message = $"Lượt của {nextPlayerName}"
                });
                await Clients.Caller.SendAsync("NextPlayerSuccess", $"Đã chuyển lượt sang {nextPlayerName}");
            });
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            bool sendToCaller = true;
            await ExecuteWithErrorHandling(async () =>
            {
                var connectionId = Context.ConnectionId;
                var rooms = await _rooms.Find(r => r.IsActive && !r.IsDeleted).ToListAsync();
                foreach (var room in rooms)
                {
                    var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId && p.IsActive);
                    if (player != null)
                    {
                        await Groups.RemoveFromGroupAsync(connectionId, room.RoomId);
                        await _roomService.LeaveRoom(room.RoomId, player.PlayerId);

                        var updatedRoom = await _roomService.GetRoom(room.RoomId);
                        var players = updatedRoom.Players
                            .Where(p => p.IsActive)
                            .Select(p => new { p.PlayerId, p.PlayerName })
                            .ToList();

                        await Clients.Group(room.RoomId).SendAsync("PlayerListUpdated", players);
                        await Clients.Group(room.RoomId).SendAsync("PlayerLeft", player.PlayerName);
                        break;
                    }
                }
            }, sendToCaller = false);

            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task TestConnection(string message)
        {
            Console.WriteLine($"Message from client: {message}");
            await Clients.Caller.SendAsync("ReceiveMessage", $"Server received: {message}");
        }
    }
}
