using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.DTOs.GameSession
{
    public class SessionHistoryDTO
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public List<QuestionDetailDTO> Questions { get; set; }
        public string Status { get; set; }
        public string Response { get; set; }
        public string ResponseUrl { get; set; }
        public int PointsEarned { get; set; }
        public DateTime Timestamp { get; set; }
    }
    public class QuestionDetailDTO
    {
        public string QuestionId { get; set; }
        public string QuestionContent { get; set; }
    }
}
