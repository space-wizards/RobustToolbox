using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    protected int LastMapId;

    private void InitializeMap()
    {
        SubscribeLocalEvent<MapComponent, ComponentAdd>(OnComponentAdd);
        SubscribeLocalEvent<MapComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<MapComponent, ComponentStartup>(OnCompStartup);
        SubscribeLocalEvent<MapComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MapComponent, ComponentShutdown>(OnMapRemoved);
        SubscribeLocalEvent<MapComponent, ComponentHandleState>(OnMapHandleState);
        SubscribeLocalEvent<MapComponent, ComponentGetState>(OnMapGetState);
    }

    public bool MapExists([NotNullWhen(true)] MapId? mapId)
    {
        return mapId != null && Maps.ContainsKey(mapId.Value);
    }

    public EntityUid GetMap(MapId mapId)
    {
        return Maps[mapId];
    }

    /// <summary>
    /// Get the entity UID for a map, or <see cref="EntityUid.Invalid"/> if the map doesn't exist.
    /// </summary>
    /// <param name="mapId">The ID of the map to look up.</param>
    /// <returns>
    /// The entity UID of the map entity with the specific map ID,
    /// or <see cref="EntityUid.Invalid"/> if the map doesn't exist.
    /// </returns>
    /// <seealso cref="GetMap"/>
    public EntityUid GetMapOrInvalid(MapId? mapId)
    {
        if (TryGetMap(mapId, out var uid))
            return uid.Value;

        return EntityUid.Invalid;
    }

    public bool TryGetMap([NotNullWhen(true)] MapId? mapId, [NotNullWhen(true)] out EntityUid? uid)
    {
        if (mapId == null || !Maps.TryGetValue(mapId.Value, out var map))
        {
            uid = null;
            return false;
        }

        uid = map;
        return true;
    }

    private void OnMapHandleState(EntityUid uid, MapComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MapComponentState state)
            return;

        if (component.MapId == MapId.Nullspace)
        {
            if (state.MapId == MapId.Nullspace)
                throw new Exception($"Received invalid map state for {ToPrettyString(uid)}");

            AssignMapId((uid, component), state.MapId);
            RecursiveMapIdUpdate(uid, uid, component.MapId);
        }

        if (component.MapId != state.MapId)
            throw new Exception($"Received invalid map state for {ToPrettyString(uid)}");

        component.LightingEnabled = state.LightingEnabled;
        component.MapInitialized = state.Initialized;

        if (LifeStage(uid) >= EntityLifeStage.Initialized)
            SetPaused(uid, state.MapPaused);
        else
            component.MapPaused = state.MapPaused;
    }

    private void RecursiveMapIdUpdate(EntityUid uid, EntityUid mapUid, MapId mapId)
    {
        // This is required only in the event where an entity becomes a map AFTER children have already been attached to it.
        // AFAIK, this currently only happens when the client applies entity states out of order (i.e., ignoring transform hierarchy),
        // which itself only happens if PVS is disabled.
        // TODO MAPS remove this

        var xform = Transform(uid);
        xform.MapUid = mapUid;
        xform.MapID = mapId;
        xform._mapIdInitialized = true;
        foreach (var child in xform._children)
        {
            RecursiveMapIdUpdate(child, mapUid, mapId);
        }
    }

    private void OnMapGetState(EntityUid uid, MapComponent component, ref ComponentGetState args)
    {
        args.State = new MapComponentState(component.MapId, component.LightingEnabled, component.MapPaused, component.MapInitialized);
    }

    protected abstract MapId GetNextMapId();

    private void OnComponentAdd(EntityUid uid, MapComponent component, ComponentAdd args)
    {
        // ordered startups when
        EnsureComp<PhysicsMapComponent>(uid);
        EnsureComp<GridTreeComponent>(uid);
        EnsureComp<MovedGridsComponent>(uid);
    }

    internal void AssignMapId(Entity<MapComponent> map, MapId? id = null)
    {
        if (map.Comp.MapId != MapId.Nullspace)
        {
            if (id != null && map.Comp.MapId != id)
            {
                QueueDel(map.Owner);
                throw new Exception($"Map entity {ToPrettyString(map.Owner)} has already been assigned an id");
            }

            if (!Maps.TryGetValue(map.Comp.MapId, out var existing) || existing != map.Owner)
            {
                QueueDel(map.Owner);
                throw new Exception($"Map entity {ToPrettyString(map.Owner)} was improperly assigned a map id?");
            }

            DebugTools.Assert(UsedIds.Contains(map.Comp.MapId));
            return;
        }

        map.Comp.MapId = id ?? GetNextMapId();

        if (IsClientSide(map) != map.Comp.MapId.IsClientSide)
            throw new Exception($"Attempting to assign a client-side map id to a networked entity or vice-versa");

        if (!UsedIds.Add(map.Comp.MapId))
            Log.Warning($"Re-using a previously used map id ({map.Comp.MapId}) for map entity {ToPrettyString(map)}");

        if (Maps.TryAdd(map.Comp.MapId, map.Owner))
            return;

        if (Maps[map.Comp.MapId] == map.Owner)
            return;

        QueueDel(map);
        throw new Exception(
            $"Attempted to assign an existing mapId {map.Comp} to a map entity {ToPrettyString(map.Owner)}");
    }

    private void OnCompInit(Entity<MapComponent> map, ref ComponentInit args)
    {
        AssignMapId(map);

#pragma warning disable CS0618 // Type or member is obsolete
        var msg = new MapChangedEvent(map, map.Comp.MapId, true);
#pragma warning restore CS0618 // Type or member is obsolete
        RaiseLocalEvent(map, msg, true);
        var ev = new MapCreatedEvent(map, map.Comp.MapId);
        RaiseLocalEvent(map, ev, true);
    }

    private void OnMapInit(EntityUid uid, MapComponent component, MapInitEvent args)
    {
        DebugTools.Assert(!component.MapInitialized);
        component.MapInitialized = true;
        Dirty(uid, component);
    }

    private void OnCompStartup(EntityUid uid, MapComponent component, ComponentStartup args)
    {
        // un-initialized maps are always paused.
        component.MapPaused |= !component.MapInitialized;

        if (!component.MapPaused)
            return;

        // Recursively pause all entities on the map
        component.MapPaused = false;
        SetPaused(uid, true);
    }

    private void OnMapRemoved(EntityUid uid, MapComponent component, ComponentShutdown args)
    {
        DebugTools.Assert(component.MapId != MapId.Nullspace);
        Maps.Remove(component.MapId);

#pragma warning disable CS0618 // Type or member is obsolete
        var msg = new MapChangedEvent(uid, component.MapId, false);
#pragma warning restore CS0618 // Type or member is obsolete
        RaiseLocalEvent(uid, msg, true);

        var ev = new MapRemovedEvent(uid, component.MapId);
        RaiseLocalEvent(uid, ev, true);
    }

    /// <summary>
    ///     Creates a new map, automatically assigning a map id.
    /// </summary>
    public EntityUid CreateMap(out MapId mapId, bool runMapInit = true)
    {
        mapId = GetNextMapId();
        var uid = CreateMap(mapId, runMapInit);
        return uid;
    }

    /// <inheritdoc cref="CreateMap(out Robust.Shared.Map.MapId,bool)"/>
    public EntityUid CreateMap(bool runMapInit = true) => CreateMap(out _, runMapInit);

    /// <summary>
    ///     Creates a new map with the specified map id.
    /// </summary>
    /// <exception cref="ArgumentException">Throws if an invalid or already existing map id is provided.</exception>
    public EntityUid CreateMap(MapId mapId, bool runMapInit = true)
    {
        if (Maps.ContainsKey(mapId))
            throw new ArgumentException($"Map with id {mapId} already exists");

        if (mapId == MapId.Nullspace)
            throw new ArgumentException($"Cannot create a null-space map");

        if (_netManager.IsServer && mapId.IsClientSide)
            throw new ArgumentException($"Attempted to create a client-side map on the server?");

        if (_netManager.IsClient && _netManager.IsConnected && !mapId.IsClientSide)
            throw new ArgumentException($"Attempted to create a client-side map entity with a non client-side map ID?");

        if (UsedIds.Contains(mapId))
            Log.Warning($"Re-using MapId: {mapId}");

        var (uid, map, meta) = CreateUninitializedMap();
        DebugTools.AssertEqual(map.MapId, MapId.Nullspace);
        AssignMapId((uid, map), mapId);

        // Initialize components. this should add the map id to the collections.
        EntityManager.InitializeEntity(uid, meta);
        EntityManager.StartEntity(uid);
        DebugTools.AssertEqual(Maps[mapId], uid);

        if (runMapInit)
            InitializeMap((uid, map));
        else
            SetPaused((uid, map), true);

        return uid;
    }

    public Entity<MapComponent, MetaDataComponent> CreateUninitializedMap()
    {
        var uid = EntityManager.CreateEntityUninitialized(null, out var meta);
        _meta.SetEntityName(uid, $"Map Entity", meta);
        return (uid, AddComp<MapComponent>(uid), meta);
    }

    public void DeleteMap(MapId mapId)
    {
        if (TryGetMap(mapId, out var uid))
            Del(uid);
    }

    public IEnumerable<MapId> GetAllMapIds()
    {
        return Maps.Keys;
    }
}
