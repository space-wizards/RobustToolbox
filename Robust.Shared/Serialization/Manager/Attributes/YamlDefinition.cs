using System;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [MeansYamlDefinition]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class YamlDefinition : Attribute{ }
}
