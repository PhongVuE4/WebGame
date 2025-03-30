using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Player
{
    public class PlayerIdCannotNull : Exception
    {
        public PlayerIdCannotNull( ) : base($"Player id cannot be null or empty") { }
    }
}
