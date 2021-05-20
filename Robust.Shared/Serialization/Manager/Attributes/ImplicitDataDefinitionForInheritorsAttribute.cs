using System;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class ImplicitDataDefinitionForInheritorsAttribute : Attribute
    {
    }
}
