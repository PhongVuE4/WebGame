using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
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
        private readonly IHubContext<RoomHub> _hubContext;
        public RoomHub(IRoomService roomService, MongoDbContext dbContext, IHubContext<RoomHub> hubContext)
        {
            _roomService = roomService;
            _rooms = dbContext.Rooms;
            _hubContext = hubContext;
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
                // Kiểm tra xem ConnectionId đã được cập nhật chưa
                if (player.ConnectionId == Context.ConnectionId && player.IsActive)
                {
                    Console.WriteLine($"ReconnectPlayer: Player {playerId} đã được kết nối với ConnectionId={Context.ConnectionId}. Bỏ qua cập nhật.");
                    await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                    var pls = room.Players != null ? room.Players.Select(p => new { p.PlayerId, p.PlayerName })
                        .Cast<dynamic>()
                        .ToList() : new List<dynamic>();
                    await Clients.Caller.SendAsync("ReconnectSuccess", new
                    {
                        message = $"Đã kết nối lại vào phòng {roomId}",
                        roomStatus = room.Status,
                        currentPlayerId = room.CurrentPlayerIdTurn,
                        pls
                    });
                    return;
                }
                //if (!player.IsActive)
                //{
                //    throw new PlayerNotActiveException();
                //}
                Console.WriteLine($"ReconnectPlayer: roomId={roomId}, playerId={playerId}, playerName={playerName}, isActive={player.IsActive}, connectionId={player.ConnectionId}, playerCount={room.PlayerCount}");
                // Cập nhật connectionId
                var update = Builders<Room>.Update
                        .Set("Players.$[p].connection_id", Context.ConnectionId)
                        .Set("Players.$[p].is_active", true);
                if (!player.IsActive)
                {
                    update = update.Inc("PlayerCount", 1);
                }
                var filter = Builders<Room>.Filter.Eq(r => r.RoomId, roomId);
                var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("p.player_id", playerId))
        };
                Console.WriteLine($"ReconnectPlayer: filter=RoomId:{roomId}, arrayFilters=p.PlayerId:{playerId}");

                var updateResult = await _rooms.UpdateOneAsync(
                       filter,
                       update,
                       new UpdateOptions { ArrayFilters = arrayFilters }
                   );

                if (updateResult.ModifiedCount == 0)
                {

                    Console.WriteLine($"ReconnectPlayer failed: No player updated for roomId={roomId}, playerId={playerId}");
                    Console.WriteLine($"Room players: {string.Join(", ", room.Players.Select(p => $"Id={p.PlayerId}, Name={p.PlayerName}, IsActive={p.IsActive}, ConnectionId={p.ConnectionId}"))}");

                    throw new Exception("Failed to update player connection.");
                }
                // Lấy lại room để đảm bảo dữ liệu mới nhất
                room = await _rooms.Find(r => r.RoomId == roomId).FirstOrDefaultAsync();
                //await _rooms.UpdateOneAsync(filter, update, arrayFilter);

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
                await Clients.Group(roomId).SendAsync("PlayerListUpdated", players);
            });
        }
        public async Task LeaveRoom(string roomId, string playerId)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                // Gọi RoomService.LeaveRoom
                var result = await _roomService.LeaveRoom(roomId, playerId);

                // Lấy phòng để cập nhật trạng thái mới nhất
                var room = await _roomService.GetRoom(roomId);
                if (room == null)
                {
                    throw new RoomNotExistException(roomId);
                }
                var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId);
                if (player == null)
                {
                    throw new PlayerIdNotFound(playerId);
                }

                // Gửi thông báo rời phòng
                await Clients.Caller.SendAsync("LeaveRoomSuccess", $"Đã rời phòng {roomId}");

                if (room.PlayerCount > 0 && room.Players.Any(p => p.IsActive))
                {
                    // Gửi danh sách người chơi còn active
                    var players = room.Players
                        .Where(p => p.IsActive)
                        .Select(p => new { p.PlayerId, p.PlayerName })
                        .ToList();
                    Console.WriteLine($"Sending PlayerListUpdated to group {roomId}: {string.Join(", ", players.Select(p => $"{p.PlayerName} ({p.PlayerId})"))}");
                    await Clients.Group(roomId).SendAsync("PlayerListUpdated", players);
                    await Clients.Group(roomId).SendAsync("PlayerLeft", new
                    {
                        playerId,
                        message = $"{player.PlayerName} has left the room."
                    });

                    // Nếu lượt thay đổi (result là nextPlayerId), gửi NextPlayerTurn và CurrentTurn
                    if (result != "Leave room success")
                    {
                        string nextPlayerId = result;
                        var nextPlayer = room.Players.FirstOrDefault(p => p.PlayerId == nextPlayerId);
                        if (nextPlayer != null)
                        {
                            Console.WriteLine($"Sending NextPlayerTurn: {nextPlayer.PlayerName} ({nextPlayer.PlayerId})");
                            await Clients.Group(roomId).SendAsync("NextPlayerTurn", new
                            {
                                nextPlayerId = nextPlayer.PlayerId,
                                nextPlayerName = nextPlayer.PlayerName,
                                isHost = nextPlayer.IsHost,
                                message = $"Turn passed to {nextPlayer.PlayerName} because {player.PlayerName} left the room."
                            });

                            Console.WriteLine($"Sending CurrentTurn: {nextPlayer.PlayerName} ({nextPlayer.PlayerId})");
                            await Clients.Group(roomId).SendAsync("CurrentTurn", new
                            {
                                currentPlayerId = nextPlayer.PlayerId,
                                currentPlayerName = nextPlayer.PlayerName,
                                isHost = nextPlayer.IsHost,
                                message = $"Current turn is {nextPlayer.PlayerName}."
                            });
                        }
                        else
                        {
                            Console.WriteLine($"Error: Next player {nextPlayerId} not found in room {roomId}.");
                        }
                    }
                }
                else
                {
                    // Phòng trống, gửi thông báo kết thúc
                    await Clients.Group(roomId).SendAsync("GameEnded", new
                    {
                        roomId,
                        message = "Game has ended due to no active players."
                    });
                }
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
                    currentPlayerName = room.Players.FirstOrDefault(p => p.PlayerId == room.CurrentPlayerIdTurn)?.PlayerName,
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
            bool sendToCaller = false;
            await ExecuteWithErrorHandling(async () =>
            {
                // Tìm phòng chứa người chơi dựa trên ConnectionId
                var room = await _rooms
                    .Find(r => r.Players.Any(p => p.ConnectionId == Context.ConnectionId && p.IsActive))
                    .FirstOrDefaultAsync();

                if (room != null)
                {
                    var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId && p.IsActive);
                    if (player != null)
                    {
                        Console.WriteLine($"Player disconnected: PlayerId={player.PlayerId}, PlayerName={player.PlayerName}, RoomId={room.RoomId}");

                        // Không đặt IsActive = false ngay, chỉ xóa ConnectionId
                        player.ConnectionId = null;
                        // Gửi thông báo tạm thời tới nhóm
                        await Clients.Group(room.RoomId).SendAsync("ReceiveMessage",
                            $"{player.PlayerName} has disconnected. Waiting for reconnect...");

                        // Cập nhật phòng để lưu trạng thái ConnectionId = null
                        await _rooms.ReplaceOneAsync(r => r.RoomId == room.RoomId, room);

                        // Chờ 30 giây để kiểm tra reconnect
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(TimeSpan.FromSeconds(30));
                            var updatedRoom = await _rooms.Find(r => r.RoomId == room.RoomId).FirstOrDefaultAsync();
                            if (updatedRoom != null)
                            {
                                var disconnectedPlayer = updatedRoom.Players.FirstOrDefault(p => p.PlayerId == player.PlayerId);
                                if (disconnectedPlayer != null && disconnectedPlayer.ConnectionId == null)
                                {
                                    Console.WriteLine($"Player {player.PlayerId} did not reconnect after 30s. Updating status...");

                                    // Người chơi không reconnect: đặt IsActive = false và giảm PlayerCount
                                    disconnectedPlayer.IsActive = false;
                                    updatedRoom.PlayerCount--;

                                    // Nếu người chơi là host, chuyển quyền host
                                    if (disconnectedPlayer.IsHost && updatedRoom.Players.Any(p => p.IsActive))
                                    {
                                        Console.WriteLine($"Transferring host from {disconnectedPlayer.PlayerName}.");

                                        disconnectedPlayer.IsHost = false;
                                        var nextActivePlayer = updatedRoom.Players.First(p => p.IsActive);
                                        nextActivePlayer.IsHost = true;
                                    }

                                    var updatedPlayers = updatedRoom.Players
                                        .Where(p => p.IsActive)
                                        .Select(p => new { p.PlayerId, p.PlayerName })
                                        .ToList();
                                    Console.WriteLine($"Sending PlayerListUpdated to group {updatedRoom.RoomId}: {string.Join(", ", updatedPlayers.Select(p => $"{p.PlayerName} ({p.PlayerId})"))}");
                                    await _hubContext.Clients.Group(updatedRoom.RoomId).SendAsync("PlayerListUpdated", updatedPlayers);
                                    await _hubContext.Clients.Group(updatedRoom.RoomId).SendAsync("PlayerLeft", new
                                    {
                                        playerId = player.PlayerId,
                                        message = $"{player.PlayerName} has left the room."
                                    });

                                    // Nếu người chơi là CurrentPlayerIdTurn, chuyển lượt
                                    if (updatedRoom.Status == "playing" && updatedRoom.CurrentPlayerIdTurn == player.PlayerId)
                                    {
                                        Console.WriteLine($"Current player {player.PlayerId} disconnected. Transferring turn...");

                                        var (nextPlayerId, isGameEnded, message) = await _roomService.NextPlayer(updatedRoom.RoomId, player.PlayerId);
                                        updatedRoom.CurrentPlayerIdTurn = nextPlayerId;
                                        updatedRoom.LastQuestionTimestamp = null;
                                        updatedRoom.LastTurnTimestamp = DateTime.Now;
                                        if (isGameEnded)
                                        {
                                            updatedRoom.Status = "ended";
                                            Console.WriteLine($"Game ended: {message}");
                                            await _rooms.ReplaceOneAsync(r => r.RoomId == updatedRoom.RoomId, updatedRoom);
                                            Console.WriteLine($"Sending GameEnded: {message}");
                                            await _hubContext.Clients.Group(updatedRoom.RoomId).SendAsync("GameEnded", new
                                            {
                                                message = message ?? "Game has ended."
                                            });
                                        }
                                        else if (nextPlayerId != null)
                                        {
                                            var nextPlayer = updatedRoom.Players.FirstOrDefault(p => p.PlayerId == nextPlayerId);
                                            if (nextPlayer != null)
                                            {
                                                await _rooms.ReplaceOneAsync(r => r.RoomId == updatedRoom.RoomId, updatedRoom);
                                                Console.WriteLine($"Sending NextPlayerTurn: {nextPlayer.PlayerName} ({nextPlayer.PlayerId})");
                                                await _hubContext.Clients.Group(updatedRoom.RoomId).SendAsync("NextPlayerTurn", new
                                                {
                                                    nextPlayerId = nextPlayer.PlayerId,
                                                    nextPlayerName = nextPlayer.PlayerName,
                                                    isHost = nextPlayer.IsHost,
                                                    message = $"Turn passed to {nextPlayer.PlayerName} because {player.PlayerName} left the room."
                                                });

                                                Console.WriteLine($"Sending CurrentTurn: {nextPlayer.PlayerName} ({nextPlayer.PlayerId})");
                                                await _hubContext.Clients.Group(updatedRoom.RoomId).SendAsync("CurrentTurn", new
                                                {
                                                    currentPlayerId = nextPlayer.PlayerId,
                                                    currentPlayerName = nextPlayer.PlayerName,
                                                    isHost = nextPlayer.IsHost,
                                                    message = $"Current turn is {nextPlayer.PlayerName}."
                                                });
                                            }
                                            else
                                            {
                                                Console.WriteLine($"Error: Next player {nextPlayerId} not found in room {updatedRoom.RoomId}.");
                                            }
                                        }
                                        else
                                        {
                                            await _rooms.ReplaceOneAsync(r => r.RoomId == updatedRoom.RoomId, updatedRoom);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Player {player.PlayerId} reconnected or not found in room {room.RoomId}.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Error: Room {room.RoomId} not found after 30 seconds.");
                                }
                            }
                        });
                    }
                }

                await base.OnDisconnectedAsync(exception);
            }, sendToCaller: false);

        }

        public async Task TestConnection(string message)
        {
            Console.WriteLine($"Message from client: {message}");
            await Clients.Caller.SendAsync("ReceiveMessage", $"Server received: {message}");
        }
    }
}
