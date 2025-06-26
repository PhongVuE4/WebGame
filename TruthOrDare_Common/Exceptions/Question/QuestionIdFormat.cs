using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Question
{
    public class QuestionIdFormat : Exception
    {
        public QuestionIdFormat(string message) : base(message)
        {
        }
        public QuestionIdFormat() : base("Question ID format is invalid.")
        {
        }
    }
}
