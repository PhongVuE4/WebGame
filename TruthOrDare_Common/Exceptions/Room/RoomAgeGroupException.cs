using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Room
{
    public class RoomAgeGroupException : Exception
    {
        public RoomAgeGroupException() : base($"Age group must be 'Kids', 'Teen', 'Adult' and 'All'") { }
    }
}
