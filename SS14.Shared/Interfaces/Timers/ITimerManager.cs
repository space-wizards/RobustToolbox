using SS14.Shared.Timers;

namespace SS14.Shared.Interfaces.Timers
{
    public interface ITimerManager
    {
        void AddTimer(Timer timer);

        void UpdateTimers(float frameTime);
    }
}
