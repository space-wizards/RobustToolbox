using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces
{
    /// <summary>
    /// A serializer for a given type that supports reading and writing.
    /// </summary>
    /// <p>
    /// Types that implement this should be marked with the following annotation:
    /// </p>
    /// <see cref="Robust.Shared.Serialization.Manager.Attributes.TypeSerializerAttribute"/>
    /// <typeparam name="TType">The type that we want to serialize to/from</typeparam>
    /// <typeparam name="TNode">The YAML node that can represent the type</typeparam>
    public interface ITypeSerializer<TType, TNode> :
        ITypeReaderWriter<TType, TNode>
        where TNode : DataNode
    {
    }
}
