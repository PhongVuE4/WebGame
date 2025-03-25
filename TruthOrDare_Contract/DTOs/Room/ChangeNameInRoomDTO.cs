using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.DTOs.Room
{
    public class ChangeNameInRoomDTO
    {
        public string RoomId { get; set; }
        public string PlayerId { get; set; }
        public string NewName { get; set; }
    }
}
