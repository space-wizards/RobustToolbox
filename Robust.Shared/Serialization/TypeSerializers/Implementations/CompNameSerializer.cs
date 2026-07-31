using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using static Robust.Shared.Serialization.Manager.ISerializationManager;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations;

/// <summary>
/// Serializer used automatically for <see cref="CompName"/> types.
/// </summary>
[TypeSerializer]
public sealed class CompNameSerializer : ITypeSerializer<CompName, ValueDataNode>, ITypeCopyCreator<CompName>
{
    private IComponentFactory? _factory;

    public ValidationNode Validate(ISerializationManager serialization, ValueDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var name = node.Value;
        _factory ??= dependencies.Resolve<IComponentFactory>();
        if (_factory.HasRegistration(name))
            return new ValidatedValueNode(node);

        return new ErrorNode(node, $"No component found with name {name}");
    }

    public CompName Read(ISerializationManager serialization, ValueDataNode node, IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, InstantiationDelegate<CompName>? instanceProvider = null)
        => new CompName(node.Value, _factory ??= dependencies.Resolve<IComponentFactory>());

    public DataNode Write(ISerializationManager serialization, CompName value, IDependencyCollection dependencies, bool alwaysWrite = false, ISerializationContext? context = null)
        => new ValueDataNode(value.Name);

    public CompName CreateCopy(ISerializationManager serializationManager, CompName source, IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        => source;
}
