using Robust.Shared.Animations;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects;

public sealed class AnimationPlayerSystem : SharedAnimationPlayerSystem
{
    public override void Flick(EntityUid uid, string stateId, object layerKey)
    {
        var ev = new AnimationFlickEvent(GetNetEntity(uid), stateId, layerKey);
        RaiseNetworkEvent(ev);
    }
}
