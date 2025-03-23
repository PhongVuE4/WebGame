using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.IServices
{
    public interface IRoomService
    {
        Task CreateRoomAsync(string roomId, List<string> players);
        Task NextTurnAsync(string roomId);
        Task CompleteTurnAsync(string roomId, string playerId, string response, string responseUrl = null);
    }

}

