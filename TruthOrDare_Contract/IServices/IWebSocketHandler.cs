using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;


namespace TruthOrDare_Contract.IServices
{
    public interface IWebSocketHandler
    {
        Task HandleWebSocket(HttpContext context, WebSocket webSocket, string roomId, string playerId);
        Task BroadcastMessage(string roomId, string message);
    }
}
