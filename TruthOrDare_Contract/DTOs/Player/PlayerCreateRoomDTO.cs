using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.DTOs.Player
{
    public class PlayerCreateRoomDTO
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public bool IsHost { get; set; }
    }
}
