using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.DTOs;
using TruthOrDare_Contract.Models;

namespace TruthOrDare_Contract.IRepository
{
    public interface IQuestionRepository
    {
        Task<Question> GetRandomQuestionAsync(List<string> excludeIds);
        Task<int> GetPointsForQuestionAsync(string questionId);
        Task<string> CreateQuestion(QuestionCreateDTO question);
    }
}
