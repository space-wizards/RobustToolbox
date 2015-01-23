using SS14.Shared.Utility;

namespace SS14.Server.Timing
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

