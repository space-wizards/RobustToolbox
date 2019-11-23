using System.Diagnostics;
using Robust.Server.Interfaces.Debugging;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;

namespace Robust.Server.Debugging
{
    internal class DebugDrawingManager : IDebugDrawingManager
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNetManager _net;
        [Dependency] private readonly IPhysicsManager _physics;
#pragma warning restore 649

        public void Initialize()
        {
            _net.RegisterNetMessage<MsgRay>(MsgRay.NAME);
            _physics.DebugDrawRay += data => PhysicsOnDebugDrawRay(data);
        }

        [Conditional("DEBUG")]
        private void PhysicsOnDebugDrawRay(DebugRayData data)
        {
            var msg = _net.CreateNetMessage<MsgRay>();
            msg.RayOrigin = data.Ray.Position;
            if (data.Results.DidHitObject)
            {
                msg.DidHit = true;
                msg.RayHit = data.Results.HitPos;
            }
            else
            {
                msg.RayHit = data.Ray.Position + data.Ray.Direction * data.MaxLength;
            }

            _net.ServerSendToAll(msg);
        }
    }
}
