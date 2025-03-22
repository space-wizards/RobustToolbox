using System;
using System.Linq;
using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;

namespace Robust.UnitTesting.Constraints;

public sealed class EntityManagerComponentCountConstraint<T>(int count) : Constraint
    where T : IComponent
{
    private readonly int _count = count;

    public override string Description => $"{_count} entities";

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (actual is not IEntityManager entMan)
            throw new ArgumentException($"Expected IEntityManager but was {actual?.GetType()}");
        var allComps = entMan.AllComponents<T>();
        var entsWithComp = allComps.Select(e => entMan.ToPrettyString(e.Uid));
        return new ConstraintResult(this, $"{allComps.Length} entities: {string.Join(", ", entsWithComp)}");
    }
}
