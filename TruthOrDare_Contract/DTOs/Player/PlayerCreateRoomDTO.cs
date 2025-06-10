using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.DTOs.Player
{
    public class PlayerCreateRoomDTO
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public bool IsHost { get; set; }
        public bool IsActive { get; set; }
        public string ConnectionId { get; set; }
    }
}
