using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;

namespace Robust.UnitTesting.Constraints;

public sealed class EntityInRangeOfConstraint(EntityUid other, float range, SharedTransformSystem xformSys) : Constraint
{
    private readonly EntityUid _other = other;
    private readonly float _range = range;
    private readonly SharedTransformSystem _xformSys = xformSys;

    public override string Description => $"within {_range} units of other";

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        var uid = ConstraintUtils.GetEntityUid(actual);
        return new ConstraintResult(this, $"more than {_range} units from other", _xformSys.InRange(uid, _other, _range));
    }
}
