using System.Threading;

namespace Robust.Shared.Timing
{
    public interface ITimerManager
    {
        void AddTimer(Timer timer, CancellationToken cancellationToken = default);

        void UpdateTimers(FrameEventArgs frameEventArgs);
    }
}
