using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common
{
    public static class NameGenerator
    {
        private static readonly string[] Adjectives = { "Happy", "Brave", "Clever", "Funny", "Swift" };
        private static readonly string[] Nouns = { "Fox", "Bear", "Wolf", "Eagle", "Tiger" };
        private static readonly Random Random = new Random();

        public static string GenerateRandomName()
        {
            var adjective = Adjectives[Random.Next(Adjectives.Length)];
            var noun = Nouns[Random.Next(Nouns.Length)];
            var number = Random.Next(100, 1000); // Số ngẫu nhiên từ 100 đến 999
            return $"{adjective}{noun}{number}";
        }
    }
}
