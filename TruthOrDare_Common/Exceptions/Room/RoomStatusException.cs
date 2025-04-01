using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Room
{
    public class RoomStartStatusException : Exception
    {
        public RoomStartStatusException() : base($"Game can only start from 'Waiting' status.") { }
    }
    public class RoomEndStatusException : Exception
    {
        public RoomEndStatusException() : base($"Game can only end from 'Palying' status.") { }
    }
    public class RoomResetStatusException : Exception
    {
        public RoomResetStatusException() : base($"Game can only reset from 'Ended' status.") { }
    }
}
