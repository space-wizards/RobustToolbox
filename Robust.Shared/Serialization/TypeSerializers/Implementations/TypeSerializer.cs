using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public class TypeSerializer : ITypeSerializer<Type, ValueDataNode>
    {
        private static readonly Dictionary<string, Type> _shortcuts = new ()
        {
            {"bool", typeof(bool)}
        };

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            if (_shortcuts.ContainsKey(node.Value)) return new ValidatedValueNode(node);

            return dependencies.Resolve<IReflectionManager>().GetType(node.Value) == null
                ? new ErrorNode(node, $"Type '{node.Value}' not found.")
                : new ValidatedValueNode(node);
        }

        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null)
        {
            if(_shortcuts.TryGetValue(node.Value, out var shortcutType)) return DeserializationResult.Value(shortcutType);

            var type = dependencies.Resolve<IReflectionManager>().GetType(node.Value);
            if (type == null) throw new InvalidMappingException($"Type '{node.Value}' not found.");
            return DeserializationResult.Value(type);
        }

        public DataNode Write(ISerializationManager serializationManager, Type value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.FullName ?? value.Name);
        }

        public Type Copy(ISerializationManager serializationManager, Type source, Type target, bool skipHook,
            ISerializationContext? context = null)
        {
            return source;
        }
    }
}
