using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Player
{
    public class PlayerIdNotFound : Exception
    {
        public PlayerIdNotFound(string playerId) : base($"Player with id '{playerId}' not found.") { }
    }
}
