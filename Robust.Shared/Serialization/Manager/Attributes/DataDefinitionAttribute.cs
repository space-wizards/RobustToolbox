using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    [MeansDataDefinition]
    [MeansImplicitUse]
    public sealed class DataDefinitionAttribute : Attribute
    {
    }
}
