using System;
using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Map;

internal sealed class MapSerializationContext : ISerializationContext, IEntityLoadContext,
    ITypeSerializer<EntityUid, ValueDataNode>
{
    private readonly IComponentFactory _factory;
    private readonly ISerializationManager _serializationManager;

    public Dictionary<(Type, Type), object> TypeReaders { get; }
    public Dictionary<Type, object> TypeWriters { get; }
    public Dictionary<Type, object> TypeCopiers => TypeWriters;
    public Dictionary<(Type, Type), object> TypeValidators => TypeReaders;

    // Run-specific data
    public Dictionary<ushort, string>? TileMap;
    public readonly Dictionary<string, MappingDataNode> CurrentReadingEntityComponents = new();
    public HashSet<string> CurrentlyIgnoredComponents = new();
    public string? CurrentWritingComponent;
    public EntityUid? CurrentWritingEntity;

    private Dictionary<int, EntityUid> _uidEntityMap = new();
    private Dictionary<EntityUid, int> _entityUidMap = new();

    public MapSerializationContext(IComponentFactory factory, ISerializationManager serializationManager)
    {
        _factory = factory;
        _serializationManager = serializationManager;

        TypeWriters = new Dictionary<Type, object>()
        {
            {typeof(EntityUid), this}
        };
        TypeReaders = new Dictionary<(Type, Type), object>()
        {
            {(typeof(EntityUid), typeof(ValueDataNode)), this}
        };
    }

    public void Set(Dictionary<int, EntityUid> uidEntityMap, Dictionary<EntityUid, int> entityUidMap)
    {
        _uidEntityMap = uidEntityMap;
        _entityUidMap = entityUidMap;
    }

    public void Clear()
    {
        CurrentReadingEntityComponents.Clear();
        CurrentlyIgnoredComponents.Clear();
        CurrentWritingComponent = null;
        CurrentWritingEntity = null;
    }

    // Create custom object serializers that will correctly allow data to be overriden by the map file.
    MappingDataNode IEntityLoadContext.GetComponentData(string componentName,
        MappingDataNode? protoData)
    {
        if (CurrentReadingEntityComponents == null)
        {
            throw new InvalidOperationException();
        }


        if (CurrentReadingEntityComponents.TryGetValue(componentName, out var mapping))
        {
            if (protoData == null) return mapping.Copy();

            return _serializationManager.PushCompositionWithGenericNode(
                _factory.GetRegistration(componentName).Type, new[] { protoData }, mapping, this);
        }

        return protoData ?? new MappingDataNode();
    }

    public IEnumerable<string> GetExtraComponentTypes()
    {
        return CurrentReadingEntityComponents!.Keys;
    }

    public bool ShouldSkipComponent(string compName)
    {
        return CurrentlyIgnoredComponents.Contains(compName);
    }

    ValidationNode ITypeValidator<EntityUid, ValueDataNode>.Validate(ISerializationManager serializationManager,
        ValueDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
    {
        if (node.Value == "null")
        {
            return new ValidatedValueNode(node);
        }

        if (!int.TryParse(node.Value, out var val) || !_uidEntityMap.ContainsKey(val))
        {
            return new ErrorNode(node, "Invalid EntityUid", true);
        }

        return new ValidatedValueNode(node);
    }

    public DataNode Write(ISerializationManager serializationManager, EntityUid value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        if (!_entityUidMap.TryGetValue(value, out var entityUidMapped))
        {
            // Terrible hack to mute this warning on the grids themselves when serializing blueprints.
            if (CurrentWritingComponent != "Transform")
            {
                Logger.WarningS("map", "Cannot write entity UID '{0}'.", value);
            }

            return new ValueDataNode("null");
        }

        return new ValueDataNode(entityUidMapped.ToString(CultureInfo.InvariantCulture));
    }

    EntityUid ITypeReader<EntityUid, ValueDataNode>.Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        bool skipHook,
        ISerializationContext? context, EntityUid _)
    {
        if (node.Value == "null")
        {
            return EntityUid.Invalid;
        }

        var val = int.Parse(node.Value);

        if (!_uidEntityMap.TryGetValue(val, out var entity))
        {
            Logger.ErrorS("map", "Error in map file: found local entity UID '{0}' which does not exist.", val);
            return EntityUid.Invalid;
        }
        else
        {
            return entity;
        }
    }

    [MustUseReturnValue]
    public EntityUid Copy(ISerializationManager serializationManager, EntityUid source, EntityUid target,
        bool skipHook,
        ISerializationContext? context = null)
    {
        return new((int)source);
    }
}
