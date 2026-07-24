using Robust.Client.Animus.Actions;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus.States;

internal sealed partial class AnimusStateAnimation : AnimusStateBase
{
    private static readonly AnimusActionAnimationBase NullAction = new AnimusActionAnimationNull();

    private AnimationPlayerSystem _animationPlayerSystem;
    private AnimusSystem _animusSystem;
    private AppearanceSystem _appearanceSystem;

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
        _animusSystem = entityManager.System<AnimusSystem>();
        _appearanceSystem = entityManager.System<AppearanceSystem>();
        Action.Initialize(entityManager);
    }

    internal override void Enter(EntityUid ent)
    {
        if (_animusSystem.HasAnimationRunning(ent, RunningAnimationKey) ||
            !Action.TryNextAnimation(_appearanceSystem, ent, out var animation, false))
            return;

        _animationPlayerSystem.Play(ent, animation, RunningAnimationKey);
    }

    internal override void Update(EntityUid ent, bool finished)
    {
        if (_animusSystem.HasAnimationRunning(ent, RunningAnimationKey) ||
            !Action.TryNextAnimation(_appearanceSystem, ent, out var animation, finished))
            return;

        _animationPlayerSystem.Play(ent, animation, RunningAnimationKey);
    }

    internal override void Exit(EntityUid ent)
    {
        // Stop the running animation of this state.
        if (_animusSystem.HasAnimationRunning(ent, RunningAnimationKey))
            _animationPlayerSystem.Stop(ent, RunningAnimationKey);

        // Fetch stopping animation if it isn't running yet.
        if (_animusSystem.HasAnimationRunning(ent, StopAnimationKey) ||
            !Action.TryStopAnimation(_appearanceSystem, ent, out var animation))
            return;

        // Play the stopping animation.
        _animationPlayerSystem.Play(ent, animation, StopAnimationKey);
    }
}
