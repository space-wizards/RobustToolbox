using System.Threading;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timers.Timer;

namespace Robust.Shared.Timers
{
    public interface ITimerManager
    {
        void AddTimer(Timer timer, CancellationToken cancellationToken = default);

        void UpdateTimers(FrameEventArgs frameEventArgs);
    }
}
