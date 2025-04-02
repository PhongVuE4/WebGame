using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.Models
{
    public class GameSession
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("room_id")]
        public string RoomId { get; set; }
        [BsonElement("room_name")]
        public string RoomName { get; set; }

        [BsonElement("start_time")]
        public DateTime StartTime { get; set; }

        [BsonElement("end_time")]
        [BsonIgnoreIfNull]
        public DateTime? EndTime { get; set; }
        [BsonElement("history")]
        public List<SessionHistory> History { get; set; } = new();
        [BsonElement("total_questions")]
        public int TotalQuestions { get; set; }
        [BsonElement("is_deleted")]
        public bool IsDeleted { get; set; }
    }

    public class SessionHistory
    {
        [BsonElement("question_id")]
        public string QuestionId { get; set; }
        [BsonElement("question_text")]
        public string QuestionText { get; set; }
        [BsonElement("player_id")]
        public string PlayerId { get; set; }
        [BsonElement("player_name")]
        public string PlayerName { get; set; }
        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("response")]
        [BsonIgnoreIfNull]
        public string Response { get; set; }

        [BsonElement("response_url")]
        [BsonIgnoreIfNull]
        public string ResponseUrl { get; set; }

        [BsonElement("points_earned")]
        public int PointsEarned { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
