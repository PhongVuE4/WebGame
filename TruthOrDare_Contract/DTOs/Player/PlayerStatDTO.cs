using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.DTOs.Player
{
    public class PlayerStatDTO
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int QuestionsAnswered { get; set; }
    }
}
