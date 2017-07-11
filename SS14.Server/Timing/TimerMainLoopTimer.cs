using System;
using System.Timers;

namespace SS14.Server.Timing
{
    public delegate void MainServerLoop();
    public interface IMainLoopTimer : IDisposable
    {
        Object CreateMainLoopTimer(MainServerLoop mainLoop, uint period);
    }
    public class TimerMainLoopTimer : IMainLoopTimer
    {
        private static Timer myTimer;
        public Object CreateMainLoopTimer(MainServerLoop mainLoop, uint period)
        {
            myTimer = new Timer(period);
            myTimer.Elapsed += (sender, e) => {mainLoop();};
            myTimer.Enabled = true;
            return myTimer;
        }

        public void Dispose()
        {
            myTimer.Dispose();
        }
    }
}

