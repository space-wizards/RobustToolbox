using System.Linq;
using Robust.Shared.GameObjects.Components.Timers;

namespace Robust.Shared.GameObjects.Systems
{
    public class TimerSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            // Avoid a collection was modified while enumerating.
            var timers = ComponentManager.EntityQuery<TimerComponent>(false).ToList();

            foreach (var timer in timers)
            {
                timer.Update(frameTime);
            }
        }
    }
}
