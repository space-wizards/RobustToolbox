using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Server.Maps;

public sealed partial class MapManagerSystem
{
    private ISawmill _logLoader = default!;

    private static readonly MapLoadOptions DefaultLoadOptions = new();
    private const int MapFormatVersion = 2;

    private MapSerializationContext _context = default!;
    private Stopwatch _stopwatch = new();

    public override void Initialize()
    {
        base.Initialize();
        _logLoader = Logger.GetSawmill("loader");
        _context = new MapSerializationContext(_factory, EntityManager, _serManager);
    }

    #region Public

    public bool TryLoadGrid(MapId mapId, string path, out EntityUid? gridUid, MapLoadOptions? options = null)
    {
        options ??= DefaultLoadOptions;
        throw new NotImplementedException();
    }

    public bool TryLoadMap(MapId mapId, string path, out IReadOnlyList<EntityUid> gridUids,
        MapLoadOptions? options = null)
    {
        options ??= DefaultLoadOptions;
        gridUids = new List<EntityUid>();

        if (options.LoadMap && _mapManager.MapExists(mapId))
        {
            _logLoader.Error($"Tried to load map file {path} onto existing map {mapId} without overwriting the existing map?");
#if DEBUG
            DebugTools.Assert(false);
#endif
            return false;
        }

        throw new NotImplementedException();
    }

    public void Save(EntityUid uid, string ymlPath)
    {
        if (!Exists(uid))
        {
            _logLoader.Error($"Unable to find entity {uid} for saving.");
            return;
        }

        if (Transform(uid).MapUid == null)
        {
            _logLoader.Error($"Found invalid map for {ToPrettyString(uid)}, aborting saving.");
            return;
        }

        _logLoader.Info($"Saving entity {ToPrettyString(uid)} to {ymlPath}");

        var document = new YamlDocument(GetSaveData(uid).ToYaml());

        var resPath = new ResourcePath(ymlPath).ToRootedPath();
        _resourceManager.UserData.CreateDir(resPath.Directory);

        using var writer = _resourceManager.UserData.OpenWriteText(resPath);
        {
            var stream = new YamlStream { document };
            stream.Save(new YamlMappingFix(new Emitter(writer)), false);
        }

        _logLoader.Info($"Saved {ToPrettyString(uid)} to {ymlPath}");
    }

    public void SaveMap(MapId mapId, string ymlPath)
    {
        if (!_mapManager.MapExists(mapId))
        {
            _logLoader.Error($"Unable to find map {mapId}");
            return;
        }

        Save(_mapManager.GetMapEntityId(mapId), ymlPath);
    }

    #endregion

    #region Saving

    private MappingDataNode GetSaveData(EntityUid uid)
    {
        var data = new MappingDataNode();
        WriteMetaSection(data, uid);
        WriteTileMapSection(data);

        var entityUidMap = new Dictionary<EntityUid, int>();
        var uidEntityMap = new Dictionary<int, EntityUid>();
        var entities = new List<EntityUid>();

        _stopwatch.Restart();
        PopulateEntityList(uid, entities, uidEntityMap, entityUidMap);
        _logLoader.Debug($"Populated entity list in {_stopwatch.Elapsed}");
        WriteGridSection(data, entities);

        _stopwatch.Restart();
        WriteEntitySection(data, uidEntityMap, entityUidMap);
        _logLoader.Debug($"Wrote entity section for {entities.Count} entities in {_stopwatch.Elapsed}");
        _context.Clear();

        return data;
    }

    private void WriteMetaSection(MappingDataNode rootNode, EntityUid uid)
    {
        var meta = new MappingDataNode();
        rootNode.Add("meta", meta);
        meta.Add("format", MapFormatVersion.ToString(CultureInfo.InvariantCulture));
        // TODO: Make these values configurable.
        meta.Add("name", "DemoStation");
        meta.Add("author", "Space-Wizards");

        var xform = Transform(uid);
        var isPostInit = _mapManager.IsMapInitialized(xform.MapID);

        meta.Add("postmapinit", isPostInit ? "true" : "false");
    }

    private void WriteTileMapSection(MappingDataNode rootNode)
    {
        var tileMap = new MappingDataNode();
        rootNode.Add("tilemap", tileMap);
        foreach (var tileDefinition in _tileDefManager)
        {
            tileMap.Add(tileDefinition.TileId.ToString(CultureInfo.InvariantCulture), tileDefinition.ID);
        }
    }

    private void PopulateEntityList(EntityUid uid, List<EntityUid> entities, Dictionary<int, EntityUid> uidEntityMap, Dictionary<EntityUid, int> entityUidMap)
    {
        var withoutUid = new HashSet<EntityUid>();
        var saveCompQuery = GetEntityQuery<MapSaveIdComponent>();
        var transformCompQuery = GetEntityQuery<TransformComponent>();
        var metaCompQuery = GetEntityQuery<MetaDataComponent>();

        RecursivePopulate(uid, entities, uidEntityMap, withoutUid, metaCompQuery, transformCompQuery, saveCompQuery);

        var uidCounter = 0;
        foreach (var entity in withoutUid)
        {
            while (uidEntityMap.ContainsKey(uidCounter))
            {
                // Find next available UID.
                uidCounter += 1;
            }

            uidEntityMap.Add(uidCounter, entity);
            uidCounter += 1;
        }

        // Build a reverse lookup
        entityUidMap.EnsureCapacity(uidEntityMap.Count);
        foreach(var (saveId, mapId) in uidEntityMap)
        {
            entityUidMap.Add(mapId, saveId);
        }
    }

    private bool IsSaveable(EntityUid uid, EntityQuery<MetaDataComponent> metaQuery, EntityQuery<TransformComponent> transformQuery)
    {
        // Don't serialize things parented to un savable things.
        // For example clothes inside a person.
        while (uid.IsValid())
        {
            var meta = metaQuery.GetComponent(uid);

            if (meta.EntityDeleted || meta.EntityPrototype?.MapSavable == false) break;

            uid = transformQuery.GetComponent(uid).ParentUid;
        }

        // If we manage to get up to the map (root node) then it's saveable.
        return !uid.IsValid();
    }

    private void RecursivePopulate(EntityUid uid,
        List<EntityUid> entities,
        Dictionary<int, EntityUid> uidEntityMap,
        HashSet<EntityUid> withoutUid,
        EntityQuery<MetaDataComponent> metaQuery,
        EntityQuery<TransformComponent> transformQuery,
        EntityQuery<MapSaveIdComponent> saveCompQuery)
    {
        if (!IsSaveable(uid, metaQuery, transformQuery))
            return;

        entities.Add(uid);

        // TODO: Given there's some structure to this now we can probably omit the parent / child a bit.
        if (!saveCompQuery.TryGetComponent(uid, out var mapSaveComp) ||
            !uidEntityMap.TryAdd(mapSaveComp.Uid, uid))
        {
            // If the id was already saved before, or has no save component we need to find a new id for this entity
            withoutUid.Add(uid);
        }

        var enumerator = transformQuery.GetComponent(uid).ChildEnumerator;

        while (enumerator.MoveNext(out var child))
        {
            RecursivePopulate(child.Value, entities, uidEntityMap, withoutUid, metaQuery, transformQuery, saveCompQuery);
        }
    }

    private void WriteGridSection(MappingDataNode rootNode, List<EntityUid> entities)
    {
        // TODO: Shitcode
        var grids = new SequenceDataNode();
        rootNode.Add("grids", grids);
        var mapGridQuery = GetEntityQuery<MapGridComponent>();

        int index = 0;
        foreach (var entity in entities)
        {
            if (!mapGridQuery.TryGetComponent(entity, out var grid))
                continue;

            grid.GridIndex = index;
            var entry = _serManager.WriteValue(grid, context: _context);
            grids.Add(entry);
            index++;
        }
    }

    private void WriteEntitySection(MappingDataNode rootNode, Dictionary<int, EntityUid> uidEntityMap, Dictionary<EntityUid, int> entityUidMap)
    {
        var metaQuery = GetEntityQuery<MetaDataComponent>();
        var entities = new SequenceDataNode();
        rootNode.Add("entities", entities);

        var prototypeCompCache = new Dictionary<string, Dictionary<string, MappingDataNode>>();
        _context.Set(uidEntityMap, entityUidMap);

        foreach (var (saveId, entityUid) in uidEntityMap.OrderBy( e=> e.Key))
        {
            _context.CurrentWritingEntity = entityUid;
            var mapping = new MappingDataNode
            {
                {"uid", saveId.ToString(CultureInfo.InvariantCulture)}
            };

            var md = metaQuery.GetComponent(entityUid);

            Dictionary<string, MappingDataNode>? cache = null;

            if (md.EntityPrototype is {} prototype)
            {
                mapping.Add("type", prototype.ID);

                if (!prototypeCompCache.TryGetValue(prototype.ID, out cache))
                {
                    prototypeCompCache[prototype.ID] = cache = new Dictionary<string, MappingDataNode>(prototype.Components.Count);

                    foreach (var (compType, comp) in prototype.Components)
                    {
                        cache.Add(compType, _serManager.WriteValueAs<MappingDataNode>(comp.Component.GetType(), comp.Component));
                    }
                }
            }

            var components = new SequenceDataNode();

            // See engine#636 for why the Distinct() call.
            foreach (var component in EntityManager.GetComponents(entityUid))
            {
                if (component is MapSaveIdComponent)
                    continue;

                var compType = component.GetType();
                var compName = _factory.GetComponentName(compType);
                _context.CurrentWritingComponent = compName;
                var compMapping = _serManager.WriteValueAs<MappingDataNode>(compType, component, context: _context);

                if (cache?.TryGetValue(compName, out var protMapping) == true)
                {
                    // This will NOT recursively call Except() on the values of the mapping. It will only remove
                    // key-value pairs if both the keys and values are equal.
                    compMapping = compMapping.Except(protMapping);
                    if(compMapping == null)
                        continue;
                }

                // Don't need to write it if nothing was written! Note that if this entity has no associated
                // prototype, we ALWAYS want to write the component, because merely the fact that it exists is
                // information that needs to be written.
                if (compMapping.Children.Count != 0 || md.EntityPrototype == null)
                {
                    compMapping.Add("type", new ValueDataNode(compName));
                    // Something actually got written!
                    components.Add(compMapping);
                }
            }

            if (components.Count != 0)
            {
                mapping.Add("components", components);
            }

            entities.Add(mapping);
        }
    }

    #endregion

    private sealed record MapLoaderData
    {
        public readonly MapId TargetMap;
        public readonly MappingDataNode RootNode;

        public MapLoaderData(MappingDataNode rootNode)
        {
            RootNode = rootNode;
        }
    }

    private sealed class MapSerializationContext : ISerializationContext, IEntityLoadContext,
        ITypeSerializer<EntityUid, ValueDataNode>
    {
        private readonly IComponentFactory _factory;
        private readonly IEntityManager _entityManager;
        private readonly ISerializationManager _serializationManager;

        public Dictionary<(Type, Type), object> TypeReaders { get; }
        public Dictionary<Type, object> TypeWriters { get; }
        public Dictionary<Type, object> TypeCopiers => TypeWriters;
        public Dictionary<(Type, Type), object> TypeValidators => TypeReaders;

        // Run-specific data
        private Dictionary<string, MappingDataNode>? _currentReadingEntityComponents;
        public string? CurrentWritingComponent;
        public EntityUid? CurrentWritingEntity;
        private Dictionary<int, EntityUid> _uidEntityMap = new();
        private Dictionary<EntityUid, int> _entityUidMap = new();

        public MapSerializationContext(IComponentFactory factory, IEntityManager entityManager, ISerializationManager serializationManager)
        {
            _factory = factory;
            _entityManager = entityManager;
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
            _currentReadingEntityComponents?.Clear();
            CurrentWritingComponent = null;
            CurrentWritingEntity = null;
        }

        // Create custom object serializers that will correctly allow data to be overriden by the map file.
        MappingDataNode IEntityLoadContext.GetComponentData(string componentName,
            MappingDataNode? protoData)
        {
            if (_currentReadingEntityComponents == null)
            {
                throw new InvalidOperationException();
            }


            if (_currentReadingEntityComponents.TryGetValue(componentName, out var mapping))
            {
                if (protoData == null) return mapping.Copy();

                return _serializationManager.PushCompositionWithGenericNode(
                    _factory.GetRegistration(componentName).Type, new[] { protoData }, mapping, this);
            }

            return protoData ?? new MappingDataNode();
        }

        public IEnumerable<string> GetExtraComponentTypes()
        {
            return _currentReadingEntityComponents!.Keys;
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
}
