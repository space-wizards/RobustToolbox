using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [BaseTypeRequired(typeof(ITypeSerializer<>))]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [MeansImplicitUse]
    public class TypeSerializerAttribute : Attribute{}
}
