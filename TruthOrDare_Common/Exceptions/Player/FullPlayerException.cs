using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Player
{
    public class FullPlayerException : Exception
    {
        public FullPlayerException(int maxPlayer) : base($"Room is full with '{maxPlayer}' player.") { }
    }
}
