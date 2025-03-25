using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.DTOs.Room
{
    public class JoinRoomDTO
    {
        public string RoomId { get; set; }
        public string RoomPassword { get; set; }
        public string PlayerName { get; set; }
    }
}
