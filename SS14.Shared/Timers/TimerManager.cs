using SS14.Shared.Interfaces.Timers;
using System.Collections.Generic;

namespace SS14.Shared.Timers
{
    public class TimerManager : ITimerManager
    {
        private List<Timer> _timers = new List<Timer>();

        public void AddTimer(Timer timer)
        {
            _timers.Add(timer);
        }

        public void UpdateTimers(float frameTime)
        {
            new List<Timer>(_timers).ForEach(timer => timer.Update(frameTime));
            _timers.RemoveAll(timer => !timer.IsActive);
        }
    }
}
