using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    /// <summary>
    /// Registers a <see cref="Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeSerializer"/> as a default serializer for its type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [MeansImplicitUse]
    public sealed class TypeSerializerAttribute : Attribute
    {
    }
}
