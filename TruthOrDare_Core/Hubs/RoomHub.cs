using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using TruthOrDare_Contract.IServices;
using TruthOrDare_Contract.Models;
using TruthOrDare_Infrastructure;

namespace TruthOrDare_Core.Hubs
{
    public class RoomHub : Hub
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
            await Clients.Group(roomId).SendAsync("ReceiveMessage", message);
        }

        public async Task JoinRoom(string roomId, string playerId, string playerName, string roomPassword)
        {
            var connectionId = Context.ConnectionId;
            try
            {
                // Gọi RoomService.JoinRoom
                var result = await _roomService.JoinRoom(roomId, playerId, playerName, roomPassword, connectionId);

                // Thêm người chơi vào nhóm SignalR
                await Groups.AddToGroupAsync(connectionId, roomId);

                // Lấy thông tin phòng và người chơi
                var room = await _roomService.GetRoom(roomId);
                var players = room.Players
                    .Where(p => p.IsActive)
                    .Select(p => new { p.PlayerId, p.PlayerName })
                    .ToList();

                // Thông báo tới người gọi
                await Clients.Caller.SendAsync("JoinRoomSuccess", $"Đã vào phòng {roomId} thành công với tên {result.playerName}");

                // Thông báo tới tất cả người chơi trong phòng (bao gồm người mới) về danh sách người chơi
                await Clients.Group(roomId).SendAsync("PlayerListUpdated", players);

                // Thông báo tới các người chơi khác (trừ người mới) rằng có người vào
                await Clients.OthersInGroup(roomId).SendAsync("PlayerJoined", playerName);
            }
            catch (Exception ex)
            {
                // Gửi thông báo lỗi tới người gọi
                await Clients.Caller.SendAsync("JoinRoomFailed", ex.Message);
            }
        }

        public async Task LeaveRoom(string roomId)
        {
            var connectionId = Context.ConnectionId;

            try
            {
                // Xóa người chơi khỏi nhóm SignalR
                await Groups.RemoveFromGroupAsync(connectionId, roomId);

                // Lấy thông tin phòng
                var room = await _roomService.GetRoom(roomId);
                var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId && p.IsActive);
                if (player != null)
                {
                    // Gọi RoomService.LeaveRoom để cập nhật trạng thái phòng
                    await _roomService.LeaveRoom(roomId, player.PlayerId);

                    // Lấy danh sách người chơi còn lại
                    room = await _roomService.GetRoom(roomId); // Lấy lại phòng sau khi rời
                    var players = room.Players
                        .Where(p => p.IsActive)
                        .Select(p => new { p.PlayerId, p.PlayerName })
                        .ToList();

                    // Thông báo tới các người chơi còn lại
                    await Clients.Group(roomId).SendAsync("PlayerListUpdated", players);
                    await Clients.Group(roomId).SendAsync("PlayerLeft", player.PlayerName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi rời phòng: {ex.Message}");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            try
            {
                // Tìm phòng chứa connectionId
                var rooms = await _rooms.Find(r => r.IsActive && !r.IsDeleted).ToListAsync();
                foreach (var room in rooms)
                {
                    var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId && p.IsActive);
                    if (player != null)
                    {
                        // Xóa khỏi nhóm SignalR
                        await Groups.RemoveFromGroupAsync(connectionId, room.RoomId);

                        // Gọi LeaveRoom để cập nhật trạng thái
                        await _roomService.LeaveRoom(room.RoomId, player.PlayerId);

                        // Lấy danh sách người chơi còn lại
                        var updatedRoom = await _roomService.GetRoom(room.RoomId);
                        var players = updatedRoom.Players
                            .Where(p => p.IsActive)
                            .Select(p => new { p.PlayerId, p.PlayerName })
                            .ToList();

                        // Thông báo tới các người chơi còn lại
                        await Clients.Group(room.RoomId).SendAsync("PlayerListUpdated", players);
                        await Clients.Group(room.RoomId).SendAsync("PlayerLeft", player.PlayerName);

                        break; // Thoát vòng lặp sau khi xử lý
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi ngắt kết nối: {ex.Message}");
            }

            Console.WriteLine($"Client disconnected: {connectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task TestConnection(string message)
        {
            Console.WriteLine($"Message from client: {message}");
            await Clients.Caller.SendAsync("ReceiveMessage", $"Server received: {message}");
        }
    }
}
