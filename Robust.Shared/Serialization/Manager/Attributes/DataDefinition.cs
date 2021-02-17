using System;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [MeansDataDefinition]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class DataDefinition : Attribute{ }
}
