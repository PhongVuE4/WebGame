using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Question
{
    // Lỗi validation: Thiếu trường bắt buộc
    public class QuestionFieldsRequiredException : Exception
    {
        public QuestionFieldsRequiredException()
            : base("Question text, type, mode, and difficulty are required.")
        {
        }
    }

    // Lỗi validation: Mode không hợp lệ
    public class InvalidQuestionModeException : Exception
    {
        public InvalidQuestionModeException()
            : base("Mode must be 'Friends', 'Couples', or 'Party'.")
        {
        }
    }

    // Lỗi validation: Type không hợp lệ
    public class InvalidQuestionTypeException : Exception
    {
        public InvalidQuestionTypeException()
            : base("Type must be 'Truth' or 'Dare'.")
        {
        }
    }

    // Lỗi validation: Difficulty không hợp lệ
    public class InvalidQuestionDifficultyException : Exception
    {
        public InvalidQuestionDifficultyException()
            : base("Difficulty must be 'Easy', 'Medium', or 'Hard'.")
        {
        }
    }

    // Lỗi validation: AgeGroup không hợp lệ
    public class InvalidQuestionAgeGroupException : Exception
    {
        public InvalidQuestionAgeGroupException()
            : base("AgeGroup must be 'Kid', 'Teen', 'Adult', or 'All'.")
        {
        }
    }

    // Lỗi validation: TimeLimit âm
    public class InvalidTimeLimitException : Exception
    {
        public InvalidTimeLimitException()
            : base("TimeLimit must be non-negative.")
        {
        }
    }

    // Lỗi validation: Points âm
    public class InvalidPointsException : Exception
    {
        public InvalidPointsException()
            : base("Points must be non-negative.")
        {
        }
    }

    // Lỗi logic nghiệp vụ: Câu hỏi đã tồn tại
    public class QuestionAlreadyExistsException : Exception
    {
        public QuestionAlreadyExistsException(string text)
            : base($"Question with text '{text}' already exists.")
        {
        }
    }

    // Lỗi logic nghiệp vụ: Câu hỏi không tìm thấy
    public class QuestionNotFoundException : Exception
    {
        public QuestionNotFoundException(string questionId)
            : base($"Question with ID '{questionId}' does not exist or has been deleted.")
        {
        }
    }

    // Lỗi logic nghiệp vụ: Danh sách câu hỏi rỗng
    public class EmptyQuestionListException : Exception
    {
        public EmptyQuestionListException()
            : base("The list of questions to insert is empty or null.")
        {
        }
    }

    // Lỗi validation: JSON filters không hợp lệ
    public class InvalidFiltersException : Exception
    {
        public InvalidFiltersException()
            : base("Invalid JSON format for filters.")
        {
        }
    }
}
