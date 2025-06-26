using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Room
{
    public class RoomOrPlayIdNotFoundException : Exception
    {
        public RoomOrPlayIdNotFoundException() : base("RoomId or PlayerId not found.")
        {
        }
    }
}
