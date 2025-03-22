using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Constraints;

namespace Robust.UnitTesting;

public static class ConstraintExtensions
{
    public static PrototypeManagerIndexConstraint<T> Index<T>(this ConstraintExpression expression, ProtoId<T> protoId)
        where T : class, IPrototype
    {
        var constraint = new PrototypeManagerIndexConstraint<T>(protoId);
        expression.Append(constraint);
        return constraint;
    }

    public static PrototypeManagerIndexConstraint<EntityPrototype> Index(this ConstraintExpression expression, EntProtoId protoId)
    {
        var constraint = new PrototypeManagerIndexConstraint<EntityPrototype>(protoId);
        expression.Append(constraint);
        return constraint;
    }

    public static EntityPrototypeComponentConstraint<T> Component<T>(this ConstraintExpression expression, IComponentFactory compFactory)
        where T : IComponent, new()
    {
        var constraint = new EntityPrototypeComponentConstraint<T>(compFactory);
        expression.Append(constraint);
        return constraint;
    }

    public static EntityComponentConstraint<T> Component<T>(this ConstraintExpression expression, IEntityManager entityManager)
        where T : IComponent, new()
    {
        var constraint = new EntityComponentConstraint<T>(entityManager);
        expression.Append(constraint);
        return constraint;
    }

    public static EntityDeletedConstraint Deleted(this ConstraintExpression expression, IEntityManager entMan)
    {
        var constraint = new EntityDeletedConstraint(entMan);
        expression.Append(constraint);
        return constraint;
    }

    public static EntityOnMapConstraint OnMap(this ConstraintExpression expression, MapId mapId, IEntityManager entMan)
    {
        var constraint = new EntityOnMapConstraint(mapId, entMan);
        expression.Append(constraint);
        return constraint;
    }

    public static EntityOnMapConstraint InNullspace(this ConstraintExpression expression, IEntityManager entMan)
    {
        var constraint = new EntityOnMapConstraint(MapId.Nullspace, entMan);
        expression.Append(constraint);
        return constraint;
    }

    public static EntityInRangeOfConstraint InRangeOf(this ConstraintExpression expression, EntityUid other, float range, SharedTransformSystem xformSystem)
    {
        var constraint = new EntityInRangeOfConstraint(other, range, xformSystem);
        expression.Append(constraint);
        return constraint;
    }

    public static EntityInRangeOfConstraint InRangeOf(this ConstraintExpression expression, EntityUid other, float range, IEntityManager entMan)
    {
        var constraint = new EntityInRangeOfConstraint(other, range, entMan.System<SharedTransformSystem>());
        expression.Append(constraint);
        return constraint;
    }
}
