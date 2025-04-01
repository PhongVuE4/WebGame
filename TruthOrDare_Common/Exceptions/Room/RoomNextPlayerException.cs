using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Room
{
    public class RoomNextPlayerException : Exception
    {
        public RoomNextPlayerException() : base("No turn or question has started yet!")
        {
        }
    }
}
