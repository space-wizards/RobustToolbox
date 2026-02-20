using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus.Conditions;

[ImplicitDataDefinitionForInheritors]
[PublicAPI]
public abstract partial class AnimusConditionBase
{
    internal bool LastResult = false;

    /// <summary>
    /// I couldn't get IoCManager.InjectDependencies to work, use this method to initialize them manually.
    /// </summary>
    /// <param name="entityManager"></param>
    public virtual void Initialize(EntityManager entityManager)
    {

    }

    protected abstract bool Evaluate(EntityUid ent);

    internal bool EvaluateInternal(Entity<AnimusComponent> ent)
    {
        LastResult = Evaluate(ent);
        return LastResult;
    }
}
