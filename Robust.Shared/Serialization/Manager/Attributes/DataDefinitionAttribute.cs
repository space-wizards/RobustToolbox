using System;
using JetBrains.Annotations;
using YamlDotNet.Serialization.NamingConventions;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    [MeansDataDefinition]
    [MeansImplicitUse]
    [Virtual]
    public sealed class DataDefinitionAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    [MeansDataDefinition]
    [MeansImplicitUse]
    public sealed class DataRecord : Attribute {}
}
