using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public async Task<int> GetPointsForQuestionAsync(string questionId)
        {
            var question = await _questions.Find(q => q.Id == questionId).FirstOrDefaultAsync();
            return question?.Points ?? 0;
        }
    }
}
