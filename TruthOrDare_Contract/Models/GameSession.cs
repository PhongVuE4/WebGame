using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.DTOs.GameSession;

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
        [BsonElement("mode")]
        public string Mode { get; set; }
        [BsonElement("age_group")]
        public string AgeGroup { get; set; }
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
        [BsonElement("player_id")]
        public string PlayerId { get; set; }
        [BsonElement("player_name")]
        public string PlayerName { get; set; }
        [BsonElement("questions")]
        public List<QuestionDetail> Questions { get; set; } = new();
        [BsonElement("status")]
        public string Status { get; set; }
        [BsonElement("response_type")]
        [BsonIgnoreIfNull]
        public string ResponseType { get; set; }
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
    public class QuestionDetail
    {
        [BsonElement("question_id")]
        public string QuestionId { get; set; }
        [BsonElement("question_content")]
        public string QuestionContent { get; set; }
    }
}
