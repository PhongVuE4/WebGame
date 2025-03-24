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
                var orFilter = Builders<Question>.Filter.And(conditions);
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
        public async Task<(int SuccessCount, List<string> Errors)> InsertManyQuestions(List<QuestionCreateDTO> questions)
        {
            try
            {
                if(questions == null || !questions.Any())
                {
                    return (0, new List<string> {"Khong co cau hoi nao"});
                }
                var errors = new List<string>();
                var questionsToInsert = new List<Question>();
                var existingTexts = new HashSet<string>();

                var existingQuestions = await _questions
                    .Find(Builders<Question>.Filter.Eq(a => a.IsDeleted, false))
                    .Project(a => a.Text.ToLower()).ToListAsync();


                foreach (var text in existingQuestions)
                {
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var normalizedText = text.Trim().ToLower().Normalize(NormalizationForm.FormC);
                        existingTexts.Add(normalizedText);
                    }
                }

                for (int i = 0; i < questions.Count; i++)
                {
                    var question = questions[i];

                    if (question == null || string.IsNullOrWhiteSpace(question.Text) || string.IsNullOrWhiteSpace(question.Type))
                    {
                        errors.Add($"Question at index {i}: Text and Type are required.");
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(question.Type))
                    {
                        var typeLower = question.Type.ToLower();
                        if (typeLower != "truth" && typeLower != "dare")
                        {
                            errors.Add($"Question at index {i}: Type must be either 'Truth' or 'Dare'.");
                            continue;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(question.Difficulty))
                    {
                        var difficultyLower = question.Difficulty.ToLower();
                        if (difficultyLower != "easy" && difficultyLower != "medium" && difficultyLower != "hard")
                        {
                            errors.Add($"Question at index {i}: Difficulty must be 'Easy', 'Medium', or 'Hard'.");
                            continue;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(question.AgeGroup))
                    {
                        var agegroupLower = question.AgeGroup.ToLower();
                        if (agegroupLower != "kid" && agegroupLower != "teen" && agegroupLower != "adult" && agegroupLower != "all")
                        {
                            errors.Add($"Question at index {i}: AgeGroup must be 'Kid', 'Teen', 'Adult' or 'All'.");
                            continue;
                        }
                    }
                    
                    if (question.TimeLimit < 0)
                    {
                        errors.Add($"Question at index {i}: TimeLimit must be non-negative.");
                        continue;
                    }

                    if (question.Points < 0)
                    {
                        errors.Add($"Question at index {i}: Points must be non-negative.");
                        continue;
                    }

                    if (existingTexts.Contains(question.Text.ToLower()))
                    {
                        errors.Add($"Question at index {i}: Text '{question.Text}' already exists.");
                        continue;
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

                    questionsToInsert.Add(newQuestion);
                    existingTexts.Add(question.Text.ToLower()); // Thêm vào HashSet để tránh trùng lặp trong cùng batch
                }

                if (!questionsToInsert.Any())
                {
                    errors.Add("Khong co cau hoi nao duoc them.");
                    return (0, errors);
                }

                await _questions.InsertManyAsync(questionsToInsert);

                return (questionsToInsert.Count, errors);
            }
            catch (Exception ex)
            {
                return (0, new List<string> { $"Failed to insert questions: {ex.Message}" });
            }
        }
    }
}
