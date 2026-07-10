using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus.Conditions;

[ImplicitDataDefinitionForInheritors]
[PublicAPI]
public abstract partial class AnimusConditionBase
{
    private bool _lastResult;
    private TimeSpan _lastUpdate;
    protected virtual TimeSpan UpdateInterval { get; } = TimeSpan.FromSeconds(0.1);

    /// <summary>
    /// IoCManager.InjectDependencies doesn't work, override this method to initialize dependencies manually.
    /// </summary>
    /// <param name="entityManager"></param>
    public virtual void Initialize(EntityManager entityManager)
    {

    }

    protected abstract bool Evaluate(EntityUid ent);

    internal bool EvaluateInternal(Entity<AnimusComponent> ent, TimeSpan currentTime, bool isFirstTimePredicted)
    {
        if (!isFirstTimePredicted || currentTime - _lastUpdate < UpdateInterval)
            return _lastResult;
        _lastResult = Evaluate(ent);
        return _lastResult;
    }
}
