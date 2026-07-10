using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus.Actions;

[ImplicitDataDefinitionForInheritors]
[PublicAPI]
public abstract partial class AnimusActionAnimationBase
{
    /// <summary>
    /// Simple stopping animation for cases where only rotation and offset were affected.
    /// </summary>
    protected static readonly Animation StopAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Rotation),
                InterpolationMode = AnimationInterpolationMode.Linear,
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(0), 0),
                },
            },
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Offset),
                InterpolationMode = AnimationInterpolationMode.Linear,
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector2(), 0),
                },
            },
        },
    };

    public virtual void Initialize(EntityManager entityManager)
    {

    }

    protected abstract Animation? GetNextAnimation(AppearanceSystem appearanceSystem, EntityUid entity, bool restarting);

    protected abstract Animation? GetStopAnimation(AppearanceSystem appearanceSystem, EntityUid entity);

    internal bool TryNextAnimation(AppearanceSystem appearanceSystem, EntityUid entity, [NotNullWhen(true)] out Animation? anim, bool restarting)
    {
        anim = GetNextAnimation(appearanceSystem, entity, restarting);
        return anim != null;
    }

    internal bool TryStopAnimation(AppearanceSystem appearanceSystem, EntityUid entity, [NotNullWhen(true)] out Animation? anim)
    {
        anim = GetStopAnimation(appearanceSystem, entity);
        return anim != null;
    }
}

public sealed partial class AnimusActionAnimationNull : AnimusActionAnimationBase
{
    protected override Animation? GetNextAnimation(AppearanceSystem appearanceSystem, EntityUid entity, bool restarting)
    {
        return null;
    }

    protected override Animation? GetStopAnimation(AppearanceSystem appearanceSystem, EntityUid entity)
    {
        return null;
    }
}
