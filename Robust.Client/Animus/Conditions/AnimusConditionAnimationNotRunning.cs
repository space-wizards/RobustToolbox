using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus.Conditions;

public sealed partial class AnimusConditionAnimationNotRunning : AnimusConditionBase
{
    [DataField]
    public string AnimationKey = "";

    private AnimusSystem _animationStateMachineSystem;

    public override void Initialize(EntityManager entityManager)
    {
        base.Initialize(entityManager);
        _animationStateMachineSystem = entityManager.System<AnimusSystem>();
    }

    protected override bool Evaluate(EntityUid ent)
    {
        return !_animationStateMachineSystem.HasAnimationRunning(ent, AnimationKey);
    }
}
