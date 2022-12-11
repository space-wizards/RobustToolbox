using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Server.GameObjects;

public sealed class ServerInterfaceDataTypeSerializer : ITypeReaderWriter<Dictionary<Enum, BoundUserInterface>, SequenceDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return serializationManager.ValidateNode<List<SharedUserInterfaceComponent.PrototypeData>>(node, context);
    }

    public Dictionary<Enum, BoundUserInterface> Read(ISerializationManager serializationManager, SequenceDataNode node, IDependencyCollection dependencies,
        bool skipHook, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<Dictionary<Enum, BoundUserInterface>>? instanceProvider = null)
    {
        var instance = instanceProvider != null ? instanceProvider() : new();

        foreach (var data in serializationManager.Read<SharedUserInterfaceComponent.PrototypeData[]>(node, context, skipHook))
        {
            instance[data.UiKey] = new(data);
        }

        return instance;
    }

    public DataNode Write(ISerializationManager serializationManager, Dictionary<Enum, BoundUserInterface> value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        return serializationManager.WriteValue(value.Values.ToArray(), alwaysWrite, context);
    }
}
