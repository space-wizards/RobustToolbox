using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

/// <summary>
/// Simple string serializer that just validates that strings correspond to valid component names
/// </summary>
public sealed class ComponentNameSerializer : ITypeSerializer<string, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var factory = dependencies.Resolve<IComponentFactory>();
        if (!factory.TryGetRegistration(node.Value, out _))
            return new ErrorNode(node, $"Unknown component kind: {node.Value}");

        return new ValidatedValueNode(node);
    }

    public string Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<string>? instanceProvider = null)
    {
        return node.Value;
    }

    public DataNode Write(ISerializationManager serializationManager, string value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        return new ValueDataNode(value);
    }
}
