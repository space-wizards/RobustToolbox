using Robust.Shared.Timers;

namespace Robust.Shared.Interfaces.Timers
{
    public interface ITimerManager
    {
        void AddTimer(Timer timer);

        void UpdateTimers(float frameTime);
    }
}
