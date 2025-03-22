using Robust.Shared.Prototypes;
using Robust.UnitTesting.Constraints;

namespace Robust.UnitTesting;

public sealed class Has : NUnit.Framework.Has
{
    public static PrototypeIndexConstraint<T> Index<T>(ProtoId<T> protoId)
        where T : class, IPrototype
    {
        return new(protoId);
    }
}
