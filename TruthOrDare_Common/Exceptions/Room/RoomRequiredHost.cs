using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Room
{
    public class RoomRequiredHost : Exception
    {
        public RoomRequiredHost() : base($"Only the host can start the game.") { }
    }
}
