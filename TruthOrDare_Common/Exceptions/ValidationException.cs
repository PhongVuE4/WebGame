using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions
{
    public class ValidationException : Exception
    {
        public int ErrorCode { get; }
        public IDictionary<string, string[]> Errors { get; }

        private const int DefaultValidationErrorCode = 9998; // Giá trị của ErrorCode.ValidationError

        public ValidationException(int errorCode, string message, IDictionary<string, string[]> errors)
            : base(message)
        {
            ErrorCode = errorCode;
            Errors = errors;
        }

        public ValidationException(IDictionary<string, string[]> errors)
            : this(DefaultValidationErrorCode, "One or more validation errors occurred.", errors)
        {
        }

        public static ValidationException ForProperty(string propertyName, string errorMessage, int errorCode = DefaultValidationErrorCode)
        {
            var errors = new Dictionary<string, string[]>
            {
                { propertyName, new[] { errorMessage } }
            };
            return new ValidationException(errorCode, $"Validation error for {propertyName}: {errorMessage}", errors);
        }
    }
}
