using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Player
{
    public class PlayerIdAlreadyInUseException : Exception
    {
        public PlayerIdAlreadyInUseException(string playerId)
            : base($"Player ID '{playerId}' is already in use by another playerName in the room.")
        {
        }
    }
}
