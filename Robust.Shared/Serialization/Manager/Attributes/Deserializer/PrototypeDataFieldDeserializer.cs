using System;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.Shared.Serialization.Manager.Attributes.Deserializer
{
    public class PrototypeDataFieldDeserializer : IDataFieldDeserializer
    {
        public DeserializationResult Read(
            object obj,
            Type type,
            DataNode node,
            ISerializationManager manager,
            IDependencyCollection dependencies,
            ISerializationContext? context,
            bool skipHook,
            FieldDefinition field)
        {
            if (node is not ValueDataNode valueNode)
            {
                throw new ArgumentException($"Unable to map a node of type {node.GetType()} into a prototype reference: Use a value node instead.");
            }

            dependencies.Resolve<IPrototypeManager>().RegisterReference(type, valueNode.Value, obj, field);

            return DeserializationResult.Value(type, null);
        }
    }
}
