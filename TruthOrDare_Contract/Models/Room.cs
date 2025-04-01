using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace TruthOrDare_Contract.Models
{
    public class Room
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("room_id")]
        public string RoomId { get; set; }
        [BsonElement("room_name")]
        public string RoomName { get; set; }
        [BsonElement("room_password")]
        [JsonIgnore]
        public string? RoomPassword { get; set; }
        [BsonElement("max_player")]
        public int MaxPlayer { get; set; }
        [BsonElement("player_count")]
        public int PlayerCount { get; set; }

        [BsonElement("players")]
        public List<Player> Players { get; set; } = new List<Player>();
        [BsonElement("has_password")]
        public bool HasPassword { get; set; }

        [BsonElement("current_question_id")]
        public string CurrentQuestionId { get; set; }

        [BsonElement("current_player_turn")]
        public string CurrentPlayerIdTurn { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }
        [BsonElement("age_group")]
        public string AgeGroup { get; set; }
        [BsonElement("mode")]
        public string Mode { get; set; }
        [BsonElement("created_by")]
        public string CreatedBy { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }
        [BsonElement("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [BsonElement("is_active")]
        public bool IsActive { get; set; }

        [BsonElement("ttl_expiry")]
        public DateTime TtlExpiry { get; set; }
        [BsonElement("is_deleted")]
        public bool IsDeleted { get; set; }
        [BsonElement("used_question_ids")]
        public List<string> UsedQuestionIds { get; set; } = new List<string>(); // Câu hỏi đã dùng

        [BsonElement("history")]
        public List<SessionHistory> History { get; set; } = new List<SessionHistory>();

        // Thêm trường để lưu thời gian lấy câu hỏi cuối cùng
        [BsonElement("last_question_timestamp")]
        [BsonIgnoreIfNull]
        public DateTime? LastQuestionTimestamp { get; set; }// Thời gian trả lời câu hỏi
        [BsonElement("last_turn_timestamp")]
        [BsonIgnoreIfNull]
        public DateTime? LastTurnTimestamp { get; set; } // Thời gian bắt đầu lượt
        [BsonElement("is_last_question_assigned")]
        [BsonIgnoreIfNull]
        public bool? IsLastQuestionAssigned { get; set; }
    }
}
