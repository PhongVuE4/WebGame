using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TruthOrDare_Contract.DTOs.Question;
using TruthOrDare_Contract.IRepository;
using TruthOrDare_Contract.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using TruthOrDare_Common.Exceptions.Question;
using TruthOrDare_Common.Exceptions;

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
        public async Task<List<Question>> GetQuestions(string? filters)
        {
            var baseFilter = Builders<Question>.Filter.Eq(q => q.IsDeleted, false);

            if (string.IsNullOrWhiteSpace(filters))
            {
                return await _questions.Find(baseFilter).ToListAsync();
            }
            Dictionary<string, string> filterDict;
            try
            {
                filterDict = JsonSerializer.Deserialize<Dictionary<string, string>>(filters);
            }
            catch (JsonException)
            {
                // Nếu JSON không hợp lệ, trả về danh sách rỗng hoặc xử lý lỗi tùy ý
                throw new InvalidFiltersException();
            }


            var conditions = new List<FilterDefinition<Question>>();

            if (filterDict.TryGetValue("mode", out var mode) && !string.IsNullOrWhiteSpace(mode))
            {
                var modeLower = mode.ToLower();
                if (modeLower != "friends" && modeLower != "couples" && modeLower != "party")
                {
                    throw new InvalidQuestionModeException();
                }
                conditions.Add(Builders<Question>.Filter.Eq(q => q.Mode, mode));
            }

            if (filterDict.TryGetValue("type", out var type) && !string.IsNullOrWhiteSpace(type))
            {
                var typeLower = type.ToLower();
                if (typeLower != "truth" && typeLower != "dare")
                {
                    throw new InvalidQuestionTypeException();
                }
                conditions.Add(Builders<Question>.Filter.Eq(q => q.Type, type));
            }

            if (filterDict.TryGetValue("difficulty", out var difficulty) && !string.IsNullOrWhiteSpace(difficulty))
            {
                var difficultyLower = difficulty.ToLower();
                if (difficultyLower != "easy" && difficultyLower != "medium" && difficultyLower != "hard")
                {
                    throw new InvalidQuestionDifficultyException();
                }
                conditions.Add(Builders<Question>.Filter.Eq(q => q.Difficulty, difficulty));
            }

            if (filterDict.TryGetValue("age_group", out var ageGroup) && !string.IsNullOrWhiteSpace(ageGroup))
            {
                var agegroupLower = ageGroup.ToLower();
                if (agegroupLower != "kid" && agegroupLower != "teen" && agegroupLower != "adult" && agegroupLower != "all")
                {
                    throw new InvalidQuestionAgeGroupException();
                }
                conditions.Add(Builders<Question>.Filter.Eq(q => q.AgeGroup, ageGroup));
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

            return await _questions.Find(filterBuilder).ToListAsync();
        }
        public async Task<int> GetPointsForQuestionAsync(string questionId)
        {
            var question = await _questions.Find(q => q.Id == questionId).FirstOrDefaultAsync();
            if (question == null)
            {
                throw new QuestionNotFoundException(questionId);
            }
            return question.Points;
        }
        public async Task<Question> CreateQuestion(QuestionCreateDTO question)
        {
            if (question == null
                || string.IsNullOrWhiteSpace(question.Text)
                || string.IsNullOrWhiteSpace(question.Type)
                || string.IsNullOrWhiteSpace(question.Mode)
                || string.IsNullOrWhiteSpace(question.Difficulty))
            {
                throw new QuestionFieldsRequiredException();
            }
            if (!string.IsNullOrWhiteSpace(question.Mode))
            {
                var modeLower = question.Mode.ToLower();
                if (modeLower != "friends" && modeLower != "couples" && modeLower != "party")
                {
                    throw new InvalidQuestionModeException();
                }
            }
            if (!string.IsNullOrWhiteSpace(question.Type))
            {
                var typeLower = question.Type.ToLower();
                if (typeLower != "truth" && typeLower != "dare")
                {
                    throw new InvalidQuestionTypeException();
                }
            }
            if (!string.IsNullOrWhiteSpace(question.Difficulty))
            {
                var difficultyLower = question.Difficulty.ToLower();
                if (difficultyLower != "easy" && difficultyLower != "medium" && difficultyLower != "hard")
                {
                    throw new InvalidQuestionDifficultyException();
                }
            }
            var checkq = await _questions
                .Find(a => a.Text.ToLower() == question.Text.ToLower())
                .FirstOrDefaultAsync();
            if (checkq != null)
            {
                throw new QuestionAlreadyExistsException(checkq.Text);
            }
            var newQuestion = new Question
            {
                Mode = question.Mode,
                Type = question.Type,
                Text = question.Text,
                Difficulty = question.Difficulty,
                AgeGroup = string.IsNullOrWhiteSpace(question.AgeGroup) ? "all" : question.AgeGroup,
                TimeLimit = question.TimeLimit > 0 ? question.TimeLimit : 0,
                ResponseType = string.IsNullOrWhiteSpace(question.ResponseType) ? "none" : question.ResponseType,
                Points = question.Points > 0 ? question.Points : 10,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Visibility = string.IsNullOrWhiteSpace(question.Visibility) ? "public" : question.Visibility,
                Tags = question.Tags ?? new List<string> { "all" },
                CreatedBy = "Admin",
            };
            await _questions.InsertOneAsync(newQuestion);
            return newQuestion;
        }
        public async Task<int> InsertManyQuestions(List<QuestionCreateDTO> questions)
        {
            if (questions == null || !questions.Any())
            {
                throw new EmptyQuestionListException();
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
                var errorMessages = new List<string>();

                if (question == null || string.IsNullOrWhiteSpace(question.Text)
                    || string.IsNullOrWhiteSpace(question.Type)
                    || string.IsNullOrWhiteSpace(question.Mode)
                    || string.IsNullOrWhiteSpace(question.Difficulty))
                {
                    errorMessages.Add($"Question at index {i}: {new QuestionFieldsRequiredException().Message}");
                }

                if (!string.IsNullOrWhiteSpace(question.Type))
                {
                    var typeLower = question.Type.ToLower();
                    if (typeLower != "truth" && typeLower != "dare")
                    {
                        errorMessages.Add($"Question at index {i}: {new InvalidQuestionTypeException().Message}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(question.Mode))
                {
                    var modeLower = question.Mode.ToLower();
                    if (modeLower != "friends" && modeLower != "couples" && modeLower != "party")
                    {
                        errorMessages.Add($"Question at index {i}: {new InvalidQuestionModeException().Message}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(question.Difficulty))
                {
                    var difficultyLower = question.Difficulty.ToLower();
                    if (difficultyLower != "easy" && difficultyLower != "medium" && difficultyLower != "hard")
                    {
                        errorMessages.Add($"Question at index {i}: {new InvalidQuestionDifficultyException().Message}");
                    }
                }
                if (!string.IsNullOrWhiteSpace(question.AgeGroup))
                {
                    var agegroupLower = question.AgeGroup.ToLower();
                    if (agegroupLower != "kid" && agegroupLower != "teen" && agegroupLower != "adult" && agegroupLower != "all")
                    {
                        errorMessages.Add($"Question at index {i}: {new InvalidQuestionAgeGroupException().Message}");
                    }
                }

                if (question.TimeLimit < 0)
                {
                    errorMessages.Add($"Question at index {i}: {new InvalidTimeLimitException().Message}");

                }

                if (question.Points < 0)
                {
                    errorMessages.Add($"Question at index {i}: {new InvalidPointsException().Message}");
                }

                if (question != null && !string.IsNullOrWhiteSpace(question.Text) && existingTexts.Contains(question.Text.ToLower()))
                {
                    errorMessages.Add($"Question at index {i}: {new QuestionAlreadyExistsException(question.Text).Message}");
                }
                // Nếu không có lỗi, thêm câu hỏi vào danh sách để insert
                if (errorMessages.Any())
                {
                    errors.AddRange(errorMessages);
                }
                else
                {
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
                        Visibility = string.IsNullOrWhiteSpace(question.Visibility) ? "public" : question.Visibility,
                        Tags = question.Tags ?? new List<string> { "all" },
                        CreatedBy = "Admin",
                    };

                    questionsToInsert.Add(newQuestion);
                    existingTexts.Add(question.Text.ToLower()); // Thêm vào HashSet để tránh trùng lặp trong cùng batch
                }
                
            }

            // Nếu có lỗi, ném ngoại lệ chứa tất cả lỗi
            if (errors.Any())
            {
                throw new MultipleValidationException(errors);
            }

            // Nếu không có lỗi, insert các câu hỏi hợp lệ
            if (questionsToInsert.Any())
            {
                await _questions.InsertManyAsync(questionsToInsert);
            }

            return questionsToInsert.Count;
        }
        public async Task DeleteQuestion(string questionId)
        {
            if (string.IsNullOrWhiteSpace(questionId))
            {
                throw new ArgumentException("Question ID cannot be null or empty.", nameof(questionId));
            }

            // Kiểm tra định dạng ObjectId
            if (!MongoDB.Bson.ObjectId.TryParse(questionId, out _))
            {
                throw new ArgumentException("Invalid Question ID format. ID must be a valid ObjectId.", nameof(questionId));
            }

            var question = await _questions
                .Find(Builders<Question>.Filter.And(
                    Builders<Question>.Filter.Eq(a => a.Id, questionId),
                    Builders<Question>.Filter.Eq(a => a.IsDeleted, false)))
                .SingleOrDefaultAsync();

            if (question == null)
            {
                throw new QuestionNotFoundException(questionId);
            }

            var update = Builders<Question>.Update
                .Set(a => a.IsDeleted, true)
                .Set(a => a.UpdatedAt, DateTime.Now);

            await _questions.UpdateOneAsync(Builders<Question>.Filter.Eq(a => a.Id, questionId), update);
        }
    }
}
