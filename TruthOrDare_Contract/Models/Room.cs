using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.Models
{
    public class Room
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("room_id")]
        public string RoomId { get; set; }

        [BsonElement("players")]
        public List<string> Players { get; set; } = new();

        [BsonElement("current_question_id")]
        public string CurrentQuestionId { get; set; }

        [BsonElement("current_player_turn")]
        public string CurrentPlayerTurn { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("ttl_expiry")]
        public DateTime TtlExpiry { get; set; }
        [BsonElement("is_deleted")]
        public bool IsDeleted { get; set; }
    }
}
