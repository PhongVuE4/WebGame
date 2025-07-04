﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using TruthOrDare_Common.Exceptions.Player;
using TruthOrDare_Common.Exceptions.Room;
using TruthOrDare_Contract.IRepository;
using TruthOrDare_Contract.IServices;
using TruthOrDare_Contract.Models;
using TruthOrDare_Core.Services;
using TruthOrDare_Infrastructure;

namespace TruthOrDare_Core.Hubs
{
    public class RoomHub : BaseHub
    {
        private readonly IRoomService _roomService;
        private readonly IQuestionRepository _questionRepository;
        private readonly IMongoCollection<Room> _rooms;
        private readonly IHubContext<RoomHub> _hubContext;
        private readonly GoogleDriveService _driveService;
        private readonly YouTubeService _youTubeService;
        private static readonly ConcurrentDictionary<string, bool> _roomLocks = new ConcurrentDictionary<string, bool>();

        public RoomHub(IRoomService roomService,
            MongoDbContext dbContext,
            IHubContext<RoomHub> hubContext,
            GoogleDriveService driveSerevice,
            YouTubeService youTubeService,
            IQuestionRepository questionRepository)
        {
            _roomService = roomService;
            _rooms = dbContext.Rooms;
            _hubContext = hubContext;
            _driveService = driveSerevice;
            _youTubeService = youTubeService;
            _questionRepository = questionRepository;
        }
        public async Task SendMessage(string roomId, string playerId, string message)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                // Lấy thông tin phòng
                var room = await _roomService.GetRoom(roomId);
                if (room == null)
                {
                    throw new RoomNotExistException(roomId);
                }

                // Tìm người chơi
                var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId && p.IsActive);
                if (player == null)
                {
                    throw new RoomNotFoundPlayerIdException();
                }
                var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var vnTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
                // Gửi tin nhắn đến nhóm
                await Clients.Group(roomId).SendAsync("ReceiveMessage", new
                {
                    message,
                    playerId,
                    playerName = player.PlayerName,
                    messTime = vnTime.ToString("dd/MM/yyyy HH:mm:ss")
                });
            });
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
                    ? room.Players.Where(p => p.IsActive).Select(p => (dynamic)new { p.PlayerId, p.PlayerName }).ToList()
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
                    //Console.WriteLine($"ReconnectPlayer: Player {playerId} đã được kết nối với ConnectionId={Context.ConnectionId}. Bỏ qua cập nhật.");
                    await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                    var pls = room.Players != null ? room.Players
                    .Where(p => p.IsActive).Select(p => new { p.PlayerId, p.PlayerName })
                        .Cast<dynamic>()
                        .ToList() : new List<dynamic>();
                    await Clients.Caller.SendAsync("ReconnectSuccess", new
                    {
                        message = $"Đã kết nối lại vào phòng {roomId}",
                        roomStatus = room.Status,
                        currentPlayerId = room.CurrentPlayerIdTurn,
                        pls,
                    });
                    return;
                }
                //Console.WriteLine($"ReconnectPlayer: roomId={roomId}, playerId={playerId}, playerName={playerName}, isActive={player.IsActive}, connectionId={player.ConnectionId}, playerCount={room.PlayerCount}");
                // Cập nhật connectionId
                var update = Builders<Room>.Update
                        .Set("Players.$[p].connection_id", Context.ConnectionId)
                        .Set("Players.$[p].is_active", true);
                if (!player.IsActive)
                {
                    if (room.MaxPlayer <= room.PlayerCount)
                    {
                        throw new FullPlayerException(room.PlayerCount);
                    }
                    update = update.Inc("PlayerCount", 1);
                }
                var filter = Builders<Room>.Filter.Eq(r => r.RoomId, roomId);
                var arrayFilters = new List<ArrayFilterDefinition>
                {
                    new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("p.player_id", playerId))
                };
                //Console.WriteLine($"ReconnectPlayer: filter=RoomId:{roomId}, arrayFilters=p.PlayerId:{playerId}");

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
                string questionText = "";
                string responseType = "";
                string questionOwnerId = "";
                if (!string.IsNullOrEmpty(room.CurrentQuestionId))
                {
                    var q = await _questionRepository.GetQuestionById(room.CurrentQuestionId);
                    questionText = q?.Text;
                    responseType = q?.ResponseType;
                    questionOwnerId = room.CurrentPlayerIdTurn;
                    var lastEntry = room.History?
                        .Where(h => h.Questions != null && h.Questions.Any(q => q.QuestionId == room.CurrentQuestionId))
                        .OrderByDescending(h => h.Timestamp)
                        .FirstOrDefault();
                    if (lastEntry != null)
                    {
                        questionOwnerId = lastEntry.PlayerId;
                    }

                }
                // Thêm lại vào nhóm SignalR
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

                // Gửi thông tin phòng để đồng bộ client
                var players = room.Players != null ? room.Players.Where(p => p.IsActive).Select(p => new { p.PlayerId, p.PlayerName })
                      .Cast<dynamic>()
                      .ToList() : new List<dynamic>();

                await Clients.Caller.SendAsync("ReconnectSuccess", new
                {
                    message = $"Đã kết nối lại vào phòng {roomId}",
                    roomStatus = room.Status,
                    currentPlayerId = room.CurrentPlayerIdTurn,
                    players,
                    questionOwnerId = questionOwnerId,
                    question = questionText,
                    responseType,

                });
                await Clients.Group(roomId).SendAsync("PlayerReconnected", new
                {
                    playerId,
                    playerName,
                    questionOwnerId = questionOwnerId,
                    question = questionText,
                    message = $"{playerName} đã kết nối lại vào phòng.",
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
                var (question, isLastQuestion, totalQuestions, usedQuestions, responseType) = await _roomService.GetRandomQuestionForRoom(roomId, playerId, questionType);

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
                    Id = question.Id,
                    Text = question.Text,
                    Type = question.Type,
                    playerId,
                    playerName = room.Players.FirstOrDefault(p => p.PlayerId == playerId)?.PlayerName,
                    isLastQuestion,
                    totalQuestions,
                    usedQuestions,
                    responseType
                });
                //await Clients.Caller.SendAsync("GetQuestionSuccess", new
                //{
                //    questionId = question.Id,
                //    questionText = question.Text,
                //    isLastQuestion,
                //    totalQuestions,
                //    usedQuestions
                //});
                await Clients.Caller.SendAsync("GetQuestionSuccess", question);
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

        public async Task UploadChallenge(string roomId, string playerId, string fileName, string mimeType, byte[] fileData)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                // Validate inputs
                if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(fileName) || fileData == null)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new { message = "Room ID, player ID, file name, and file data are required" }
                    });
                    return;
                }

                string mediaUrl;
                using (var stream = new MemoryStream(fileData))
                {
                    if (mimeType.StartsWith("image/"))
                    {
                        // Upload to Google Drive
                        mediaUrl = await _driveService.UploadFile(stream, fileName, mimeType);
                    }
                    else if (mimeType.StartsWith("video/"))
                    {
                        // Upload to YouTube
                        //mediaUrl = await _youTubeService.UploadVideo(stream, fileName, $"Challenge from {playerId} in room {roomId}");
                        mediaUrl = await _youTubeService.UploadVideo(
                            stream,
                            $"Challenge from {playerId} in room {roomId}",
                            $"Submitted on {DateTime.UtcNow.ToString("o")}"
                        );
                    }
                    else
                    {
                        await Clients.Caller.SendAsync("OperationFailed", new
                        {
                            statusCode = 422,
                            errors = new { message = "Unsupported file type. Only images and videos are allowed." }
                        });
                        return;
                    }
                }

                // Broadcast the uploaded media URL to all clients in the room
                await Clients.Group(roomId).SendAsync("ChallengeUploaded", new
                {
                    roomId,
                    playerId,
                    mediaUrl,
                    fileName,
                    mimeType,
                    uploadTime = DateTime.UtcNow.ToString("o")
                });
            });
        }
        public async Task SubmitTextAnswer(string roomId, string playerId, string questionId, string response)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(questionId) || string.IsNullOrEmpty(response))
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new { errorMessage = "Room ID, player ID, question ID, and response are required" }
                    });
                    return;
                }

                var room = await _rooms.Find(r => r.RoomId == roomId && !r.IsDeleted).FirstOrDefaultAsync();
                if (room == null)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new { errorMessage = "Room not found" }
                    });
                    return;
                }
                if (room.Status.ToLower() != "playing")
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new { errorMessage = "Game must be playing" }
                    });
                    return;
                }
                if (room.CurrentPlayerIdTurn != playerId)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new { errorMessage = "Not your turn" }
                    });
                    return;
                }

                var historyItem = room.History.LastOrDefault(h => h.PlayerId == playerId && h.Questions.Any(q => q.QuestionId == questionId) && h.Status == "assigned");
                if (historyItem == null)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new { errorMessage = "No assigned question found for this player" }
                    });
                    return;
                }

                historyItem.Status = "answered";
                historyItem.Response = response;
                historyItem.ResponseType = "text";
                historyItem.Timestamp = DateTime.Now;

                room.LastQuestionTimestamp = null;
                await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, room);

                await Clients.Group(roomId).SendAsync("AnswerSubmitted", new
                {
                    roomId,
                    playerId,
                    playerName = room.Players.FirstOrDefault(p => p.PlayerId == playerId)?.PlayerName,
                    questionId,
                    response,
                    responseUrl = (string)null,
                    responseType = "text",
                    timestamp = DateTime.UtcNow.ToString("o")
                });
            });
        }
        public async Task SubmitMediaAnswer(string roomId, string playerId, string questionId, string responseUrl, string responseType)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(questionId) || string.IsNullOrEmpty(responseUrl) || string.IsNullOrEmpty(responseType))
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new { errorMessage = "Room ID, player ID, question ID, response URL, and response type are required" }
                    });
                    return;
                }

                if (responseType != "image" && responseType != "video")
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new { errorMessage = "Invalid response type. Must be 'image' or 'video'" }
                    });
                    return;
                }

                var room = await _rooms.Find(r => r.RoomId == roomId && !r.IsDeleted).FirstOrDefaultAsync();
                if (room == null)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new { errorMessage = "Room not found" }
                    });
                    return;
                }
                if (room.Status.ToLower() != "playing")
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new { errorMessage = "Game must be playing" }
                    });
                    return;
                }
                if (room.CurrentPlayerIdTurn != playerId)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new { errorMessage = "Not your turn" }
                    });
                    return;
                }

                var historyItem = room.History.LastOrDefault(h => h.PlayerId == playerId && h.Questions.Any(q => q.QuestionId == questionId) && h.Status == "assigned");
                if (historyItem == null)
                {
                    await Clients.Caller.SendAsync("OperationFailed", new
                    {
                        statusCode = 422,
                        errors = new { errorMessage = "No assigned question found for this player" }
                    });
                    return;
                }

                historyItem.Status = "answered";
                historyItem.ResponseUrl = responseUrl;
                historyItem.ResponseType = responseType;
                historyItem.Timestamp = DateTime.Now;

                room.LastQuestionTimestamp = null;
                await _rooms.ReplaceOneAsync(r => r.RoomId == roomId, room);

                await Clients.Group(roomId).SendAsync("AnswerSubmitted", new
                {
                    roomId,
                    playerId,
                    playerName = room.Players.FirstOrDefault(p => p.PlayerId == playerId)?.PlayerName,
                    questionId,
                    response = (string)null,
                    responseUrl,
                    responseType,
                    timestamp = DateTime.UtcNow.ToString("o")
                });
            });
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            bool sendToCaller = false;
            await ExecuteWithErrorHandling(async () =>
            {
                var room = await _rooms
                    .Find(r => r.Players.Any(p => p.ConnectionId == Context.ConnectionId && p.IsActive))
                    .FirstOrDefaultAsync();

                if (room != null)
                {
                    var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId && p.IsActive);
                    if (player != null)
                    {
                        Console.WriteLine($"Player disconnected: PlayerId={player.PlayerId}, PlayerName={player.PlayerName}, RoomId={room.RoomId}");

                        player.ConnectionId = null;
                        await Clients.Group(room.RoomId).SendAsync("ReconnectMessage",
                            $"{player.PlayerName} has disconnected. Waiting for reconnect...");

                        await _rooms.ReplaceOneAsync(r => r.RoomId == room.RoomId, room);

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(30));
                                if (!_roomLocks.TryAdd(room.RoomId, true))
                                {
                                    Console.WriteLine($"Room {room.RoomId} is being processed by another operation. Skipping.");
                                    return;
                                }

                                try
                                {
                                    var updatedRoom = await _rooms
                                        .Find(r => r.RoomId == room.RoomId && r.IsActive && !r.IsDeleted)
                                        .FirstOrDefaultAsync();
                                    if (updatedRoom == null)
                                    {
                                        Console.WriteLine($"Error: Room {room.RoomId} not found or no longer active/deleted after 30 seconds.");
                                        return;
                                    }

                                    var disconnectedPlayer = updatedRoom.Players.FirstOrDefault(p => p.PlayerId == player.PlayerId);
                                    if (disconnectedPlayer == null)
                                    {
                                        Console.WriteLine($"Error: Player {player.PlayerId} not found in room {room.RoomId} after 30 seconds.");
                                        return;
                                    }

                                    if (disconnectedPlayer.ConnectionId != null)
                                    {
                                        Console.WriteLine($"Player {player.PlayerId} reconnected to room {room.RoomId} with ConnectionId={disconnectedPlayer.ConnectionId}.");
                                        return;
                                    }

                                    Console.WriteLine($"Player {player.PlayerId} did not reconnect after 30s. Updating status...");

                                    disconnectedPlayer.IsActive = false;
                                    updatedRoom.PlayerCount--;

                                    if (disconnectedPlayer.IsHost && updatedRoom.Players.Any(p => p.IsActive))
                                    {
                                        Console.WriteLine($"Transferring host from {disconnectedPlayer.PlayerName}.");
                                        disconnectedPlayer.IsHost = false;
                                        var nextActivePlayer = updatedRoom.Players.First(p => p.IsActive);
                                        nextActivePlayer.IsHost = true;
                                    }

                                    try
                                    {
                                        var replaceResult = await _rooms.ReplaceOneAsync(r => r.RoomId == updatedRoom.RoomId, updatedRoom);
                                        if (replaceResult.ModifiedCount == 0)
                                        {
                                            Console.WriteLine($"Warning: No changes saved to room {updatedRoom.RoomId}. Room state may already be updated.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error saving room {updatedRoom.RoomId} to MongoDB: {ex.Message}\n{ex.StackTrace}");
                                        return;
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

                                    if (updatedRoom.Status == "playing" && updatedRoom.CurrentPlayerIdTurn == player.PlayerId)
                                    {
                                        Console.WriteLine($"Current player {player.PlayerId} disconnected. Transferring turn...");

                                        try
                                        {
                                            Console.WriteLine($"Before NextPlayer: Room {updatedRoom.RoomId}, isActive={updatedRoom.IsActive}, isDeleted={updatedRoom.IsDeleted}, currentPlayerIdTurn={updatedRoom.CurrentPlayerIdTurn}");
                                            var (nextPlayerId, isGameEnded, message) = await _roomService.NextPlayer(updatedRoom.RoomId, player.PlayerId);
                                            updatedRoom.CurrentPlayerIdTurn = nextPlayerId;
                                            updatedRoom.LastQuestionTimestamp = null;
                                            updatedRoom.LastTurnTimestamp = DateTime.Now;

                                            if (isGameEnded)
                                            {
                                                updatedRoom.Status = "ended";
                                                Console.WriteLine($"Game ended: {message}");
                                                await _rooms.ReplaceOneAsync(r => r.RoomId == updatedRoom.RoomId, updatedRoom);
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
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Error calling NextPlayer for room {updatedRoom.RoomId}: {ex.Message}\n{ex.StackTrace}");
                                            updatedRoom.Status = "ended";
                                            await _rooms.ReplaceOneAsync(r => r.RoomId == updatedRoom.RoomId, updatedRoom);
                                            await _hubContext.Clients.Group(updatedRoom.RoomId).SendAsync("GameEnded", new
                                            {
                                                message = "Game has ended due to error in transferring turn."
                                            });
                                        }
                                    }
                                }
                                finally
                                {
                                    _roomLocks.TryRemove(room.RoomId, out _);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error in OnDisconnectedAsync Task.Run: {ex.Message}\n{ex.StackTrace}");
                            }
                        });
                    }
                    else
                    {
                        Console.WriteLine($"No active player found for ConnectionId {Context.ConnectionId} in room {room.RoomId}.");
                    }
                }
                else
                {
                    Console.WriteLine($"No room found for ConnectionId {Context.ConnectionId}.");
                }

                await base.OnDisconnectedAsync(exception);
            }, sendToCaller: false);
        }

        public async Task TestConnection(string message)
        {
            Console.WriteLine($"Message from client: {message}");
            await Clients.Caller.SendAsync("ConnectMessage", $"Server received: {message}");
        }
    }
}
