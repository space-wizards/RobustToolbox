using JetBrains.Annotations;
using Robust.Shared.Network.Messages;
using Robust.Shared.Debugging;

namespace Robust.Server.Debugging;

[UsedImplicitly]
internal sealed class DebugRayDrawingSystem : SharedDebugRayDrawingSystem
{
#if DEBUG
    protected override void ReceiveLocalRayAtMainThread(DebugRayData data)
    {
        // This code won't be called on release - eliminate it anyway for good measure.
        var msg = new MsgRay {RayOrigin = data.Ray.Position, Map = data.Map};
        if (data.Results != null)
        {
            msg.DidHit = true;
            msg.RayHit = data.Results.Value.HitPos;
        }
        else
        {
            msg.RayHit = data.Ray.Position + data.Ray.Direction * data.MaxLength;
        }

        RaiseNetworkEvent(msg);
    }
#endif
}
