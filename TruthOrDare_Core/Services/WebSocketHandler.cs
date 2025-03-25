using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.IServices;
using TruthOrDare_Contract.Models;

namespace TruthOrDare_Core.Services
{
    public class WebSocketHandler : IWebSocketHandler
    {
        private readonly IRoomService _roomService;

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
            }
            catch (Exception ex)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, ex.Message, CancellationToken.None);
                return;
            }

            var player = room.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Player not found in room.", CancellationToken.None);
                return;
            }

            player.WebSocket = webSocket;
            await BroadcastMessage(roomId, $"{player.PlayerName} has joined the room.");

            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result;

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await BroadcastMessage(roomId, $"{player.PlayerName}: {message}");
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _roomService.LeaveRoom(roomId, playerId);
                        await BroadcastMessage(roomId, $"{player.PlayerName} has left the room.");
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnected", CancellationToken.None);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                await _roomService.LeaveRoom(roomId, playerId);
                await BroadcastMessage(roomId, $"{player.PlayerName} has left the room due to an error: {ex.Message}");
            }
        }

        public async Task BroadcastMessage(string roomId, string message)
        {
            var room = await _roomService.GetRoom(roomId);
            if (room == null) return;

            var messageBytes = Encoding.UTF8.GetBytes(message);
            foreach (var player in room.Players.Where(p => p.WebSocket != null && p.WebSocket.State == WebSocketState.Open))
            {
                await player.WebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
