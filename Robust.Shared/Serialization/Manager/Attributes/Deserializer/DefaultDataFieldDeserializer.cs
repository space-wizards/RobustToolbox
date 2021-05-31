using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager.Attributes.Deserializer
{
    public class DefaultDataFieldDeserializer : IDataFieldDeserializer
    {
        public DeserializationResult Read(object obj, Type type,
            DataNode node,
            ISerializationManager manager,
            IDependencyCollection dependencies,
            ISerializationContext? context,
            bool skipHook,
            FieldDefinition field)
        {
            var serializer = field.Attribute.CustomTypeSerializer;

            if (serializer != null)
            {
                return manager.ReadWithTypeSerializer(type, serializer, node, context, skipHook);
            }

            return manager.Read(type, node, context, skipHook);
        }
    }
}
