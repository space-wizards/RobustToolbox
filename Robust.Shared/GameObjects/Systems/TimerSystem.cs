using System.Linq;
using Robust.Shared.Collections;
using Robust.Shared.IoC;

namespace Robust.Shared.GameObjects
{
    public sealed class TimerSystem : EntitySystem
    {
#pragma warning disable CS0618
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Avoid a collection was modified while enumerating.
            var timers = EntityManager.EntityQueryEnumerator<TimerComponent>();
            var timersList = new ValueList<(EntityUid, TimerComponent)>();
            while (timers.MoveNext(out var uid, out var timer))
            {
                timersList.Add((uid, timer));
            }

            foreach (var (_, timer) in timersList)
            {
                timer.Update(frameTime);
            }

            foreach (var (uid, timer) in timersList)
            {
                if (!timer.Deleted && !EntityManager.Deleted(uid) && timer.RemoveOnEmpty && timer.TimerCount == 0)
                {
                    EntityManager.RemoveComponent<TimerComponent>(uid);
                }
            }
        }
#pragma warning restore CS0618
    }
}
