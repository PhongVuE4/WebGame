using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.DTOs.GameSession;
using TruthOrDare_Contract.Models;

namespace TruthOrDare_Contract.IRepository
{
    public interface IGameSessionsRepository
    {
        Task<List<GameSession>> GetGameSessions(string? filters);
        Task<GameSessionDetailDTO> GetGameSessionDetail(string filters);
    }
}
