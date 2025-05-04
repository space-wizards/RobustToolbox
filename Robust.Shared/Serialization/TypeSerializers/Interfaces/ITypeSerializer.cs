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
