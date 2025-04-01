using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.DTOs.Player
{
    public class PlayerDTO
    {
        public string PlayerId { get; set; }

        public string PlayerName { get; set; }

        public string AgeGroup { get; set; }

        public int TotalPoints { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsHost { get; set; }
        public int QuestionsAnswered { get; set; } = 0;
        public WebSocket WebSocket { get; set; }

    }
}
