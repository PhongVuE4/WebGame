using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Room
{
    public class RoomNeedMoreTimeException : Exception
    {
        public RoomNeedMoreTimeException() : base("Please await 1 seconds before moving to the next player!")
        {
        }
    }
}
