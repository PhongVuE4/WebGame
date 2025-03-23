using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.IServices;

namespace TruthOrDare_Core.Services
{
    public class WebSocketHandler : IWebSocketHandler
    {
        private readonly Dictionary<string, List<WebSocket>> _rooms = new();

        public async Task HandleWebSocket(HttpContext context, WebSocket webSocket, string roomId)
        {
            if (!_rooms.ContainsKey(roomId)) _rooms[roomId] = new();
            _rooms[roomId].Add(webSocket);

            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await BroadcastToRoom(roomId, message);
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            _rooms[roomId].Remove(webSocket);
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        public async Task BroadcastToRoom(string roomId, string message)
        {
            if (_rooms.ContainsKey(roomId))
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                foreach (var socket in _rooms[roomId])
                {
                    if (socket.State == WebSocketState.Open)
                        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}
