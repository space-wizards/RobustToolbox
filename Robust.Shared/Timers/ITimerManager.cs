using System.Threading;
using Robust.Shared.Timing;

namespace Robust.Shared.Timers
{
    public interface ITimerManager
    {
        void AddTimer(Timer timer, CancellationToken cancellationToken = default);

        void UpdateTimers(FrameEventArgs frameEventArgs);
    }
}
