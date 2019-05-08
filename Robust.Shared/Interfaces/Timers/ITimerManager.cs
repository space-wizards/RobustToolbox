using System.Threading;
using Timer = Robust.Shared.Timers.Timer;

namespace Robust.Shared.Interfaces.Timers
{
    public interface ITimerManager
    {
        void AddTimer(Timer timer, CancellationToken cancellationToken = default);

        void UpdateTimers(float frameTime);
    }
}
