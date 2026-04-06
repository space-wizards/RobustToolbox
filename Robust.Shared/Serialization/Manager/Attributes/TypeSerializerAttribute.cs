using System;
using JetBrains.Annotations;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    /// <summary>
    ///     Registers a <see cref="ITypeSerializer{TType,TNode}"/> as a default serializer for its type.
    ///     This should only be used for serializers that should always be used unconditionally. If you're making a
    ///     custom serializer, do not apply this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [MeansImplicitUse]
    public sealed class TypeSerializerAttribute : Attribute
    {
    }
}
