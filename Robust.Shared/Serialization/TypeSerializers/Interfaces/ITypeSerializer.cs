using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces;

/// <summary>
/// A serializer for a given type that supports reading and writing. Can be used as a default serializer or a custom serializer on a datafield.
/// </summary>
/// <remarks>
/// Types that implement this may be annotated with <see cref="Robust.Shared.Serialization.Manager.Attributes.TypeSerializerAttribute"/>
/// to register it as the default serializer for a type
/// </remarks>
/// <typeparam name="TType">The type that we want to serialize to/from</typeparam>
/// <typeparam name="TNode">The YAML node that can represent the type</typeparam>
public interface ITypeSerializer<TType, TNode> :
    ITypeReader<TType, TNode>,
    ITypeWriter<TType>
    where TNode : DataNode;

/// <summary>
/// Base class for type serializers that will have some fields set when created by the manager.
/// </summary>
public abstract class BaseTypeSerializer
{
    /// <summary>
    /// The serialization manager.
    /// </summary>
    public SerializationManager SerMan { get; internal set; } = default!;

    /// <summary>
    /// Sawmill that can be used for logging errors specific to type serializers.
    /// </summary>
    public ISawmill Log { get; internal set; } = default!;
}
