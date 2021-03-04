using Robust.Client.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Client.Serialization
{
    [TypeSerializer]
    public class AppearanceVisualizerSerializer : ITypeSerializer<AppearanceVisualizer, MappingDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager, MappingDataNode node,
            bool skipHook,
            ISerializationContext? context = null)
        {
            if (!node.TryGetNode("type", out var typeNode))
                throw new InvalidMappingException("No type specified for AppearanceVisualizer!");

            if (typeNode is not ValueDataNode typeValueDataNode)
                throw new InvalidMappingException("Type node not a value node for AppearanceVisualizer!");

            var type = IoCManager.Resolve<IReflectionManager>()
                .YamlTypeTagLookup(typeof(AppearanceVisualizer), typeValueDataNode.Value);
            if (type == null)
                throw new InvalidMappingException(
                    $"Invalid type {typeValueDataNode.Value} specified for AppearanceVisualizer!");

            var newNode = (MappingDataNode)node.Copy();
            newNode.RemoveNode("type");
            return serializationManager.Read(type, newNode, context, skipHook);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
            ISerializationContext? context)
        {
            if (!node.TryGetNode("type", out var typeNode) || typeNode is not ValueDataNode valueNode)
            {
                return new ErrorNode(node, "Missing/Invalid type", true);
            }

            var reflectionManager = IoCManager.Resolve<IReflectionManager>();
            var type = reflectionManager.YamlTypeTagLookup(typeof(AppearanceVisualizer), valueNode.Value);

            if (type == null)
            {
                return new ErrorNode(node, $"Failed to resolve type: {valueNode.Value}", true);
            }

            return serializationManager.ValidateNode(type, node.CopyCast<MappingDataNode>().RemoveNode("type"));
        }

        public DataNode Write(ISerializationManager serializationManager, AppearanceVisualizer value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var mapping = serializationManager.WriteValueAs<MappingDataNode>(value.GetType(), value, alwaysWrite, context);
            mapping.AddNode("type", new ValueDataNode(value.GetType().Name));
            return mapping;
        }

        public AppearanceVisualizer Copy(ISerializationManager serializationManager, AppearanceVisualizer source,
            AppearanceVisualizer target, bool skipHook, ISerializationContext? context = null)
        {
            return serializationManager.Copy(source, target, context)!;
        }
    }
}
