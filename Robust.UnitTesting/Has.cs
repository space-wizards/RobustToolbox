using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Constraints;

namespace Robust.UnitTesting;

public sealed class Has : NUnit.Framework.Has
{
    /// <summary>
    /// Returns a constraint that tests if a <typeparamref name="T"/> is registered with the specified id.
    /// </summary>
    public static PrototypeManagerIndexConstraint<T> Index<T>(ProtoId<T> protoId)
        where T : class, IPrototype => new(protoId);

    /// <summary>
    /// Returns a constraint that tests if an <see cref="EntityPrototype"/> is registered with the specified id.
    /// </summary>
    public static PrototypeManagerIndexConstraint<EntityPrototype> Index(EntProtoId protoId) => new(protoId);

    /// <summary>
    /// Returns a constraint that tests if the <see cref="EntityPrototype"/> has a component of type <typeparamref name="T"/>.
    /// </summary>
    public static EntityPrototypeComponentConstraint<T> Component<T>(IComponentFactory componentFactory)
        where T : IComponent, new() => new(componentFactory);

    /// <summary>
    /// Returns a constraint that tests if the entity has a component of type <typeparamref name="T"/>.
    /// </summary>
    public static EntityComponentConstraint<T> Component<T>(IEntityManager entityManager)
        where T : IComponent, new() => new(entityManager);
}
