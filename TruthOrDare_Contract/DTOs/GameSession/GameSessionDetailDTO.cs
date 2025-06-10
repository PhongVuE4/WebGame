using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.DTOs.GameSession
{
    public class GameSessionDetailDTO
    {
        public string Id { get; set; }
        public string RoomId { get; set; }
        public string RoomName { get; set; }
        public string Mode { get; set; }
        public string AgeGroup { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public List<SessionHistoryDTO> History { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsDeleted { get; set; }
    }
}
