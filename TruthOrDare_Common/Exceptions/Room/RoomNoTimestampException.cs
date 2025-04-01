using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Room
{
    public class RoomNoTimestampException : Exception
    {
        public RoomNoTimestampException() : base("No timestamp available.")
        {
        }
    }
}
