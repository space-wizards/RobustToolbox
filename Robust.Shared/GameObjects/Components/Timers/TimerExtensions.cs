using System.Threading;
using Robust.Shared.Interfaces.GameObjects;
using Timer = Robust.Shared.Timers.Timer;

namespace Robust.Shared.GameObjects.Components.Timers
{
    public static class TimerExtensions
    {
        public static void AddTimer(this IEntity entity, Timer timer, CancellationTokenSource? cancellationToken = null)
        {
            var component = entity.EnsureComponent<TimerComponent>();
            
            component.AddTimer(timer, cancellationToken);
        }
    }
}