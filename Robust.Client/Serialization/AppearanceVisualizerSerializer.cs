using System;
using Robust.Client.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Client.Serialization
{
    [TypeSerializer]
    public sealed class AppearanceVisualizerSerializer : ITypeSerializer<AppearanceVisualizer, MappingDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null, AppearanceVisualizer? value = null)
        {
            Type? type = null;
            if (!node.TryGet("type", out var typeNode))
            {
                if (value == null)
                    throw new InvalidMappingException("No type specified for AppearanceVisualizer!");

                type = value.GetType();
            }
            else
            {
                if (typeNode is not ValueDataNode typeValueDataNode)
                    throw new InvalidMappingException("Type node not a value node for AppearanceVisualizer!");

                type = IoCManager.Resolve<IReflectionManager>()
                    .YamlTypeTagLookup(typeof(AppearanceVisualizer), typeValueDataNode.Value);
                if (type == null)
                    throw new InvalidMappingException(
                        $"Invalid type {typeValueDataNode.Value} specified for AppearanceVisualizer!");

                if(value != null && !type.IsInstanceOfType(value))
                {
                    throw new InvalidMappingException(
                        $"Specified Type does not match type of provided Value for AppearanceVisualizer! (TypeOfValue: {value.GetType()}, ProvidedValue: {type})");
                }
            }

            var newNode = node.Copy();
            newNode.Remove("type");
            return serializationManager.Read(type, newNode, context, skipHook, value);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context)
        {
            if (!node.TryGet("type", out var typeNode) || typeNode is not ValueDataNode valueNode)
            {
                return new ErrorNode(node, "Missing/Invalid type", true);
            }

            var reflectionManager = IoCManager.Resolve<IReflectionManager>();
            var type = reflectionManager.YamlTypeTagLookup(typeof(AppearanceVisualizer), valueNode.Value);

            if (type == null)
            {
                return new ErrorNode(node, $"Failed to resolve type: {valueNode.Value}", true);
            }

            return serializationManager.ValidateNode(type, node.CopyCast<MappingDataNode>().Remove("type"));
        }

        public DataNode Write(ISerializationManager serializationManager, AppearanceVisualizer value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var mapping = serializationManager.WriteValueAs<MappingDataNode>(value.GetType(), value, alwaysWrite, context);
            mapping.Add("type", new ValueDataNode(value.GetType().Name));
            return mapping;
        }

        public AppearanceVisualizer Copy(ISerializationManager serializationManager, AppearanceVisualizer source,
            AppearanceVisualizer target, bool skipHook, ISerializationContext? context = null)
        {
            return serializationManager.Copy(source, target, context)!;
        }
    }
}
