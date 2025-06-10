using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Room
{
    public class RoomModeException : Exception
    {
        public RoomModeException() : base($"Mode must be 'Friends', 'Couples', or 'Party'.") { }
    }
}
