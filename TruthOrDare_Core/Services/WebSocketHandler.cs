using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruthOrDare_Contract.IServices;
using TruthOrDare_Contract.Models;

namespace TruthOrDare_Core.Services
{
    public class WebSocketHandler : IWebSocketHandler
    {
        private readonly IRoomService _roomService;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public WebSocketHandler(IRoomService roomService)
        {
            _roomService = roomService;
        }

        public async Task HandleWebSocket(HttpContext context, WebSocket webSocket, string roomId, string playerId)
        {
            Room room;
            try
            {
                room = await _roomService.GetRoom(roomId);
                if (room.Status.ToLower() != "waiting" && room.Status.ToLower() != "playing")
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Room is not active.", CancellationToken.None);
                    return;
                }
            }
            catch (Exception ex)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, $"Error getting room: {ex.Message}", CancellationToken.None);
                return;
            }

            var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Player not found in room.", CancellationToken.None);
                return;
            }

            // Đồng bộ hóa gán WebSocket để tránh race condition
            lock (player)
            {
                player.WebSocket = webSocket;
            }

            await BroadcastMessage(roomId, $"{player.PlayerName} has joined the room.");

            var buffer = new byte[1024 * 4];
            try
            {
                while (webSocket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await BroadcastMessage(roomId, $"{player.PlayerName}: {message}");
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await HandlePlayerDisconnect(roomId, playerId, webSocket, "Client disconnected");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ứng dụng đang tắt, xử lý ngắt kết nối
                await HandlePlayerDisconnect(roomId, playerId, webSocket, "Server shutdown");
            }
            catch (Exception ex)
            {
                await HandlePlayerDisconnect(roomId, playerId, webSocket, $"Error: {ex.Message}");
            }
        }

        private async Task HandlePlayerDisconnect(string roomId, string playerId, WebSocket webSocket, string reason)
        {
            try
            {
                await _roomService.LeaveRoom(roomId, playerId);
                await BroadcastMessage(roomId, $"{(await _roomService.GetRoom(roomId))?.Players.FirstOrDefault(p => p.PlayerId == playerId)?.PlayerName ?? "A player"} has left the room: {reason}");
            }
            finally
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
                }
            }
        }

        public async Task BroadcastMessage(string roomId, string message)
        {
            Room room;
            try
            {
                room = await _roomService.GetRoom(roomId);
                if (room == null) return;
            }
            catch (Exception)
            {
                return; // Bỏ qua nếu không lấy được phòng
            }

            var messageBytes = Encoding.UTF8.GetBytes(message);
            var tasks = new List<Task>();
            foreach (var player in room.Players.Where(p => p.WebSocket != null && p.WebSocket.State == WebSocketState.Open))
            {
                tasks.Add(player.WebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, _cts.Token));
            }
            await Task.WhenAll(tasks);
        }
    }
}