using System;
using Robust.Client.Animus.Conditions;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus.States;

[ImplicitDataDefinitionForInheritors]
internal abstract partial class AnimusStateBase
{
    /// <summary>
    /// A collection of conditions that must be true for this state to activate.
    /// </summary>
    [DataField]
    internal AnimusConditionBase[] Conditions = [];

    /// <summary>
    /// If set to true, the state only executes its action one time instead of restarting it once the animation ends.
    /// </summary>
    [DataField]
    internal bool OneShot;

    /// <summary>
    /// If set to non-zero, state exits automatically after the defined timespan.
    /// </summary>
    [DataField]
    internal TimeSpan ExitPeriod = TimeSpan.Zero;

    internal AnimusInstance Instance;

    [MustCallBase]
    internal virtual void Initialize(EntityUid ent, EntityManager entityManager, AnimusInstance animusInstance)
    {
        Instance = animusInstance;
    }

    internal virtual void Enter(EntityUid ent)
    {
    }

    internal virtual void Update(EntityUid ent, bool finished)
    {
    }

    internal virtual void Exit(EntityUid ent)
    {
    }
}

internal sealed partial class AnimusStateNull : AnimusStateBase
{
}
