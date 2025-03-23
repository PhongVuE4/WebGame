using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.DTOs;
using TruthOrDare_Contract.IRepository;
using TruthOrDare_Contract.Models;

namespace TruthOrDare_Infrastructure.Repository
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly IMongoCollection<Question> _questions;

        public QuestionRepository(MongoDbContext context)
        {
            _questions = context.Questions;
        }

        public async Task<Question> GetRandomQuestionAsync(List<string> excludeIds)
        {
            var filter = Builders<Question>.Filter.Not(Builders<Question>.Filter.In(q => q.Id, excludeIds));
            return await _questions.Find(filter).Limit(1).FirstOrDefaultAsync();
        }
        public async Task<List<Question>> GetQuestions(string? mode, string? type, string? difficulty, string? age_group)
        {
            var baseFilter = Builders<Question>.Filter.Eq(q => q.IsDeleted, false);

            if (string.IsNullOrWhiteSpace(mode) &&
                string.IsNullOrWhiteSpace(type) &&
                string.IsNullOrWhiteSpace(difficulty) &&
                string.IsNullOrWhiteSpace(age_group))
            {
                return await _questions.Find(baseFilter).ToListAsync();
            }

            var conditions = new List<FilterDefinition<Question>>();

            if (!string.IsNullOrWhiteSpace(mode))
            {
                conditions.Add(Builders<Question>.Filter.Eq(q => q.Mode, mode));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                conditions.Add(Builders<Question>.Filter.Eq(q => q.Type, type));
            }

            if (!string.IsNullOrWhiteSpace(difficulty))
            {
                conditions.Add(Builders<Question>.Filter.Eq(q => q.Difficulty, difficulty));
            }

            if (!string.IsNullOrWhiteSpace(age_group))
            {
                conditions.Add(Builders<Question>.Filter.Eq(q => q.AgeGroup, age_group));
            }

            FilterDefinition<Question> filterBuilder;
            if (conditions.Any())
            {
                var orFilter = Builders<Question>.Filter.Or(conditions);
                filterBuilder = Builders<Question>.Filter.And(baseFilter, orFilter);
            }
            else
            {
                filterBuilder = baseFilter;
            }

            var questions = await _questions.Find(filterBuilder).ToListAsync();

            return questions;
        }
        public async Task<int> GetPointsForQuestionAsync(string questionId)
        {
            var question = await _questions.Find(q => q.Id == questionId).FirstOrDefaultAsync();
            return question?.Points ?? 0;
        }
        public async Task<string> CreateQuestion(QuestionCreateDTO question)
        {
            try
            {
                if (question == null || string.IsNullOrWhiteSpace(question.Text) || string.IsNullOrWhiteSpace(question.Type))
                {
                    return "Question text and type are required";
                }
                var checkq = await _questions
                    .Find(a => a.Text.ToLower() == question.Text.ToLower())
                    .FirstOrDefaultAsync();
                if (checkq != null)
                {
                    return "Question text already exist";
                }
                var newQuestion = new Question
                {
                    Mode = question.Mode,
                    Type = question.Type,
                    Text = question.Text,
                    Difficulty = question.Difficulty,
                    AgeGroup = question.AgeGroup,
                    TimeLimit = question.TimeLimit,
                    ResponseType = question.ResponseType,
                    Points = question.Points > 0 ? question.Points : 10,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Visibility = question.Visibility,
                    Tags = question.Tags,
                    CreatedBy = "Admin",
                };
                await _questions.InsertOneAsync(newQuestion);
                return "Success";
            }
            catch (Exception ex)
            {
                return $"Failed: {ex.Message}";
            }
        }
    }
}
