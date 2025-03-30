using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Player
{
    public class PlayerNameExisted : Exception
    {
        public PlayerNameExisted(string playerName) : base($"Player name '{playerName}' existed.") { }
    }
}
