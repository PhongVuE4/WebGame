using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.DTOs.Room
{
    public class JoinRoomDTO
    {
        public string RoomPassword { get; set; }
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string ConnectionId { get; set; }
    }
}
