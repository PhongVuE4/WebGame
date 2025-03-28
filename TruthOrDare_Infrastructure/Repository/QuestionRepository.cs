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

namespace TruthOrDare_Infrastructure.Repository
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly IMongoCollection<Question> _questions;

        public QuestionRepository(MongoDbContext context)
        {
            _questions = context.Questions;
        }

        public async Task<Question> GetRandomQuestionAsync(string questionType, string ageGroup, List<string> excludeIds)
        {
            var filter = Builders<Question>.Filter.And(
                Builders<Question>.Filter.Eq(a => a.Type, questionType),
                Builders<Question>.Filter.Eq(a => a.AgeGroup, ageGroup),
                Builders<Question>.Filter.Not(Builders<Question>.Filter.In(q => q.Id, excludeIds)),
                Builders<Question>.Filter.Eq(a => a.IsDeleted, false));
            var question = await _questions.Find(filter).Limit(1).FirstOrDefaultAsync();

            if (question != null)
            {
                excludeIds.Add(question.Id); // Thêm ID để tránh trùng lặp
            }

            return question;
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
                return new List<Question>();
            }


            var conditions = new List<FilterDefinition<Question>>();

            if (filterDict.TryGetValue("mode", out var mode) && !string.IsNullOrWhiteSpace(mode))
            {
                conditions.Add(Builders<Question>.Filter.Eq(q => q.Mode, mode));
            }

            if (filterDict.TryGetValue("type", out var type) && !string.IsNullOrWhiteSpace(type))
            {
                conditions.Add(Builders<Question>.Filter.Eq(q => q.Type, type));
            }

            if (filterDict.TryGetValue("difficulty", out var difficulty) && !string.IsNullOrWhiteSpace(difficulty))
            {
                conditions.Add(Builders<Question>.Filter.Eq(q => q.Difficulty, difficulty));
            }

            if (filterDict.TryGetValue("age_group", out var ageGroup) && !string.IsNullOrWhiteSpace(ageGroup))
            {
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
                if (question == null 
                    || string.IsNullOrWhiteSpace(question.Text) 
                    || string.IsNullOrWhiteSpace(question.Type)
                    || string.IsNullOrWhiteSpace(question.Mode)
                    || string.IsNullOrWhiteSpace(question.Difficulty))
                {
                    return "Question text, type, mode and difficulty are required";
                }
                if (!string.IsNullOrWhiteSpace(question.Mode))
                {
                    var modeLower = question.Mode.ToLower();
                    if (modeLower != "friends" && modeLower != "couples" && modeLower != "party")
                    {
                        return "Mode must be 'Friends', 'Couples' or 'Party'.";
                    }
                }
                if (!string.IsNullOrWhiteSpace(question.Type))
                {
                    var typeLower = question.Type.ToLower();
                    if (typeLower != "truth" && typeLower != "dare")
                    {
                        return "Type must be 'Truth' or 'Dare'.";
                    }
                }
                if (!string.IsNullOrWhiteSpace(question.Difficulty))
                {
                    var difficultyLower = question.Difficulty.ToLower();
                    if (difficultyLower != "easy" && difficultyLower!= "medium" && difficultyLower!= "hard")
                    {
                        return "Difficulty must be 'Easy', 'Medium' or 'Hard'.";
                    }
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

                    if (!string.IsNullOrWhiteSpace(question.Mode))
                    {
                        var modeLower = question.Mode.ToLower();
                        if(modeLower != "friends" && modeLower != "couples" && modeLower != "party")
                        {
                            errors.Add($"Question at index {i}: mode must be 'Friends' , 'Couples' or 'Party'.");
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
                        Visibility = string.IsNullOrWhiteSpace(question.Visibility) ? "public" : question.Visibility,
                        Tags = question.Tags ?? new List<string> { "all" },
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
        public async Task<string> DeleteQuestion(string questionId)
        {
            try
            {
                var question = await _questions
                    .Find(Builders<Question>.Filter.And(
                        Builders<Question>.Filter.Eq(a => a.Id, questionId),
                        Builders<Question>.Filter.Eq(a => a.IsDeleted, false))).SingleOrDefaultAsync();
                if (question != null)
                {
                    var update = Builders<Question>.Update.Set(a => a.IsDeleted, true).Set(a => a.UpdatedAt, DateTime.Now);
                    await _questions.UpdateOneAsync(Builders<Question>.Filter.Eq(a => a.Id, questionId), update);
                    return "Question deleted successfully.";
                }
                return "Question not found.";
            }
            catch (Exception ex)
            {
                return $"Failed to delete question: {ex.Message}";
            }
            
        }
    }
}
