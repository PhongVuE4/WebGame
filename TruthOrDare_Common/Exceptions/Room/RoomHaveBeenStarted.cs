using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Room
{
    public class RoomHaveBeenStarted : Exception
    {
        public RoomHaveBeenStarted() : base($"The room has already started. Players cannot join.")
        {
        }
    }
}
