using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class ResourcePathSerializer : ITypeSerializer<ResourcePath>
    {
        public ResourcePath NodeToType(IDataNode node, ISerializationContext? context = null)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            return new(valueDataNode.GetValue());
        }

        public IDataNode TypeToNode(ResourcePath value, IDataNodeFactory nodeFactory,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToString());
        }
    }
}
