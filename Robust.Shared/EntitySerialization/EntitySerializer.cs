using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
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

namespace Robust.Shared.EntitySerialization;

/// <summary>
/// This class provides methods for serializing entities into yaml. It provides some more control over
/// serialization than the methods provided by <see cref="MapLoaderSystem"/>.
/// </summary>
/// <remarks>
/// There are several methods (e.g., <see cref="SerializeEntityRecursive"/> that serialize entities into a
/// per-entity <see cref="MappingDataNode"/> stored in the <see cref="EntityData"/> dictionary, which is indexed by the
/// entity's assigned yaml id (see <see cref="GetYamlUid"/>. The generated data can then be written to a larger yaml
/// document using the various "Write" methods. (e.g., <see cref="WriteEntitySection"/>). After a one has finished using
/// the generated data, the serializer needs to be reset (<see cref="Reset"/>) using it again to serialize other entities.
/// </remarks>
public sealed class EntitySerializer : ISerializationContext,
    ITypeSerializer<EntityUid, ValueDataNode>,
    ITypeSerializer<NetEntity, ValueDataNode>
{
    public const int MapFormatVersion = 7;
    // v6->v7: PR #5572 - Added more metadata, List maps/grids/orphans, include some life-stage information
    // v5->v6: PR #4307 - Converted Tile.TypeId from ushort to int
    // v4->v5: PR #3992 - Removed name & author fields
    // v3->v4: PR #3913 - Grouped entities by prototype
    // v2->v3: PR #3468

    public SerializationManager.SerializerProvider SerializerProvider { get; } = new();

    [Dependency] public readonly EntityManager EntMan = default!;
    [Dependency] public readonly IGameTiming Timing = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
    [Dependency] private readonly IConfigurationManager _conf = default!;
    [Dependency] private readonly ILogManager _logMan = default!;

    private readonly ISawmill _log;
    public readonly Dictionary<EntityUid, int> YamlUidMap = new();
    public readonly HashSet<int> YamlIds = new();


    public string? CurrentComponent { get; private set; }
    public Entity<MetaDataComponent>? CurrentEntity { get; private set; }
    public int CurrentEntityYamlUid { get; private set; }

    /// <summary>
    /// Tile ID -> yaml tile ID mapping.
    /// </summary>
    private readonly Dictionary<int, int> _tileMap = new();
    private readonly HashSet<int> _yamlTileIds = new();

    /// <inheritdoc/>
    public bool WritingReadingPrototypes { get; private set; }

    /// <summary>
    /// If set, the serializer will refuse to serialize the given entity and will orphan any entity that is parented to
    /// it. This is useful for serializing things like a grid (or multiple grids & entities) that are parented to a map
    /// without actually serializing the map itself.
    /// </summary>
    public EntityUid Truncate { get; private set; }

    /// <summary>
    /// List of all entities that have previously been ignored via <see cref="Truncate"/>.
    /// </summary>
    /// <remarks>
    /// This is tracked in case somebody does something weird, like trying to save a grid w/o its map, and then later on
    /// including the map in the file. AFAIK, that should work in principle, though it would lead to a weird file where
    /// the grid is orphaned and not on the map where it should be.
    /// </remarks>
    public readonly HashSet<EntityUid> Truncated = new();

    public readonly SerializationOptions Options;

    /// <summary>
    /// Cached prototype data. This is used to avoid writing redundant data that is already specified in an entity's
    /// prototype.
    /// </summary>
    public readonly Dictionary<string, Dictionary<string, MappingDataNode>> PrototypeCache = new();

    /// <summary>
    /// The serialized entity data.
    /// </summary>
    public readonly Dictionary<int, (EntityUid Uid, MappingDataNode Node)> EntityData = new();

    /// <summary>
    /// <see cref="EntityData"/> indices grouped by their entity prototype ids.
    /// </summary>
    public readonly Dictionary<string, List<int>> Prototypes = new();

    /// <summary>
    /// Yaml ids of all serialized map entities.
    /// </summary>
    public readonly List<int> Maps = new();

    /// <summary>
    /// Yaml ids of all serialized null-space entities.
    /// This only includes entities that were initially in null-space, it does not include entities that were
    /// serialized without their parents. Those are in <see cref="Orphans"/>.
    /// </summary>
    public readonly List<int> Nullspace = new();

    /// <summary>
    /// Yaml ids of all serialized grid entities.
    /// </summary>
    public readonly List<int> Grids = new();

    /// <summary>
    /// Yaml ids of all serialized entities in the file whose parents were not serialized. This does not include
    /// entities that did not have a parent (e.g., maps or null-space entities). I.e., these are the entities that
    /// need to be attached to a new parent when loading the file, unless you want to load them into null-space.
    /// </summary>
    public readonly List<int> Orphans = new();

    private readonly string _metaName;
    private readonly string _xformName;
    private readonly MappingDataNode _emptyMetaNode;
    private readonly MappingDataNode _emptyXformNode;
    private int _nextYamlUid = 1;
    private int _nextYamlTileId;

    private readonly List<EntityUid> _autoInclude = new();
    private readonly EntityQuery<YamlUidComponent> _yamlQuery;
    private readonly EntityQuery<MapGridComponent> _gridQuery;
    private readonly EntityQuery<MapComponent> _mapQuery;
    private readonly EntityQuery<MetaDataComponent> _metaQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;

    /// <summary>
    /// C# event for checking whether an entity is serializable. Can be used by content to prevent specific entities
    /// from getting serialized.
    /// </summary>
    public event IsSerializableDelegate? OnIsSerializeable;
    public delegate void IsSerializableDelegate(Entity<MetaDataComponent> ent, ref bool serializable);

    public EntitySerializer(IDependencyCollection dependency, SerializationOptions options)
    {
        dependency.InjectDependencies(this);

        _log = _logMan.GetSawmill("entity_serializer");
        SerializerProvider.RegisterSerializer(this);

        _metaName = _factory.GetComponentName(typeof(MetaDataComponent));
        _xformName = _factory.GetComponentName(typeof(TransformComponent));
        _emptyMetaNode = _serialization.WriteValueAs<MappingDataNode>(typeof(MetaDataComponent), new MetaDataComponent(), alwaysWrite: true, context: this);

        CurrentComponent = _xformName;
        _emptyXformNode = _serialization.WriteValueAs<MappingDataNode>(typeof(TransformComponent), new TransformComponent(), alwaysWrite: true, context: this);
        CurrentComponent = null;

        _yamlQuery = EntMan.GetEntityQuery<YamlUidComponent>();
        _gridQuery = EntMan.GetEntityQuery<MapGridComponent>();
        _mapQuery = EntMan.GetEntityQuery<MapComponent>();
        _metaQuery = EntMan.GetEntityQuery<MetaDataComponent>();
        _xformQuery = EntMan.GetEntityQuery<TransformComponent>();
        Options = options;
    }

    public bool IsSerializable(Entity<MetaDataComponent?> ent)
    {
        if (ent.Comp == null && !EntMan.TryGetComponent(ent.Owner, out ent.Comp))
            return false;

        if (ent.Comp.EntityPrototype?.MapSavable == false)
            return false;

        bool serializable = true;
        OnIsSerializeable?.Invoke(ent!, ref serializable);
        return serializable;
    }

    #region Serialize API

    /// <summary>
    /// Serialize a single entity. This does not automatically include
    /// children, though depending on the setting of <see cref="SerializationOptions.MissingEntityBehaviour"/> it may
    /// auto-include additional entities aside from the one provided.
    /// </summary>
    public void SerializeEntity(EntityUid uid)
    {
        if (!IsSerializable(uid))
            throw new Exception($"{EntMan.ToPrettyString(uid)} is not serializable");

        DebugTools.AssertNull(CurrentEntity);
        ReserveYamlId(uid);
        SerializeEntityInternal(uid);
        DebugTools.AssertNull(CurrentEntity);
        if (_autoInclude.Count != 0)
            ProcessAutoInclude();
    }

    /// <summary>
    /// Serialize a set of entities. This does not automatically include children or parents, though depending on the
    /// setting of <see cref="SerializationOptions.MissingEntityBehaviour"/> it may auto-include additional entities
    /// aside from the one provided.
    /// </summary>
    public void SerializeEntities(HashSet<EntityUid> entities)
    {
        foreach (var uid in entities)
        {
            if (!IsSerializable(uid))
                throw new Exception($"{EntMan.ToPrettyString(uid)} is not serializable");
        }

        ReserveYamlIds(entities);
        SerializeEntitiesInternal(entities);
    }

    /// <summary>
    /// Serializes an entity and all of its serializable children. Note that this will not automatically serialize the
    /// entity's parents.
    /// </summary>
    public void SerializeEntityRecursive(EntityUid root)
    {
        if (!IsSerializable(root))
            throw new Exception($"{EntMan.ToPrettyString(root)} is not serializable");

        Truncate = _xformQuery.GetComponent(root).ParentUid;
        Truncated.Add(Truncate);
        InitializeTileMap(root);
        HashSet<EntityUid> entities = new();
        RecursivelyIncludeChildren(root, entities);
        ReserveYamlIds(entities);
        SerializeEntitiesInternal(entities);
        Truncate = EntityUid.Invalid;
    }

    #endregion

    /// <summary>
    /// Initialize the <see cref="_tileMap"/> that is used to serialize grid chunks using
    /// <see cref="MapChunkSerializer"/>. This initialization just involves checking to see if any of the entities being
    /// serialized were previously deserialized. If they were, it will re-use the old tile map. This is not actually required,
    /// and is just meant to prevent large map file diffs when the internal tile ids change. I.e., you can serialize entities
    /// without initializing the tile map.
    /// </summary>
    private void InitializeTileMap(EntityUid root)
    {
        if (!FindSavedTileMap(root, out var savedMap))
            return;

        // Note: some old maps were saved with duplicate id strings.
        // I.e, multiple integers that correspond to the same prototype id.
        // Hence the TryAdd()
        //
        // Though now we also need to use TryAdd in case InitializeTileMap() is called multiple times.
        // E.g., if different grids get added separately to a single save file, in which case the
        // tile map may already be partially populated.
        foreach (var (origId, prototypeId) in savedMap)
        {
            if (_tileDef.TryGetDefinition(prototypeId, out var definition))
            {
                _tileMap.TryAdd(definition.TileId, origId);
                _yamlTileIds.Add(origId); // Make sure we record the IDs we're using so when we need to reserve new ones we can
            }
        }
    }

    private bool FindSavedTileMap(EntityUid root, [NotNullWhen(true)] out Dictionary<int, string>? map)
    {
        // Try and fetch the mapping directly
        if (EntMan.TryGetComponent(root, out MapSaveTileMapComponent? comp))
        {
            map = comp.TileMap;
            return true;
        }

        // iterate over all of its children and grab the first grid with a mapping
        var xform = _xformQuery.GetComponent(root);
        foreach (var child in xform._children)
        {
            if (!EntMan.TryGetComponent(child, out MapSaveTileMapComponent? cComp))
                continue;
            map = cComp.TileMap;
            return true;
        }

        map = null;
        return false;
    }

    #region AutoInclude

    private void ProcessAutoInclude()
    {
        DebugTools.AssertEqual(_autoInclude.ToHashSet().Count, _autoInclude.Count);

        var ents = new HashSet<EntityUid>();

        switch (Options.MissingEntityBehaviour)
        {
            case MissingEntityBehaviour.PartialInclude:
                // Include the entity and any of its direct parents
                foreach (var uid in _autoInclude)
                {
                    RecursivelyIncludeParents(uid, ents);
                }
                break;
            case MissingEntityBehaviour.IncludeNullspace:
            case MissingEntityBehaviour.AutoInclude:
                // Find the root transform of all the included entities
                var roots = new HashSet<EntityUid>();
                foreach (var uid in _autoInclude)
                {
                    GetRootNode(uid, roots);
                }

                // Recursively include all children of these root nodes.
                foreach (var root in roots)
                {
                    RecursivelyIncludeChildren(root, ents);
                }
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        _autoInclude.Clear();
        SerializeEntitiesInternal(ents);
    }

    private void RecursivelyIncludeChildren(EntityUid uid, HashSet<EntityUid> ents)
    {
        if (!IsSerializable(uid))
            return;

        ents.Add(uid);
        var xform = _xformQuery.GetComponent(uid);
        foreach (var child in xform._children)
        {
            RecursivelyIncludeChildren(child, ents);
        }
    }

    private void GetRootNode(EntityUid uid, HashSet<EntityUid> ents)
    {
        if (!IsSerializable(uid))
            throw new NotSupportedException($"Attempted to auto-include an unserializable entity: {EntMan.ToPrettyString(uid)}");

        var xform = _xformQuery.GetComponent(uid);
        while (xform.ParentUid.IsValid() && xform.ParentUid != Truncate)
        {
            uid = xform.ParentUid;
            xform = _xformQuery.GetComponent(uid);

            if (!IsSerializable(uid))
                throw new NotSupportedException($"Encountered an un-serializable parent entity: {EntMan.ToPrettyString(uid)}");
        }

        ents.Add(uid);
    }

    private void RecursivelyIncludeParents(EntityUid uid, HashSet<EntityUid> ents)
    {
        while (uid.IsValid() && uid != Truncate)
        {
            if (!ents.Add(uid))
                break;

            if (!IsSerializable(uid))
                throw new NotSupportedException($"Encountered an un-serializable parent entity: {EntMan.ToPrettyString(uid)}");

            uid = _xformQuery.GetComponent(uid).ParentUid;
        }
    }

    #endregion

    private void SerializeEntitiesInternal(HashSet<EntityUid> entities)
    {
        foreach (var uid in entities)
        {
            DebugTools.AssertNull(CurrentEntity);
            SerializeEntityInternal(uid);
        }

        DebugTools.AssertNull(CurrentEntity);
        if (_autoInclude.Count != 0)
            ProcessAutoInclude();
    }

    /// <summary>
    /// Serialize a single entity, and store the results in <see cref="EntityData"/>.
    /// </summary>
    private void SerializeEntityInternal(EntityUid uid)
    {
        var saveId = GetYamlUid(uid);
        DebugTools.Assert(!EntityData.ContainsKey(saveId));

        // It might be possible that something could cause an entity to be included twice.
        // E.g., if someone serializes a grid w/o its map, and then tries to separately include the map and all its children.
        // In that case, the grid would already have been serialized as a orphan.
        // uhhh.... I guess its fine?
        if (EntityData.ContainsKey(saveId))
            return;

        var meta = _metaQuery.GetComponent(uid);
        var protoId = meta.EntityPrototype?.ID ?? string.Empty;

        switch (meta.EntityLifeStage)
        {
            case <= EntityLifeStage.Initializing:
                _log.Error($"Encountered an uninitialized entity: {EntMan.ToPrettyString(uid)}");
                break;
            case >= EntityLifeStage.Terminating:
                _log.Error($"Encountered terminating or deleted entity: {EntMan.ToPrettyString(uid)}");
                break;
        }

        CurrentEntityYamlUid = saveId;
        CurrentEntity = (uid, meta);

        Prototypes.GetOrNew(protoId).Add(saveId);
        var xform = _xformQuery.GetComponent(uid);

        if (_mapQuery.HasComp(uid))
            Maps.Add(saveId);
        else if (xform.ParentUid == EntityUid.Invalid)
            Nullspace.Add(saveId);

        if (_gridQuery.HasComp(uid))
        {
            // The current assumption is that grids cannot be in null-space, because the rest of the code
            // (broadphase, etc) don't support grids without maps.
            DebugTools.Assert(xform.ParentUid != EntityUid.Invalid || _mapQuery.HasComp(uid));
            Grids.Add(saveId);
        }

        var entData = new MappingDataNode
        {
            {"uid", saveId.ToString(CultureInfo.InvariantCulture)}
        };

        EntityData[saveId] = (uid, entData);
        var cache = GetProtoCache(meta.EntityPrototype);

        // Store information about whether a given entity has been map-initialized.
        // In principle, if a map has been map-initialized, then all entities on that map should also be map-initialized.
        // But technically there is nothing that prevents someone from moving a post-init entity onto a pre-init map and vice-versa.
        // Also, we need to record this information even if the map is not being serialized.
        // In 99% of cases, this data is probably redundant and just bloats the file, but I can't think of a better way of handling it.
        // At least it should only bloat post-init maps, which aren't really getting used so far.
        if (meta.EntityLifeStage == EntityLifeStage.MapInitialized)
        {
            if (Options.ExpectPreInit)
                _log.Error($"Expected all entities to be pre-mapinit, but encountered post-init entity: {EntMan.ToPrettyString(uid)}");
            entData.Add("mapInit", "true");

            // If an entity has been map-initialized, we assume it is un-paused.
            // If it is paused, we have to specify it.
            if (meta.EntityPaused)
                entData.Add("paused", "true");
        }
        else
        {
            // If an entity has not yet been map-initialized, we assume it is paused.
            // I don't know in what situations it wouldn't be, but might as well future proof this.
            if (!meta.EntityPaused)
                entData.Add("paused", "false");
        }

        var components = new SequenceDataNode();
        if (xform.NoLocalRotation && xform.LocalRotation != 0)
        {
            _log.Error($"Encountered a no-rotation entity with non-zero local rotation: {EntMan.ToPrettyString(uid)}");
            xform._localRotation = 0;
        }

        foreach (var component in EntMan.GetComponentsInternal(uid))
        {
            var compType = component.GetType();

            var reg = _factory.GetRegistration(compType);
            if (reg.Unsaved)
                continue;

            CurrentComponent = reg.Name;
            MappingDataNode? compMapping;
            MappingDataNode? protoMapping = null;
            if (cache != null && cache.TryGetValue(reg.Name, out protoMapping))
            {
                // If this has a prototype, we need to use alwaysWrite: true.
                // E.g., an anchored prototype might have anchored: true. If we we are saving an un-anchored
                // instance of this entity, and if we have alwaysWrite: false, then compMapping would not include
                // the anchored data-field (as false is the default for this bool data field), so the entity would
                // implicitly be saved as anchored.
                compMapping = _serialization.WriteValueAs<MappingDataNode>(compType, component, alwaysWrite: true, context: this);

                // This will not recursively call Except() on the values of the mapping. It will only remove
                // key-value pairs if both the keys and values are equal.
                compMapping = compMapping.Except(protoMapping);
                if(compMapping == null)
                    continue;
            }
            else
            {
                compMapping = _serialization.WriteValueAs<MappingDataNode>(compType, component, alwaysWrite: false, context: this);
            }

            // Don't need to write it if nothing was written! Note that if this entity has no associated
            // prototype, we ALWAYS want to write the component, because merely the fact that it exists is
            // information that needs to be written.
            if (compMapping.Children.Count != 0 || protoMapping == null)
            {
                compMapping.InsertAt(0, "type", new ValueDataNode(reg.Name));
                components.Add(compMapping);
            }
        }

        CurrentComponent = null;
        if (components.Count != 0)
            entData.Add("components", components);

        // TODO ENTITY SERIALIZATION
        // Consider adding a Action<EntityUid, MappingDataNode>? OnEntitySerialized
        // I.e., allow content to modify the per-entity data? I don't know if that would actually be useful, as content
        // could just as easily append a separate entity dictionary to the output that has the extra per-entity data they
        // want to serialize.

        if (meta.EntityPrototype == null)
        {
            CurrentEntityYamlUid = 0;
            CurrentEntity = null;
            return;
        }

        // an entity may have less components than the original prototype, so we need to check if any are missing.
        SequenceDataNode? missingComponents = null;
        foreach (var (name, comp) in meta.EntityPrototype.Components)
        {
            // try comp instead of has-comp as it checks whether the component is supposed to have been
            // deleted.
            if (EntMan.TryGetComponent(uid, comp.Component.GetType(), out _))
                continue;

            missingComponents ??= new();
            missingComponents.Add(new ValueDataNode(name));
        }

        if (missingComponents != null)
            entData.Add("missingComponents", missingComponents);

        CurrentEntityYamlUid = 0;
        CurrentEntity = null;
    }

    private Dictionary<string, MappingDataNode>? GetProtoCache(EntityPrototype? proto)
    {
        if (proto == null)
            return null;

        if (PrototypeCache.TryGetValue(proto.ID, out var cache))
            return cache;

        PrototypeCache[proto.ID] = cache = new(proto.Components.Count);
        WritingReadingPrototypes = true;

        foreach (var (compName, comp) in proto.Components)
        {
            CurrentComponent = compName;
            cache.Add(compName, _serialization.WriteValueAs<MappingDataNode>(comp.Component.GetType(), comp.Component, alwaysWrite: true, context: this));
        }

        CurrentComponent = null;
        WritingReadingPrototypes = false;
        cache.TryAdd(_metaName, _emptyMetaNode);
        cache.TryAdd(_xformName, _emptyXformNode);
        return cache;
    }

    #region Write

    public MappingDataNode Write()
    {
        DebugTools.AssertEqual(Maps.ToHashSet().Count, Maps.Count, "Duplicate maps?");
        DebugTools.AssertEqual(Grids.ToHashSet().Count, Grids.Count, "Duplicate grids?");
        DebugTools.AssertEqual(Orphans.ToHashSet().Count, Orphans.Count, "Duplicate orphans?");
        DebugTools.AssertEqual(Nullspace.ToHashSet().Count, Nullspace.Count, "Duplicate nullspace?");

        return new MappingDataNode
        {
            {"meta", WriteMetadata()},
            {"maps", WriteIds(Maps)},
            {"grids", WriteIds(Grids)},
            {"orphans", WriteIds(Orphans)},
            {"nullspace", WriteIds(Nullspace)},
            {"tilemap", WriteTileMap()},
            {"entities", WriteEntitySection()},
        };
    }

    public MappingDataNode WriteMetadata()
    {
        return new MappingDataNode
        {
            {"format", MapFormatVersion.ToString(CultureInfo.InvariantCulture)},
            {"category", GetCategory().ToString()},
            {"engineVersion", _conf.GetCVar(CVars.BuildEngineVersion) },
            {"forkId", _conf.GetCVar(CVars.BuildForkId)},
            {"forkVersion", _conf.GetCVar(CVars.BuildVersion)},
            {"time", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)},
            {"entityCount", EntityData.Count.ToString(CultureInfo.InvariantCulture)}
        };
    }

    public SequenceDataNode WriteIds(List<int> ids)
    {
        var result = new SequenceDataNode();
        foreach (var id in ids)
        {
            result.Add(new ValueDataNode(id.ToString(CultureInfo.InvariantCulture)));
        }
        return result;
    }

    /// <summary>
    /// Serialize the <see cref="_tileMap"/> to yaml. This data is required to deserialize any serialized grid chunks using <see cref="MapChunkSerializer"/>.
    /// </summary>
    public MappingDataNode WriteTileMap()
    {
        var map = new MappingDataNode();
        foreach (var (tileId, yamlTileId) in _tileMap.OrderBy(x => x.Key))
        {
            // This can come up if tests try to serialize test maps with custom / placeholder tile ids without registering them with the tile def manager..
            if (!_tileDef.TryGetDefinition(tileId, out var def))
                throw new Exception($"Attempting to serialize a tile {tileId} with no valid tile definition.");

            var yamlId = yamlTileId.ToString(CultureInfo.InvariantCulture);
            map.Add(yamlId, def.ID);
        }
        return map;
    }

    public SequenceDataNode WriteEntitySection()
    {
        if (YamlIds.Count != YamlUidMap.Count || YamlIds.Count != EntityData.Count)
        {
            // Maybe someone reserved a yaml id with ReserveYamlId() or implicitly with GetId() without actually
            // ever serializing the entity, This can lead to references to non-existent entities.
            throw new Exception($"Entity count mismatch");
        }

        var prototypes = new SequenceDataNode();
        var protos = Prototypes.Keys.ToList();
        protos.Sort(StringComparer.InvariantCulture);

        foreach (var protoId in protos)
        {
            var entities = new SequenceDataNode();
            var node = new MappingDataNode
            {
                { "proto", protoId },
                { "entities", entities},
            };

            prototypes.Add(node);

            var saveIds = Prototypes[protoId];
            saveIds.Sort();
            foreach (var saveId in saveIds)
            {
                var entData = EntityData[saveId].Node;
                entities.Add(entData);
            }
        }

        return prototypes;
    }

    /// <summary>
    /// Get the category that the serialized data belongs to. If one was specified in the
    /// <see cref="SerializationOptions"/> it will use that after validating it, otherwise it will attempt to infer a
    /// category.
    /// </summary>
    public FileCategory GetCategory()
    {
        switch (Options.Category)
        {
            case FileCategory.Save:
                return FileCategory.Save;

            case FileCategory.Map:
                return Maps.Count == 1 ? FileCategory.Map : FileCategory.Unknown;

            case FileCategory.Grid:
                if (Maps.Count > 0 || Grids.Count != 1)
                    return FileCategory.Unknown;
                return FileCategory.Grid;

            case FileCategory.Entity:
                if (Maps.Count > 0 || Grids.Count > 0 || Orphans.Count != 1)
                    return FileCategory.Unknown;
                return FileCategory.Entity;

            default:
                if (Maps.Count == 1)
                {
                    // Contains a single map, and no orphaned entities that need reparenting.
                    if (Orphans.Count == 0)
                        return FileCategory.Map;
                }
                else if (Grids.Count == 1)
                {
                    // Contains a single orphaned grid.
                    if (Orphans.Count == 1 && Grids[0] == Orphans[0])
                        return FileCategory.Grid;
                }
                else if (Orphans.Count == 1)
                {
                    // A lone orphaned entity.
                    return FileCategory.Entity;
                }

                return FileCategory.Unknown;
        }
    }

    #endregion

    #region YamlIds

    /// <summary>
    /// Get (or allocate) the integer id that will be used in the serialized file to refer to the given entity.
    /// </summary>
    public int GetYamlUid(EntityUid uid)
    {
        return !YamlUidMap.TryGetValue(uid, out var id) ? AllocateYamlUid(uid) : id;
    }

    private int AllocateYamlUid(EntityUid uid)
    {
        if (Truncated.Contains(uid))
        {
            _log.Error(
                "Including a previously truncated entity within the serialization process? Something probably wrong");
        }

        DebugTools.Assert(!YamlUidMap.ContainsKey(uid));
        while (!YamlIds.Add(_nextYamlUid))
        {
            _nextYamlUid++;
        }

        YamlUidMap.Add(uid, _nextYamlUid);
        return _nextYamlUid++;
    }

    /// <summary>
    /// Get (or allocate) the integer id that will be used in the serialized file to refer to the given grid tile id.
    /// </summary>
    public int GetYamlTileId(int tileId)
    {
        if (_tileMap.TryGetValue(tileId, out var yamlId))
            return yamlId;

        return AllocateYamlTileId(tileId);
    }

    private int AllocateYamlTileId(int tileId)
    {
        while (!_yamlTileIds.Add(_nextYamlTileId))
        {
            _nextYamlTileId++;
        }

        _tileMap[tileId] = _nextYamlTileId;
        return _nextYamlTileId++;
    }

    /// <summary>
    /// This method ensures that the given entities have a yaml ids assigned. If the entities have a
    /// <see cref="YamlUidComponent"/>, they will attempt to use that id, which exists to prevent large map file diffs
    /// due to changing yaml ids.
    /// </summary>
    public void ReserveYamlIds(HashSet<EntityUid> entities)
    {
        List<EntityUid> needIds = new();
        foreach (var uid in entities)
        {
            if (YamlUidMap.ContainsKey(uid))
                continue;

            if (_yamlQuery.TryGetComponent(uid, out var comp) && comp.Uid > 0 && YamlIds.Add(comp.Uid))
            {
                if (Truncated.Contains(uid))
                {
                    _log.Error(
                        "Including a previously truncated entity within the serialization process? Something probably wrong");
                }

                YamlUidMap.Add(uid, comp.Uid);
            }
            else
            {
                needIds.Add(uid);
            }
        }

        foreach (var uid in needIds)
        {
            AllocateYamlUid(uid);
        }
    }

    /// <summary>
    /// This method ensures that the given entity has a yaml id assigned to it. If the entity has a
    /// <see cref="YamlUidComponent"/>, it will attempt to use that id, which exists to prevent large map file diffs due
    /// to changing yaml ids.
    /// </summary>
    public void ReserveYamlId(EntityUid uid)
    {
        if (YamlUidMap.ContainsKey(uid))
            return;

        if (_yamlQuery.TryGetComponent(uid, out var comp) && comp.Uid > 0 && YamlIds.Add(comp.Uid))
        {
            if (Truncated.Contains(uid))
            {
                _log.Error(
                    "Including a previously truncated entity within the serialization process? Something probably wrong");
            }

            YamlUidMap.Add(uid, comp.Uid);
        }
        else
            AllocateYamlUid(uid);
    }

    #endregion

    #region ITypeSerializer

    ValidationNode ITypeValidator<EntityUid, ValueDataNode>.Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context)
    {
        if (node.Value == "invalid")
            return new ValidatedValueNode(node);

        if (!int.TryParse(node.Value, out _))
            return new ErrorNode(node, "Invalid EntityUid");

        return new ValidatedValueNode(node);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        EntityUid value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        if (YamlUidMap.TryGetValue(value, out var yamlId))
            return new ValueDataNode(yamlId.ToString(CultureInfo.InvariantCulture));

        if (CurrentComponent == _xformName)
        {
            if (value == EntityUid.Invalid)
                return new ValueDataNode("invalid");

            DebugTools.Assert(!Orphans.Contains(CurrentEntityYamlUid));
            Orphans.Add(CurrentEntityYamlUid);

            if (Options.ErrorOnOrphan && CurrentEntity != null && value != Truncate)
                _log.Error($"Serializing entity {EntMan.ToPrettyString(CurrentEntity)} without including its parent {EntMan.ToPrettyString(value)}");

            return new ValueDataNode("invalid");
        }

        if (value == EntityUid.Invalid)
        {
            if (Options.MissingEntityBehaviour != MissingEntityBehaviour.Ignore)
                _log.Error($"Encountered an invalid entityUid reference.");

            return new ValueDataNode("invalid");
        }

        if (value == Truncate)
        {
            _log.Error(
                $"{EntMan.ToPrettyString(CurrentEntity)}:{CurrentComponent} is attempting to serialize references to a truncated entity {EntMan.ToPrettyString(Truncate)}.");
        }

        switch (Options.MissingEntityBehaviour)
        {
            case MissingEntityBehaviour.Error:
                _log.Error(EntMan.Deleted(value)
                    ? $"Encountered a reference to a deleted entity {value} while serializing {EntMan.ToPrettyString(CurrentEntity)}."
                    : $"Encountered a reference to a missing entity: {value} while serializing {EntMan.ToPrettyString(CurrentEntity)}.");
                return new ValueDataNode("invalid");
            case MissingEntityBehaviour.Ignore:
                return new ValueDataNode("invalid");
            case MissingEntityBehaviour.IncludeNullspace:
                if (!EntMan.TryGetComponent(value, out TransformComponent? xform)
                    || xform.ParentUid != EntityUid.Invalid
                    || _gridQuery.HasComp(value)
                    || _mapQuery.HasComp(value))
                {
                    goto case MissingEntityBehaviour.Error;
                }
                goto case MissingEntityBehaviour.AutoInclude;
            case MissingEntityBehaviour.PartialInclude:
            case MissingEntityBehaviour.AutoInclude:
                if (Options.LogAutoInclude is {} level)
                    _log.Log(level, $"Auto-including entity {EntMan.ToPrettyString(value)} referenced by {EntMan.ToPrettyString(CurrentEntity)}");
                _autoInclude.Add(value);
                var id = GetYamlUid(value);
                return new ValueDataNode(id.ToString(CultureInfo.InvariantCulture));
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    EntityUid ITypeReader<EntityUid, ValueDataNode>.Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context,
        ISerializationManager.InstantiationDelegate<EntityUid>? _)
    {
        return node.Value == "invalid" ? EntityUid.Invalid : EntityUid.Parse(node.Value);
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (node.Value == "invalid")
            return new ValidatedValueNode(node);

        if (!int.TryParse(node.Value, out _))
            return new ErrorNode(node, "Invalid NetEntity");

        return new ValidatedValueNode(node);
    }

    public NetEntity Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<NetEntity>? instanceProvider = null)
    {
        return node.Value == "invalid" ? NetEntity.Invalid : NetEntity.Parse(node.Value);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        NetEntity value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var uid = EntMan.GetEntity(value);
        return serializationManager.WriteValue(uid, alwaysWrite, context);
    }

    #endregion
}
