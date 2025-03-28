using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common
{
    public static class IdGenerator
    {
        private static readonly Random Random = new Random();
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string GenerateRoomId()
        {
            char[] roomId = new char[6];
            for (int i = 0; i < 6; i++)
            {
                roomId[i] = Chars[Random.Next(Chars.Length)];
            }
            return new string(roomId);
        }
    }
}
