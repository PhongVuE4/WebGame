using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Player
{
    public class InvalidPlayerNameException : Exception
    {
        public InvalidPlayerNameException(string playerName) : base($"Player with name '{playerName}' must not exceed 50 characters.") { }
    }
}
