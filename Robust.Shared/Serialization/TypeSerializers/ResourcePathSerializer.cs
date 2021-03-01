using System;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class ResourcePathSerializer : ITypeSerializer<ResourcePath, ValueDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            return new DeserializedValue<ResourcePath>(new ResourcePath(node.Value));
        }

        public bool Validate(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            try
            {
                _ = new ResourcePath(node.Value);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        public DataNode Write(ISerializationManager serializationManager, ResourcePath value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        [MustUseReturnValue]
        public ResourcePath Copy(ISerializationManager serializationManager, ResourcePath source, ResourcePath target, ISerializationContext? context = null)
        {
            return new(source.ToString());
        }
    }
}
