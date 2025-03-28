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
        [Required(ErrorMessage = "Room ID is required.")]
        public string RoomId { get; set; }
        public string RoomPassword { get; set; }
        public string PlayerName { get; set; }
    }
}
