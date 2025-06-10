using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.DTOs.Player;

namespace TruthOrDare_Contract.DTOs.Room
{
    public class RoomDetailDTO
    {
        public string RoomId { get; set; }
        public string RoomName { get; set; }
        public int PlayerCount { get; set; }
        public int MaxPlayer { get; set; }
        public bool HasPassword { get; set; }
        public string Status { get; set; }
        public string Mode { get; set; }
        public string AgeGroup { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public string CurrentPlayerIdTurn { get; set; }
        public List<PlayerCreateRoomDTO> Players { get; set; } = new List<PlayerCreateRoomDTO>();
    }
}
