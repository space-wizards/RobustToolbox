using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;
using Robust.Shared.Physics;
using Robust.Shared.Debugging;

namespace Robust.Server.Debugging;

[UsedImplicitly]
internal sealed class DebugRayDrawingSystem : SharedDebugRayDrawingSystem
{
    protected override void ReceiveLocalRayAtMainThread(DebugRayData data)
    {
        // This code won't be called on release - eliminate it anyway for good measure.
#if DEBUG
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

        EntityManager.EventBus.RaiseEvent(EventSource.Network, msg);
#endif
    }
}

