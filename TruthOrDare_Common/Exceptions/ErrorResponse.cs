using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions
{
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public int ErrorCode { get; set; }
        public string Message { get; set; }
        public IDictionary<string, string[]> Errors { get; set; }

        public ErrorResponse(int statusCode, int errorCode, string message, IDictionary<string, string[]> errors = null)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            Message = message;
            Errors = errors;
        }
    }
}
