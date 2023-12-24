using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

// Partial class for handling PVS chunks.
internal sealed partial class PvsSystem
{
    public const float ChunkSize = 8;

    private readonly Dictionary<PvsChunkLocation, PvsChunk> _chunks = new();
    private readonly List<PvsChunkLocation> _visibleChunks = new(64);
    private readonly HashSet<PvsChunkLocation> _visibleChunkSet = new(64);

    // Store chunks grouped by the root node, for when maps/grids get deleted.
    private readonly Dictionary<EntityUid, HashSet<PvsChunkLocation>> _chunkSets = new();

    private List<Entity<MapGridComponent>> _grids = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkIndices(Vector2 coordinates) => (coordinates / ChunkSize).Floored();

    /// <summary>
    /// Iterate over all visible chunks and, if necessary, re-construct their list of entities.
    /// </summary>
    public void UpdateVisibleChunks(int index)
    {
        var loc = _visibleChunks[index];
        if (!_chunks.TryGetValue(loc, out var chunk))
            return; // empty / non-existent chunk.

        if (chunk.Dirty)
        {
            if (!chunk.PopulateContents(_metaQuery, _xformQuery))
                return; // Failed to populate a dirty chunk.
        }

        UpdateChunkPosition(chunk);
    }

    /// <summary>
    /// Update a chunk's world position. This is used to prioritize sending chunks that a closer to players.
    /// </summary>
    private void UpdateChunkPosition(PvsChunk? chunk)
    {
        if (chunk == null)
            return;

        var xform = Transform(chunk.Root);
        chunk.InvWorldMatrix = xform.InvLocalMatrix;
        var worldPos = xform.LocalMatrix.Transform(chunk.Centre);
        chunk.Position = new(worldPos, xform.MapID);
    }

    /// <summary>
    /// Update the list of all currently visible chunks.
    /// </summary>
    public void GetVisibleChunks(ICommonSession[] sessions)
    {
        _visibleChunks.Clear();
        _visibleChunkSet.Clear();
        foreach (var session in sessions)
        {
            var data = _playerData[session];
            DebugTools.Assert(data.VisibleChunks.Count == 0);

            GetSessionViewers(data);
            foreach (var eye in data.Viewers)
            {
                GetVisibleChunks(eye, data.VisibleChunks);
            }
        }
    }

    /// <summary>
    /// Get the chunks visible to a single entity and add them to a player's set of visible chunks.
    /// </summary>
    private void GetVisibleChunks(Entity<TransformComponent, EyeComponent?> eye, HashSet<PvsChunkLocation> playerChunks)
    {
        var (viewPos, range, mapUid) = CalcViewBounds(eye);
        if (mapUid is not {} map)
            return;

        var mapChunkEnumerator = new ChunkIndicesEnumerator(viewPos, range, ChunkSize);
        while (mapChunkEnumerator.MoveNext(out var chunkIndices))
        {
            var loc = new PvsChunkLocation(map, chunkIndices.Value);
            playerChunks.Add(loc);
            if (_visibleChunkSet.Add(loc))
                _visibleChunks.Add(loc);
        }

        _grids.Clear();
        var rangeVec = new Vector2(range, range);
        var box = new Box2(viewPos - rangeVec, viewPos + rangeVec);
        _mapManager.FindGridsIntersecting(map, box, ref _grids, approx: true, includeMap: false);

        foreach (var (grid, _) in _grids)
        {
            var localPos = _transform.GetInvWorldMatrix(grid).Transform(viewPos);
            var gridChunkEnumerator = new ChunkIndicesEnumerator(localPos, range, ChunkSize);
            while (gridChunkEnumerator.MoveNext(out var gridChunkIndices))
            {
                var loc = new PvsChunkLocation(grid, gridChunkIndices.Value);
                playerChunks.Add(loc);
                if (_visibleChunkSet.Add(loc))
                    _visibleChunks.Add(loc);
            }
        }
    }

    /// <summary>
    /// Get all viewers for a given session. This is required to get a list of visible chunks.
    /// </summary>
    private void GetSessionViewers(PvsSession pvsSession)
    {
        var session = pvsSession.Session;
        if (session.Status != SessionStatus.InGame)
        {
            pvsSession.Viewers  = Array.Empty<Entity<TransformComponent, EyeComponent?>>();
            return;
        }

        // Fast path
        if (session.ViewSubscriptions.Count == 0)
        {
            if (session.AttachedEntity is not {} attached)
            {
                pvsSession.Viewers = Array.Empty<Entity<TransformComponent, EyeComponent?>>();
                return;
            }

            Array.Resize(ref pvsSession.Viewers, 1);
            pvsSession.Viewers[0] = (attached, Transform(attached), _eyeQuery.CompOrNull(attached));
            return;
        }

        int i = 0;
        if (session.AttachedEntity is { } local)
        {
            DebugTools.Assert(!session.ViewSubscriptions.Contains(local));
            Array.Resize(ref pvsSession.Viewers, session.ViewSubscriptions.Count + 1);
            pvsSession.Viewers[i++] = (local, Transform(local), _eyeQuery.CompOrNull(local));
        }
        else
        {
            Array.Resize(ref pvsSession.Viewers, session.ViewSubscriptions.Count);
        }

        foreach (var ent in session.ViewSubscriptions)
        {
            pvsSession.Viewers[i++] =  (ent, Transform(ent), _eyeQuery.CompOrNull(ent));
        }
    }

    public void ProcessChunks(ICommonSession[] players)
    {
        GetVisibleChunks(players);

        if (_visibleChunks.Count > 0)
            _parallelMgr.ProcessNow(_chunkJob, _visibleChunks.Count + 1);
    }

    /// <summary>
    /// Add an entity to the set of entities that are directly attached to a chunk and mark the chunk as dirty.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddEntityToChunk(EntityUid uid, MetaDataComponent meta, PvsChunkLocation location)
    {
        ref var chunk = ref CollectionsMarshal.GetValueRefOrAddDefault(_chunks, location, out var existing);
        if (!existing)
        {
            chunk = _chunkPool.Get();
            chunk.Initialize(location, _metaQuery, _xformQuery);
            _chunkSets.GetOrNew(location.Uid).Add(location);
        }

        chunk!.MarkDirty();
        chunk.Children.Add(uid);
        meta.LastPvsLocation = location;
    }

    /// <summary>
    /// Remove an entity from a chunk and mark it as dirty.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveEntityFromChunk(EntityUid uid, MetaDataComponent meta)
    {
        if (meta.LastPvsLocation is not {} old)
            return;

        meta.LastPvsLocation = null;
        if (!_chunks.TryGetValue(old, out var chunk))
            return;

        chunk.MarkDirty();
        chunk.Children.Remove(uid);
        if (chunk.Children.Count > 0)
            return;

        _chunks.Remove(old);
        _chunkPool.Return(chunk);
        _chunkSets[old.Uid].Remove(old);
    }

    /// <summary>
    /// Mark a chunk as dirty.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DirtyChunk(PvsChunkLocation location)
    {
        if (_chunks.TryGetValue(location, out var chunk))
            chunk.MarkDirty();
    }

    private void OnGridRemoved(GridRemovalEvent ev)
    {
        RemoveRoot(ev.EntityUid);
    }

    private void OnMapChanged(MapChangedEvent ev)
    {
        if (!ev.Destroyed)
            RemoveRoot(ev.Uid);
    }

    private void RemoveRoot(EntityUid root)
    {
        if (!_chunkSets.Remove(root, out var locations))
            return;

        foreach (var loc in locations)
        {
            if (_chunks.Remove(loc, out var chunk))
                _chunkPool.Return(chunk);
        }
    }
}
