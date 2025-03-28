using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions
{
    public class MultipleValidationException : Exception
    {
        public List<string> Errors { get; }

        public MultipleValidationException(List<string> errors)
            : base("Multiple validation errors occurred.")
        {
            Errors = errors ?? new List<string>();
        }
    }
}
