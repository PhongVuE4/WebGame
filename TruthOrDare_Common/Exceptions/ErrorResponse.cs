using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TruthOrDare_Common.Exceptions
{
    public class ErrorResponse
    {
        public int statusCode { get; set; }
        public ErrorDetails errors { get; set; }

        public ErrorResponse(int statusCode, int errorCode, string message = null)
        {
            this.statusCode = statusCode;
            this.errors = new ErrorDetails
            {
                errorCode = errorCode,
                message = message
            };
        }

        public class ErrorDetails
        {
            public int errorCode { get; set; }
            public string message { get; set; }
        }
    }
}
