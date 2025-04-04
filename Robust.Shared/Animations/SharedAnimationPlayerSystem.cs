using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Robust.Shared.Animations;

public abstract class SharedAnimationPlayerSystem : EntitySystem
{
    public abstract void Flick(EntityUid uid, string stateId, object layerKey);
}

[Serializable, NetSerializable]
public sealed class AnimationFlickEvent : EntityEventArgs
{
    public NetEntity Entity;
    public string StateId;
    public object LayerKey;

    public AnimationFlickEvent(NetEntity entity, string stateId, object layerKey)
    {
        Entity = entity;
        StateId = stateId;
        LayerKey = layerKey;
    }
}
