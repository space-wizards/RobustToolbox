using System;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class ImplicitDataDefinitionForInheritorsAttribute : Attribute
    {
    }
}
