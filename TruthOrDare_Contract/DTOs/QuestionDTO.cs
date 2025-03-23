using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.DTOs
{
    public class QuestionDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("game_type")]
        public string GameType { get; set; } = "truth_or_dare";

        [BsonElement("mode")]
        public string Mode { get; set; }

        [BsonElement("type")]
        public string Type { get; set; }

        [BsonElement("text")]
        public string Text { get; set; }

        [BsonElement("difficulty")]
        public string Difficulty { get; set; }

        [BsonElement("age_group")]
        public string AgeGroup { get; set; }

        [BsonElement("time_limit")]
        public int TimeLimit { get; set; }

        [BsonElement("response_type")]
        public string ResponseType { get; set; }

        [BsonElement("points")]
        public int Points { get; set; }

        [BsonElement("created_by")]
        [BsonIgnoreIfNull]
        public string CreatedBy { get; set; }

        [BsonElement("is_custom")]
        public bool IsCustom { get; set; } = true;

        [BsonElement("visibility")]
        public string Visibility { get; set; } = "public";

        [BsonElement("tags")]
        [BsonIgnoreIfNull]
        public List<string> Tags { get; set; }
    }
}
