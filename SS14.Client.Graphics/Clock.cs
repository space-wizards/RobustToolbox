using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SClock = SFML.System.Clock;

namespace SS14.Client.Graphics
{
    /// <summary>
    ///     Wrapper for SFML's Clock.
    /// </summary>
    public class Clock : IDisposable
    {
        private SClock clock;

        public Clock()
        {
            clock = new SClock();
        }

        public void Dispose()
        {
            clock.Dispose();
        }

        public float ElapsedTimeAsSeconds()
        {
            return clock.ElapsedTime.AsSeconds();
        }

        public void Restart()
        {
            clock.Restart();
        }
    }
}
