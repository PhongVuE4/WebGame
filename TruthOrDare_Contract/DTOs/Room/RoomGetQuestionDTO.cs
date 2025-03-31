using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.DTOs.Room
{
    public class RoomGetQuestionDTO
    {
        public string PlayerId { get; set; }
        public string QuestionType { get; set; }
    }
}
