using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations;

public sealed class MapPathSerializer : ITypeSerializer<ResPath, ValueDataNode>
{
    private static readonly ResPath MapRoot = new("/Maps");

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var path = PathHelpers.ApparentPath(new ResPath(node.Value), MapRoot);
        var res = dependencies.Resolve<IResourceManager>();
        if (res.ContentFileExists(path))
            return new ValidatedValueNode(node);

        return new ErrorNode(node, $"Map file not found: {path}");
    }

    public ResPath Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<ResPath>? instanceProvider = null)
        => PathHelpers.ApparentPath(new ResPath(node.Value), MapRoot);

    public DataNode Write(
        ISerializationManager serializationManager,
        ResPath value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
        => new ValueDataNode(value.ToString());
}
