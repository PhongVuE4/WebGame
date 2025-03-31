using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Player
{
    public class PlayerNameRequiredException : Exception
    {
        public PlayerNameRequiredException() : base("Player name is required.") { }
    }
}
