using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Constraints;

namespace Robust.UnitTesting;

public sealed class Has : NUnit.Framework.Has
{
    public static PrototypeManagerIndexConstraint<T> Index<T>(ProtoId<T> protoId)
        where T : class, IPrototype
    {
        return new(protoId);
    }

    public static EntityPrototypeComponentConstraint<T> Component<T>(IComponentFactory componentFactory)
        where T : IComponent, new()
    {
        return new(componentFactory);
    }
}
