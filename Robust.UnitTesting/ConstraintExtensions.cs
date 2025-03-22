using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;
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
}
