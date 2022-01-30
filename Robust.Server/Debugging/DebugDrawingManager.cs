using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;
using Robust.Shared.Physics;

namespace Robust.Server.Debugging
{
    [UsedImplicitly]
    internal sealed class DebugDrawingManager : IDebugDrawingManager, IEntityEventSubscriber
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public void Initialize()
        {
#if DEBUG
            _entManager.EventBus.SubscribeEvent<DebugDrawRayMessage>(EventSource.Local, this, PhysicsOnDebugDrawRay);
#endif
        }

        private void PhysicsOnDebugDrawRay(DebugDrawRayMessage @event)
        {
            var data = @event.Data;
            var msg = new MsgRay {RayOrigin = data.Ray.Position};
            if (data.Results != null)
            {
                msg.DidHit = true;
                msg.RayHit = data.Results.Value.HitPos;
            }
            else
            {
                msg.RayHit = data.Ray.Position + data.Ray.Direction * data.MaxLength;
            }

            _entManager.EventBus.RaiseEvent(EventSource.Network, msg);
        }
    }
}
