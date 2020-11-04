using Robust.Shared.GameObjects.Components.Timers;

namespace Robust.Shared.GameObjects.Systems
{
    public class TimerSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var timer in ComponentManager.EntityQuery<TimerComponent>())
            {
                timer.Update(frameTime);
            }
        }
    }
}
