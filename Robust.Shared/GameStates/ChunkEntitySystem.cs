using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.EntitySerialization;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map.Events;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Shared.GameStates;

/// <summary>
/// Manages nullspace entities that are treated as members of map/grid PVS chunks.
/// This allows you to store data at a chunk-level on maps / grids and have them streamed in/out without
/// manually handling it. These chunks will also never be returned by EntityLookupSystem.
/// </summary>
/// <remarks>
/// You will need to handle <see cref="PostGridSplitEvent"/> yourself.
/// </remarks>
public sealed partial class ChunkEntitySystem : EntitySystem
{
    public const int ChunkSize = MapGridComponent.DefaultChunkSize;
    private static readonly EntProtoId ChunkEntityPrototype = "ChunkEntity";

    [Dependency] private EntityQuery<ChunkEntityComponent> _chentQuery;
    [Dependency] private EntityQuery<MetaDataComponent> _metaQuery;
    [Dependency] private EntityQuery<MapComponent> _mapQuery;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery;
    [Dependency] private EntityQuery<PvsChunkContainerComponent> _containerQuery;

    /// <summary>
    /// Temporary list for serialization.
    /// </summary>
    private readonly List<PvsChunkContainerComponent> _serializationContainers = new();

    private readonly List<EntityUid> _tempUids = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MapCreatedEvent>(OnMapCreated);
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInitialize);
        SubscribeLocalEvent<ChunkEntityComponent, ComponentStartup>(OnChunkStartup);
        SubscribeLocalEvent<ChunkEntityComponent, ComponentRemove>(OnChunkShutdown);
        SubscribeLocalEvent<ChunkEntityComponent, EntityTerminatingEvent>(OnChunkTerminating);
        SubscribeLocalEvent<ChunkEntityComponent, AfterAutoHandleStateEvent>(OnChunkHandleState);
        SubscribeLocalEvent<BeforeSerializationEvent>(OnBeforeSerialization);
        SubscribeLocalEvent<MapRemovedEvent>(OnMapRemoved);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
    }

    /// <summary>
    /// Will return the relevant chunk for the specified position. Assumes normalized chunk origin i.e. position divided by ChunkSize.
    /// </summary>
    public Entity<ChunkEntityComponent> GetOrCreateChunk(EntityUid root, Vector2i chunk)
    {
        if (TryGetChunk(root, chunk, out var existing))
        {
            return existing.Value;
        }

        if (!_mapQuery.HasComp(root) && !_gridQuery.HasComp(root))
            throw new ArgumentException($"Chunk entity root {ToPrettyString(root)} is not a map or grid.", nameof(root));

        var uid = EntityManager.PredictedSpawn(ChunkEntityPrototype);

        // TODO: compreg / faster addcomp
        var comp = new ChunkEntityComponent
        {
            Root = root,
            Chunk = chunk,
        };
        AddComp(uid, comp);

        return (uid, comp);
    }

    /// <summary>
    /// Returns a chunk for the specified position if one exists. Assumes normalized chunk origin i.e. position divided by ChunkSize.
    /// </summary>
    public bool TryGetChunk(EntityUid root, Vector2i chunk, [NotNullWhen(true)] out Entity<ChunkEntityComponent>? entity)
    {
        if (_containerQuery.TryGetComponent(root, out var container) &&
            container.Chunks.TryGetValue(chunk, out var existing) &&
            IsAvailable(existing))
        {
            entity = (existing.Owner, existing.Comp);
            return true;
        }

        entity = null;
        return false;
    }

    /// <summary>
    /// Returns all known, attached chunk entities for the specified root.
    /// </summary>
    public ChunkEntityRootEnumerator GetChunks(EntityUid root)
    {
        _containerQuery.TryGetComponent(root, out var container);
        return new ChunkEntityRootEnumerator(this, container);
    }

    /// <summary>
    /// Flags a chunk that it should try to be deleted. Will fail if any other components exist on it.
    /// </summary>
    /// <param name="chunk"></param>
    public bool TryRemoveChunk(Entity<ChunkEntityComponent, MetaDataComponent?> chunk)
    {
        var meta = chunk.Comp2;

        if (!_metaQuery.Resolve(chunk.Owner, ref meta))
        {
            return false;
        }

        if (Deleted(chunk.Owner, metaData: meta))
            return true;

        // Still has hanging components.
        if (EntityManager.ComponentCount(chunk.Owner) > 3)
            return false;

        Del(chunk.Owner, meta);
        return true;
    }

    // Returns chunk entities in range of the position, assumes non-normalized inputs.
    public ChunkEntityEnumerator GetChunksInRange(EntityUid root, Vector2 localPosition, float range)
    {
        return new ChunkEntityEnumerator(this, root, new ChunkIndicesEnumerator(localPosition, range, ChunkSize));
    }

    // Returns chunk entities intersecting a local bounding box, assumes non-normalized inputs.
    public ChunkEntityEnumerator GetChunksIntersecting(EntityUid root, Box2 localAabb)
    {
        return new ChunkEntityEnumerator(this, root, new ChunkIndicesEnumerator(localAabb, ChunkSize));
    }

    /// <summary>
    /// <see cref="GetChunksInRange"/> but with component overload.
    /// </summary>
    public ChunkEntityComponentEnumerator<T> GetChunksInRange<T>(
        EntityUid root,
        Vector2 localPosition,
        float range,
        EntityQuery<T> query)
        where T : IComponent
    {
        return new ChunkEntityComponentEnumerator<T>(GetChunksInRange(root, localPosition, range), query);
    }

    /// <summary>
    /// <see cref="GetChunksIntersecting"/> but with component overload.
    /// </summary>
    public ChunkEntityComponentEnumerator<T> GetChunksIntersecting<T>(
        EntityUid root,
        Box2 localAabb,
        EntityQuery<T> query)
        where T : IComponent
    {
        return new ChunkEntityComponentEnumerator<T>(GetChunksIntersecting(root, localAabb), query);
    }

    private void OnMapCreated(MapCreatedEvent ev)
    {
        EnsureComp<PvsChunkContainerComponent>(ev.Uid);
    }

    private void OnGridInitialize(GridInitializeEvent ev)
    {
        EnsureComp<PvsChunkContainerComponent>(ev.EntityUid);
    }

    private void OnChunkStartup(Entity<ChunkEntityComponent> ent, ref ComponentStartup args)
    {
        AddChunk(ent);
    }

    private void OnChunkShutdown(Entity<ChunkEntityComponent> ent, ref ComponentRemove args)
    {
        RemoveChunk(ent);
    }

    private void OnChunkTerminating(Entity<ChunkEntityComponent> ent, ref EntityTerminatingEvent args)
    {
        RemoveChunk(ent);
    }

    private void OnChunkHandleState(Entity<ChunkEntityComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        AddChunk(ent);
    }

    private void OnMapRemoved(MapRemovedEvent ev)
    {
        DeleteRootChunks(ev.Uid);
    }

    private void OnGridRemoved(GridRemovalEvent ev)
    {
        DeleteRootChunks(ev.EntityUid);
    }

    private void OnBeforeSerialization(BeforeSerializationEvent ev)
    {
        if (ev.Category != FileCategory.Map && ev.Category != FileCategory.Grid)
            return;

        // Chunk entities are nullspace entities referenced by the saved root container (i.e. grid or map),
        // so we need to ensure they are included to serialize properly (e.g. in case grid-only serialization excludes them).
        // This assumption may change at some point but for now we need to do this.
        DebugTools.Assert(_serializationContainers.Count == 0);
        foreach (var root in ev.Entities)
        {
            if (_containerQuery.TryGetComponent(root, out var container))
                _serializationContainers.Add(container);
        }

        foreach (var container in _serializationContainers)
        {
            AddRootChunks(container, ev.Entities);
        }

        _serializationContainers.Clear();
    }

    private void AddRootChunks(PvsChunkContainerComponent container, HashSet<EntityUid> entities)
    {
        foreach (var chunk in container.ChunkEntities)
        {
            DebugTools.Assert(!Deleted(chunk));
            entities.Add(chunk);
        }
    }

    private void AddChunk(Entity<ChunkEntityComponent, MetaDataComponent?> ent)
    {
        var (uid, comp, meta) = ent;
        var key = (comp.Root, comp.Chunk);
        meta ??= MetaData(uid);

        if (!_mapQuery.HasComp(comp.Root) && !_gridQuery.HasComp(comp.Root))
        {
            Del(uid, meta);
            return;
        }

        if (!_containerQuery.TryGetComponent(comp.Root, out var container))
        {
            DebugTools.Assert($"{nameof(PvsChunkContainerComponent)} missing on chunk root {ToPrettyString(comp.Root)}.");
            container = EnsureComp<PvsChunkContainerComponent>(comp.Root);
        }

        if (container.Chunks.TryGetValue(comp.Chunk, out var oldChunk))
        {
            if (oldChunk.Owner == uid)
            {
                DebugTools.Assert(container.ChunkEntities.Contains(uid));
                meta.Flags |= MetaDataFlags.ChunkEntity;
                return;
            }

            DebugTools.Assert($"Duplicate chunk entity for root {ToPrettyString(comp.Root)} and chunk {comp.Chunk}.");
        }

        meta.Flags |= MetaDataFlags.ChunkEntity;

        container.Chunks[comp.Chunk] = (uid, comp);
        container.ChunkEntities.Add(uid);

        var ev = new ChunkEntityAddedEvent(uid, key.Root, key.Chunk);
        RaiseLocalEvent(ref ev);
    }

    private bool IsAvailable(Entity<ChunkEntityComponent> chunk)
    {
        return _metaQuery.TryGetComponent(chunk.Owner, out var meta) &&
               !Deleted(chunk.Owner, meta) &&
               !chunk.Comp.Deleted &&
               (meta.Flags & MetaDataFlags.Detached) == 0;
    }

    private void RemoveChunk(Entity<ChunkEntityComponent> ent)
    {
        var (uid, comp) = ent;
        if (_containerQuery.TryGetComponent(comp.Root, out var container) &&
            container.Chunks.TryGetValue(comp.Chunk, out var existing) &&
            existing.Owner == uid)
        {
            container.Chunks.Remove(comp.Chunk);
        }

        container?.ChunkEntities.Remove(uid);

        var ev = new ChunkEntityRemovedEvent(uid, comp.Root, comp.Chunk);
        RaiseLocalEvent(ref ev);

        if (_metaQuery.TryGetComponent(uid, out var meta))
            meta.Flags &= ~MetaDataFlags.ChunkEntity;
    }

    private void DeleteRootChunks(EntityUid root)
    {
        if (!_containerQuery.TryGetComponent(root, out var container))
            return;

        _tempUids.Clear();
        _tempUids.AddRange(container.ChunkEntities);

        foreach (var uid in _tempUids)
        {
            if (!_chentQuery.TryComp(uid, out var chent))
            {
                continue;
            }

            container.Chunks.Remove(chent.Chunk);
            container.ChunkEntities.Remove(uid);

            if (_metaQuery.TryGetComponent(uid, out var meta) && !Deleted(uid, meta))
            {
                var ev = new ChunkEntityRemovedEvent(uid, root, chent.Chunk);
                RaiseLocalEvent(ref ev);

                Del(uid, meta);
            }
        }
    }

    public struct ChunkEntityEnumerator
    {
        private readonly ChunkEntitySystem _system;
        private readonly EntityUid _root;
        private ChunkIndicesEnumerator _indices;

        internal ChunkEntityEnumerator(ChunkEntitySystem system, EntityUid root, ChunkIndicesEnumerator indices)
        {
            _system = system;
            _root = root;
            _indices = indices;
        }

        public bool MoveNext([NotNullWhen(true)] out Entity<ChunkEntityComponent>? entity)
        {
            while (_indices.MoveNext(out var chunk))
            {
                if (_system.TryGetChunk(_root, chunk.Value, out entity))
                    return true;
            }

            entity = null;
            return false;
        }
    }

    public struct ChunkEntityRootEnumerator
    {
        private readonly ChunkEntitySystem _system;
        private Dictionary<Vector2i, Entity<ChunkEntityComponent>>.Enumerator _enumerator;
        private readonly bool _valid;

        internal ChunkEntityRootEnumerator(ChunkEntitySystem system, PvsChunkContainerComponent? container)
        {
            _system = system;
            _valid = container != null;
            _enumerator = container?.Chunks.GetEnumerator() ?? default;
        }

        public bool MoveNext([NotNullWhen(true)] out Entity<ChunkEntityComponent>? entity)
        {
            while (_valid && _enumerator.MoveNext())
            {
                var chunk = _enumerator.Current.Value;
                if (_system.IsAvailable(chunk))
                {
                    entity = (chunk.Owner, chunk.Comp);
                    return true;
                }
            }

            entity = null;
            return false;
        }
    }

    public struct ChunkEntityComponentEnumerator<T> where T : IComponent
    {
        private ChunkEntityEnumerator _enumerator;
        private readonly EntityQuery<T> _query;

        internal ChunkEntityComponentEnumerator(ChunkEntityEnumerator enumerator, EntityQuery<T> query)
        {
            _enumerator = enumerator;
            _query = query;
        }

        public bool MoveNext([NotNullWhen(true)] out Entity<ChunkEntityComponent, T>? entity)
        {
            while (_enumerator.MoveNext(out var chunk))
            {
                if (_query.TryComp(chunk.Value.Owner, out var comp))
                {
                    entity = (chunk.Value.Owner, chunk.Value.Comp, comp);
                    return true;
                }
            }

            entity = null;
            return false;
        }
    }
}

/// <summary>
/// Raised when a chunk entity is added.
/// </summary>
[ByRefEvent]
public record struct ChunkEntityAddedEvent(EntityUid Entity, EntityUid Root, Vector2i Chunk)
{
    public readonly EntityUid Entity = Entity;
    public readonly EntityUid Root = Root;
    public readonly Vector2i Chunk = Chunk;
}

/// <summary>
/// Raised when a chunk entity is removed.
/// </summary>
[ByRefEvent]
public record struct ChunkEntityRemovedEvent(EntityUid Entity, EntityUid Root, Vector2i Chunk)
{
    public readonly EntityUid Entity = Entity;
    public readonly EntityUid Root = Root;
    public readonly Vector2i Chunk = Chunk;
}
