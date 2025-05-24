using System;
using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;

namespace Robust.UnitTesting.Constraints;

public sealed class EntityDeletedConstraint(IEntityManager entMan) : Constraint
{
    private readonly IEntityManager _entMan = entMan;
    public override string Description => "deleted";

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        var uid = ConstraintUtils.GetEntityUid(actual);
        return new ConstraintResult(this, _entMan.ToPrettyString(uid), _entMan.Deleted(uid));
    }
}
