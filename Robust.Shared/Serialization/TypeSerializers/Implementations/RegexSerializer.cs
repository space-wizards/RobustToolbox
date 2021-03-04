using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public class RegexSerializer : ITypeSerializer<Regex, ValueDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null)
        {
            return new DeserializedValue<Regex>(new Regex(node.Value, RegexOptions.Compiled));
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            try
            {
                _ = new Regex(node.Value);
            }
            catch (Exception)
            {
                return new ErrorNode(node, "Failed compiling regex.");
            }

            return new ValidatedValueNode(node);
        }

        public DataNode Write(ISerializationManager serializationManager, Regex value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        [MustUseReturnValue]
        public Regex Copy(ISerializationManager serializationManager, Regex source, Regex target,
            bool skipHook,
            ISerializationContext? context = null)
        {
            return new(source.ToString(), source.Options, source.MatchTimeout);
        }
    }
}
