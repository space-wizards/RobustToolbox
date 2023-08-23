using System.Globalization;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.GameObjects.Serializers;

[TypeSerializer]
public sealed class NetEntitySerializer : ITypeSerializer<NetEntity, ValueDataNode>, ITypeCopyCreator<NetEntity>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var nodeValue = node.Value;

        return int.TryParse(nodeValue, CultureInfo.InvariantCulture, out _) ? new ValidatedValueNode(node) : new ErrorNode(node, "Failed parsing NetEntity.");
    }

    public NetEntity Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<NetEntity>? instanceProvider = null)
    {
        var entity = serializationManager.Read<EntityUid>(node);
        var entManager = dependencies.Resolve<IEntityManager>();
        return entManager.GetNetEntity(entity);
    }

    public DataNode Write(ISerializationManager serializationManager, NetEntity value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        var entManager = dependencies.Resolve<IEntityManager>();
        var entity = entManager.GetEntity(value);
        return serializationManager.WriteValue(entity);
    }

    public NetEntity CreateCopy(ISerializationManager serializationManager, NetEntity source, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        return new NetEntity(source.GetHashCode());
    }
}
