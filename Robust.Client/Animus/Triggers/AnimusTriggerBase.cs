using JetBrains.Annotations;
using Robust.Client.Animus.States;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus.Triggers;

[ImplicitDataDefinitionForInheritors]
[PublicAPI]
public abstract partial class AnimusTriggerBase
{
    private AnimusSystem _animationStateMachineSystem;
    private Entity<AnimusComponent> _entity;
    private AnimusStateBase _parentState;
    private bool _triggered = false;

    public virtual void TriggerIfNecessary(EntityUid entity)
    {

    }

    protected virtual void Initialize(EntityManager entityManager)
    {

    }

    protected void Trigger()
    {
        _triggered = true;
        _animationStateMachineSystem.TriggerInternal(_parentState, _entity);
    }

    internal void InitializeInternal(
        EntityManager entityManager,
        Entity<AnimusComponent> entity,
        AnimusStateBase state)
    {
        _animationStateMachineSystem = entityManager.System<AnimusSystem>();
        _entity = entity;
        _parentState = state;
        Initialize(entityManager);
    }

    internal bool TriggerIfNecessaryInternal(EntityUid entity)
    {
        _triggered = false;
        TriggerIfNecessary(entity);
        return _triggered;
    }
}
