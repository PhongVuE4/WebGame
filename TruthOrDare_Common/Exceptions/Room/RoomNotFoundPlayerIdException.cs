using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Room
{
    public class RoomNotFoundPlayerIdException : Exception
    {
        public RoomNotFoundPlayerIdException() : base($"Room not found for this player") { }
    }
}
