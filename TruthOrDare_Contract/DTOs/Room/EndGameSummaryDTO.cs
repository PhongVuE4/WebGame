using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.DTOs.Player;

namespace TruthOrDare_Contract.DTOs.Room
{
    public class EndGameSummaryDTO
    {
        public string RoomId { get; set; }
        public int TotalQuestions { get; set; }
        public List<PlayerStatDTO> PlayerStats { get; set; }
    }
}
