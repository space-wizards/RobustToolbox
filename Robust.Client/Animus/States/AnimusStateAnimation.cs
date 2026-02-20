using System;
using System.Collections.Generic;
using Robust.Client.Animations;
using Robust.Client.Animus.Actions;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus.States;

internal sealed partial class AnimusStateAnimation : AnimusStateBase
{
    private static readonly AnimusActionAnimationBase NullAction = new AnimusActionAnimationNull();

    private AnimationPlayerSystem _animationPlayerSystem;
    private AnimusSystem _animationStateMachineSystem;
    private AppearanceSystem _appearanceSystem;

    private readonly List<(Type, string)> _animationCompProps = [];

    [DataField]
    internal AnimusActionAnimationBase Action = NullAction;

    internal string RunningAnimationKey => Action.GetType().Name + "_RUNNING";
    internal string StopAnimationKey => Action.GetType().Name + "_STOP";

    /// <summary>
    /// IoCManager.InjectDependencies won't work, use this override to inject your dependencies manually.
    /// </summary>
    internal override void Initialize(EntityUid ent, EntityManager entityManager, AnimusInstance animusInstance)
    {
        base.Initialize(ent, entityManager, animusInstance);
        _animationPlayerSystem = entityManager.System<AnimationPlayerSystem>();
        _animationStateMachineSystem = entityManager.System<AnimusSystem>();
        _appearanceSystem = entityManager.System<AppearanceSystem>();
        Action.Initialize(entityManager);
    }

    internal override void Enter(EntityUid ent, bool enteredByTrigger)
    {
        if (enteredByTrigger && Action.RestartOnTrigger ||
            _animationPlayerSystem.HasRunningAnimation(ent, RunningAnimationKey) ||
            !Action.TryNextAnimation(_appearanceSystem, ent, out var animation, false))
            return;

        UpdateAnimationCompProps(ent, animation);
        _animationPlayerSystem.Play(ent, animation, RunningAnimationKey);
    }

    internal override void Update(EntityUid ent, bool finished)
    {
        if (_animationPlayerSystem.HasRunningAnimation(ent, RunningAnimationKey) ||
            !Action.TryNextAnimation(_appearanceSystem, ent, out var animation, finished))
            return;

        UpdateAnimationCompProps(ent, animation);
        _animationPlayerSystem.Play(ent, animation, RunningAnimationKey);
    }

    internal override void Exit(EntityUid ent)
    {
        // Stop the running animation of this state.
        if (_animationPlayerSystem.HasRunningAnimation(ent, RunningAnimationKey))
            _animationPlayerSystem.Stop(ent, RunningAnimationKey);

        // TODO: The stopping animation can still clash with registered component properties but to fix this
        // a state queue would be required first.
        // For now, we'll test and see if any glitches would even occur due to this.
        ClearAnimationCompProps(ent);

        // Fetch stopping animation if it isn't running yet.
        if (_animationPlayerSystem.HasRunningAnimation(ent, StopAnimationKey) ||
            !Action.TryStopAnimation(_appearanceSystem, ent, out var animation))
            return;

        // Play the stopping animation.
        _animationPlayerSystem.Play(ent, animation, StopAnimationKey);
    }

    private void ClearAnimationCompProps(EntityUid ent)
    {
        foreach (var compProp in _animationCompProps)
        {
            _animationStateMachineSystem.DeregisterEntityAnimationProperty(ent,
                compProp.Item1,
                compProp.Item2);
        }
        _animationCompProps.Clear();
    }

    private void UpdateAnimationCompProps(EntityUid ent, Animation anim)
    {
        ClearAnimationCompProps(ent);
        foreach (var track in anim.AnimationTracks)
        {
            if (track is not AnimationTrackComponentProperty { ComponentType: not null, Property: not null } propTrack)
                continue;

            _animationCompProps.Add((propTrack.ComponentType, propTrack.Property));
            _animationStateMachineSystem.RegisterEntityAnimationProperty(ent,
                propTrack.ComponentType,
                propTrack.Property);
        }
    }
}
