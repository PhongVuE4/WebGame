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
            var room = await _rooms.Find(r => r.RoomId == roomId).FirstOrDefaultAsync();
            if (room == null)
            {
                await Clients.Caller.SendAsync("ReconnectFailed", "Phòng không tồn tại");
                return;
            }

            // Kiểm tra xem playerId đã trong phòng, khớp playerName, và IsActive
            var player = room.Players?.FirstOrDefault(p => p.PlayerId == playerId && p.PlayerName == playerName);
            if (player == null)
            {
                await Clients.Caller.SendAsync("ReconnectFailed", "Bạn chưa tham gia phòng này");
                return;
            }
            if (!player.IsActive)
            {
                await Clients.Caller.SendAsync("ReconnectFailed", "Bạn đã rời phòng, vui lòng tham gia lại qua trang join");
                return;
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

            await Clients.Caller.SendAsync("ReconnectSuccess", $"Đã kết nối lại vào phòng {roomId}");
            await Clients.Caller.SendAsync("PlayerListUpdated", players);
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
