using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.UnitTesting.Constraints;

public sealed class EntityOnMapConstraint(MapId mapId, IEntityManager entMan) : Constraint
{
    private readonly MapId _mapId = mapId;
    private readonly IEntityManager _entMan = entMan;

    public override string Description => $"on map {_mapId}";

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        var uid = ConstraintUtils.GetEntityUid(actual);
        var xform = _entMan.GetComponent<TransformComponent>(uid);
        return new ConstraintResult(this, $"on map {xform.MapID}", xform.MapID == _mapId);
    }
}
