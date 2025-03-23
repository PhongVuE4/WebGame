using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.Models
{
    public class Player
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("player_id")]
        public string PlayerId { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("age_group")]
        public string AgeGroup { get; set; }

        [BsonElement("total_points")]
        public int TotalPoints { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("last_active")]
        public DateTime LastActive { get; set; }
        [BsonElement("is_deleted")]
        public bool IsDeleted { get; set; }
    }
}
