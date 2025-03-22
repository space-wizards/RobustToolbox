using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Constraints;

namespace Robust.UnitTesting;

public sealed class Has : NUnit.Framework.Has
{
    public static PrototypeManagerIndexConstraint<T> Index<T>(ProtoId<T> protoId)
        where T : class, IPrototype => new(protoId);

    public static PrototypeManagerIndexConstraint<EntityPrototype> Index(EntProtoId protoId) => new(protoId);

    public static EntityPrototypeComponentConstraint<T> Component<T>(IComponentFactory componentFactory)
        where T : IComponent, new() => new(componentFactory);

    public static EntityComponentConstraint<T> Component<T>(IEntityManager entityManager)
        where T : IComponent, new() => new(entityManager);
}
