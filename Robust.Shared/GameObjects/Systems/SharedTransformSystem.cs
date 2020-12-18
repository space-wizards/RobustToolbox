using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Map;

namespace Robust.Shared.GameObjects.Systems
{
    internal class SharedTransformSystem : EntitySystem
    {
        private readonly List<MoveEvent> _deferredMoveEvents = new();

        public void DeferMoveEvent(MoveEvent moveEvent)
        {
            _deferredMoveEvents.Add(moveEvent);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var ev in _deferredMoveEvents.OrderBy(e => e.Sender.HasComponent<IMapGridComponent>()))
            {
                ev.Sender.EntityManager.EventBus.RaiseEvent(EventSource.Local, ev);
                ev.Handled = true;
            }

            _deferredMoveEvents.RemoveAll(e => e.Handled);
        }
    }
}
