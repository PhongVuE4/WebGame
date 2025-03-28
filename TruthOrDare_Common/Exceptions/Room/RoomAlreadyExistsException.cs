using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.Room
{
    public class RoomAlreadyExistsException : Exception
    {
        public RoomAlreadyExistsException(string roomName)
            : base($"Room with name '{roomName}' already exists.")
        {
        }
    }
}
