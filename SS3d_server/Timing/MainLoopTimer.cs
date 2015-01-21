using System;
using SS13_Shared.Utility;

namespace SS13_Server.Timing
{
    public class MainLoopTimer
    {
        public IMainLoopTimer mainLoopTimer;
        public MainLoopTimer()
        {
            if(PlatformDetector.DetectPlatform() == Platform.Windows)
                mainLoopTimer = new TimerQueue();
            else
                mainLoopTimer = new TimerMainLoopTimer();
        }
    }
}

