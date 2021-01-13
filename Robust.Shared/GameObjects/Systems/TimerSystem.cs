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
            foreach (var timer in ComponentManager.EntityQuery<TimerComponent>(false).ToList())
            {
                timer.Update(frameTime);
            }
        }
    }
}
