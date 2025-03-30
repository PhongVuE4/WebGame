using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Room
{
    public class RoomPasswordRequired : Exception
    {
        public RoomPasswordRequired() : base($"Password is required") { }
    }

}
