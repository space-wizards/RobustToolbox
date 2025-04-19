using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Robust.Shared.Animations;

public abstract class SharedAnimationPlayerSystem : EntitySystem
{
    /// <summary>
    /// Plays an <see cref="AnimationTrackSpriteFlick"/> for a target entity for the full duration of the state.
    /// </summary>
    /// <param name="uid">The entity to play the animation on.</param>
    /// <param name="stateId">The state we are playing.</param>
    /// <param name="layerKey">A key for the sprite layer. Must be NetSerializable.</param>
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
