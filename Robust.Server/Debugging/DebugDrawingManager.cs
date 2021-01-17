using System.Diagnostics;
using Robust.Server.Interfaces.Debugging;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;
using Robust.Shared.Physics.Broadphase;

namespace Robust.Server.Debugging
{
    internal class DebugDrawingManager : IDebugDrawingManager
    {
        [Dependency] private readonly IServerNetManager _net = default!;

        public void Initialize()
        {
            _net.RegisterNetMessage<MsgRay>(MsgRay.NAME);
            EntitySystem.Get<SharedBroadPhaseSystem>().DebugDrawRay += data => PhysicsOnDebugDrawRay(data);
        }

        [Conditional("DEBUG")]
        private void PhysicsOnDebugDrawRay(DebugRayData data)
        {
            var msg = _net.CreateNetMessage<MsgRay>();
            msg.RayOrigin = data.Ray.Position;
            if (data.Results != null)
            {
                msg.DidHit = true;
                msg.RayHit = data.Results.Value.HitPos;
            }
            else
            {
                msg.RayHit = data.Ray.Position + data.Ray.Direction * data.MaxLength;
            }

            _net.ServerSendToAll(msg);
        }
    }
}
