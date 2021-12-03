using System.Linq;
using Robust.Shared.IoC;

namespace Robust.Shared.GameObjects
{
    public class TimerSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            // Avoid a collection was modified while enumerating.
            var timers = EntityManager.EntityQuery<TimerComponent>().ToList();

            foreach (var timer in timers)
            {
                timer.Update(frameTime);
            }

            foreach (var timer in timers)
            {
                if (!timer.Deleted && !((!IoCManager.Resolve<IEntityManager>().EntityExists(timer.Owner.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(timer.Owner.Uid).EntityLifeStage) >= EntityLifeStage.Deleted) && timer.RemoveOnEmpty && timer.TimerCount == 0)
                {
                    EntityManager.RemoveComponent<TimerComponent>(timer.Owner.Uid);
                }
            }
        }
    }
}
