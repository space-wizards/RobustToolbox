using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Shared.GameStates;

/// <summary>
/// Manages nullspace entities that are treated as members of map/grid PVS chunks.
/// </summary>
public sealed partial class ChunkEntitySystem : EntitySystem
{
    public const int ChunkSize = MapGridComponent.DefaultChunkSize;
    private static readonly EntProtoId ChunkEntityPrototype = "ChunkEntity";

    [Dependency] private EntityQuery<MetaDataComponent> _metaQuery = default!;
    [Dependency] private EntityQuery<MapComponent> _mapQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    private readonly Dictionary<(EntityUid Root, Vector2i Chunk), Entity<ChunkEntityComponent, MetaDataComponent>> _chunks = new();
    private readonly Dictionary<EntityUid, HashSet<Vector2i>> _chunksByRoot = new();
    private readonly Dictionary<EntityUid, (EntityUid Root, Vector2i Chunk)> _chunkKeysByEntity = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ChunkEntityComponent, ComponentAdd>(OnChunkStartup);
        SubscribeLocalEvent<ChunkEntityComponent, ComponentRemove>(OnChunkShutdown);
        SubscribeLocalEvent<ChunkEntityComponent, EntityTerminatingEvent>(OnChunkTerminating);
        SubscribeLocalEvent<ChunkEntityComponent, AfterAutoHandleStateEvent>(OnChunkHandleState);
        SubscribeLocalEvent<ChunkEntityComponent, EntParentChangedMessage>(OnChunkParentChanged);
        SubscribeLocalEvent<MapRemovedEvent>(OnMapRemoved);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
    }

    public Entity<ChunkEntityComponent> GetOrCreateChunk(EntityUid root, Vector2i chunk)
    {
        if (_chunks.TryGetValue((root, chunk), out var existing) &&
            !Deleted(existing.Owner, existing.Comp2) &&
            !existing.Comp1.Deleted)
        {
            return (existing.Owner, existing.Comp1);
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

    public bool TryGetChunk(EntityUid root, Vector2i chunk, [NotNullWhen(true)] out Entity<ChunkEntityComponent>? entity)
    {
        if (_chunks.TryGetValue((root, chunk), out var existing) &&
            !Deleted(existing.Owner, existing.Comp2) &&
            !existing.Comp1.Deleted &&
            (existing.Comp2.Flags & MetaDataFlags.Detached) == 0)
        {
            entity = (existing.Owner, existing.Comp1);
            return true;
        }

        entity = null;
        return false;
    }

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

    public ChunkEntityEnumerator GetChunksInRange(EntityUid root, Vector2 localPosition, float range)
    {
        return new ChunkEntityEnumerator(this, root, new ChunkIndicesEnumerator(localPosition, range, ChunkSize));
    }

    public ChunkEntityEnumerator GetChunksIntersecting(EntityUid root, Box2 localAabb)
    {
        return new ChunkEntityEnumerator(this, root, new ChunkIndicesEnumerator(localAabb, ChunkSize));
    }

    public ChunkEntityComponentEnumerator<T> GetChunksInRange<T>(
        EntityUid root,
        Vector2 localPosition,
        float range,
        EntityQuery<T> query)
        where T : IComponent
    {
        return new ChunkEntityComponentEnumerator<T>(GetChunksInRange(root, localPosition, range), query);
    }

    public ChunkEntityComponentEnumerator<T> GetChunksIntersecting<T>(
        EntityUid root,
        Box2 localAabb,
        EntityQuery<T> query)
        where T : IComponent
    {
        return new ChunkEntityComponentEnumerator<T>(GetChunksIntersecting(root, localAabb), query);
    }

    private void OnChunkStartup(Entity<ChunkEntityComponent> ent, ref ComponentAdd args)
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

    private void OnChunkParentChanged(Entity<ChunkEntityComponent> ent, ref EntParentChangedMessage args)
    {
        if (args.Transform.ParentUid == EntityUid.Invalid && args.Transform.MapID == MapId.Nullspace)
            return;

        Log.Error($"Chunk entity {ToPrettyString(ent.Owner)} had its parent changed. Root: {ToPrettyString(ent.Comp.Root)}, chunk: {ent.Comp.Chunk}, old parent: {args.OldParent}, new parent: {args.Transform.ParentUid}, map: {args.Transform.MapID}");
    }

    private void AddChunk(Entity<ChunkEntityComponent> ent)
    {
        var (uid, comp) = ent;
        var key = (comp.Root, comp.Chunk);

        if (_chunkKeysByEntity.TryGetValue(uid, out var oldKey) && oldKey != key)
            RemoveChunk(uid, oldKey);

        _chunks[key] = (uid, comp, MetaData(uid));
        _chunkKeysByEntity[uid] = key;
        _chunksByRoot.GetOrNew(comp.Root).Add(comp.Chunk);

        var ev = new ChunkEntityAddedEvent(uid, comp.Root, comp.Chunk);
        RaiseLocalEvent(ref ev);
    }

    private void RemoveChunk(Entity<ChunkEntityComponent> ent)
    {
        var (uid, _) = ent;
        var key = _chunkKeysByEntity.GetValueOrDefault(uid, (ent.Comp.Root, ent.Comp.Chunk));

        RemoveChunk(uid, key);
    }

    private void RemoveChunk(EntityUid uid, (EntityUid Root, Vector2i Chunk) key)
    {
        if (_chunks.TryGetValue(key, out var existing) && existing.Owner != uid)
            return;

        _chunks.Remove(key);
        _chunkKeysByEntity.Remove(uid);

        if (_chunksByRoot.TryGetValue(key.Root, out var chunks))
        {
            chunks.Remove(key.Chunk);
            if (chunks.Count == 0)
                _chunksByRoot.Remove(key.Root);
        }

        var ev = new ChunkEntityRemovedEvent(uid, key.Root, key.Chunk);
        RaiseLocalEvent(ref ev);
    }

    private void DeleteRootChunks(EntityUid root)
    {
        if (!_chunksByRoot.Remove(root, out var chunks))
            return;

        foreach (var chunk in chunks)
        {
            if (!_chunks.Remove((root, chunk), out var uid))
                continue;

            DebugTools.Assert(_chunkKeysByEntity.ContainsKey(uid.Owner));
            _chunkKeysByEntity.Remove(uid.Owner);

            if (!Deleted(uid.Owner, uid.Comp2))
            {
                var ev = new ChunkEntityRemovedEvent(uid.Owner, root, chunk);
                RaiseLocalEvent(ref ev);
                Del(uid.Owner, uid.Comp2);
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
