using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.DTOs.Question;
using TruthOrDare_Contract.Models;

namespace TruthOrDare_Contract.IRepository
{
    public interface IQuestionRepository
    {
        Task<Question> GetRandomQuestionAsync(string questionType, string ageGroup, List<string> excludeIds);
        Task<int> GetPointsForQuestionAsync(string questionId);
        Task<string> CreateQuestion(QuestionCreateDTO question);
        Task<List<Question>> GetQuestions(string? filter);
        Task<(int SuccessCount, List<string> Errors)> InsertManyQuestions(List<QuestionCreateDTO> questions);
        Task<string> DeleteQuestion(string questionId);
    }
}
