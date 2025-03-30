using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
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
        public List<PlayerDTO> Players { get; set; } = new List<PlayerDTO>();
        public int MaxPlayer { get; set; }
        public bool HasPassword { get; set; }
        public string Status { get; set; }// "Waiting", "Playing", "Ended"
        public string AgeGroup { get; set; }
        public string Mode { get; set; }
        public string CreatedBy { get; set; }
        public bool IsActive { get; set; }
    }
}
