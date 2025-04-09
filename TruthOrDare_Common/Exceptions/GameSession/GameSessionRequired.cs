using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.GameSession
{
    public class GameSessionRequired : Exception
    {
        public GameSessionRequired()
            : base("Game session Id is required.")
        {
        }
    }
}
