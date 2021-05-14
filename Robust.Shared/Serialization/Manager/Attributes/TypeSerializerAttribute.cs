using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [MeansImplicitUse]
    public class TypeSerializerAttribute : Attribute
    {
    }
}
