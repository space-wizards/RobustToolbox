using System;
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
                throw new Exception($"Received invalid map state? {ToPrettyString(uid)}");

            component.MapId = state.MapId;
            Maps.Add(component.MapId, uid);
            RecursiveMapIdUpdate(uid, uid, component.MapId);
        }

        DebugTools.AssertEqual(component.MapId, state.MapId);
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

    private void OnCompInit(EntityUid uid, MapComponent component, ComponentInit args)
    {
        if (component.MapId == MapId.Nullspace)
            component.MapId = GetNextMapId();

        DebugTools.AssertEqual(component.MapId.IsClientSide, IsClientSide(uid));
        if (!Maps.TryAdd(component.MapId, uid))
        {
            if (Maps[component.MapId] != uid)
                throw new Exception($"Attempted to initialize a map {ToPrettyString(uid)} with a duplicate map id {component.MapId}");
        }

        var msg = new MapChangedEvent(uid, component.MapId, true);
        RaiseLocalEvent(uid, msg, true);
    }

    private void OnCompStartup(EntityUid uid, MapComponent component, ComponentStartup args)
    {
        if (component.MapPaused)
            RecursiveSetPaused(uid, true);
    }

    private void OnMapRemoved(EntityUid uid, MapComponent component, ComponentShutdown args)
    {
        DebugTools.Assert(component.MapId != MapId.Nullspace);
        Maps.Remove(component.MapId);

        var msg = new MapChangedEvent(uid, component.MapId, false);
        RaiseLocalEvent(uid, msg, true);
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

        var uid = EntityManager.CreateEntityUninitialized(null);
        var map = _factory.GetComponent<MapComponent>();
        map.MapId = mapId;
        AddComp(uid, map);

        // Give the entity a name, mainly for debugging. Content can always override this with a localized name.
        var meta = MetaData(uid);
        _meta.SetEntityName(uid, $"Map Entity", meta);

        // Initialize components. this should add the map id to the collections.
        EntityManager.InitializeComponents(uid, meta);
        EntityManager.StartComponents(uid);
        DebugTools.Assert(Maps[mapId] == uid);

        if (runMapInit)
            InitializeMap((uid, map));
        else
            SetPaused((uid, map), true);

        return uid;
    }
}
