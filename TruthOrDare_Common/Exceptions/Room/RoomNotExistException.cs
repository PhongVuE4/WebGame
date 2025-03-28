using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Room
{
    public class RoomNotExistException : Exception
    {
        public RoomNotExistException(string roomId) : base($"Room with id '{roomId}' not exists.") { }
    }
}
