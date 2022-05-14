using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

public sealed class LocalizedStringSerializer : ITypeSerializer<string, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
         return dependencies.Resolve<ILocalizationManager>().HasString(node.Value)
             ? new ValidatedValueNode(node)
             : new ErrorNode(node, $"Can't find localization string for {node.Value}");

    }

    public string Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
        bool skipHook, ISerializationContext? context = null, string? value = default)
    {
        var isValid = dependencies.Resolve<ILocalizationManager>().TryGetString(node.Value, out var res);
        return isValid ? res! : throw new InvalidMappingException($"Localization string {node.Value} not found");
    }

    public DataNode Write(ISerializationManager serializationManager, string value, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode(value);
    }

    public string Copy(ISerializationManager serializationManager, string source, string target, bool skipHook,
        ISerializationContext? context = null)
    {
        return source;
    }
}
