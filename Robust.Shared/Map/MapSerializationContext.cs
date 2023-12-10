using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Timing;

namespace Robust.Shared.Map;

internal sealed class MapSerializationContext : ISerializationContext, IEntityLoadContext,
    ITypeSerializer<EntityUid, ValueDataNode>
{
    public SerializationManager.SerializerProvider SerializerProvider { get; } = new();

    // Run-specific data
    public Dictionary<int, string>? TileMap;
    public readonly Dictionary<string, IComponent> CurrentReadingEntityComponents = new();
    public HashSet<string> CurrentlyIgnoredComponents = new();
    public string? CurrentComponent;
    public EntityUid? CurrentWritingEntity;
    public IEntityManager EntityManager;
    public IGameTiming Timing;

    private Dictionary<int, EntityUid> _uidEntityMap = new();
    private Dictionary<EntityUid, int> _entityUidMap = new();

    /// <summary>
    /// Are we currently iterating prototypes or entities for writing.
    /// </summary>
    public bool WritingReadingPrototypes { get; set; }

    /// <summary>
    /// Whether the map has been MapInitialized or not.
    /// </summary>
    public bool MapInitialized;

    /// <summary>
    /// How long the target map has been paused. Used for time offsets.
    /// </summary>
    public TimeSpan PauseTime;

    /// <summary>
    /// The parent of the entity being saved, This entity is not itself getting saved.
    /// </summary>
    private EntityUid? _parentUid;

    public MapSerializationContext(IEntityManager entityManager, IGameTiming timing)
    {
        EntityManager = entityManager;
        Timing = timing;
        SerializerProvider.RegisterSerializer(this);
    }

    public void Set(
        Dictionary<int, EntityUid> uidEntityMap,
        Dictionary<EntityUid, int> entityUidMap,
        bool mapPreInit,
        TimeSpan pauseTime,
        EntityUid? parentUid)
    {
        _uidEntityMap = uidEntityMap;
        _entityUidMap = entityUidMap;
        MapInitialized = mapPreInit;
        PauseTime = pauseTime;
        if (parentUid != null && parentUid.Value.IsValid())
            _parentUid = parentUid;
    }

    public void Clear()
    {
        CurrentReadingEntityComponents.Clear();
        CurrentlyIgnoredComponents.Clear();
        CurrentComponent = null;
        CurrentWritingEntity = null;
        PauseTime = TimeSpan.Zero;
    }

    // Create custom object serializers that will correctly allow data to be overriden by the map file.
    bool IEntityLoadContext.TryGetComponent(string componentName, [NotNullWhen(true)] out IComponent? component)
    {
        return CurrentReadingEntityComponents.TryGetValue(componentName, out component);
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
            if (CurrentComponent == "Transform")
            {
                if (!value.IsValid() || value == _parentUid)
                    return new ValueDataNode("invalid");
            }

            dependencies
                .Resolve<ILogManager>()
                .GetSawmill("map")
                .Error("Encountered an invalid entityUid '{0}' while serializing a map.", value);

            return new ValueDataNode("invalid");
        }

        return new ValueDataNode(entityUidMapped.ToString(CultureInfo.InvariantCulture));
    }

    EntityUid ITypeReader<EntityUid, ValueDataNode>.Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context, ISerializationManager.InstantiationDelegate<EntityUid>? _)
    {
        if (node.Value == "invalid" && CurrentComponent == "Transform")
            return EntityUid.Invalid;

        if (int.TryParse(node.Value, out var val) && _uidEntityMap.TryGetValue(val, out var entity))
            return entity;

        dependencies
            .Resolve<ILogManager>()
            .GetSawmill("map")
            .Error("Error in map file: found local entity UID '{0}' which does not exist.", val);

        return EntityUid.Invalid;

    }

    [MustUseReturnValue]
    public EntityUid Copy(ISerializationManager serializationManager, EntityUid source, EntityUid target,
        bool skipHook,
        ISerializationContext? context = null)
    {
        return new((int)source);
    }
}
