using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Room
{
    public class PlayerNotActiveException : Exception
    {
        public PlayerNotActiveException() : base($"Please join by JoinRoom")
        {
        }
    }
}
