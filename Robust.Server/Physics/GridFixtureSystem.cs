using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.Physics
{
    /// <summary>
    /// Handles generating fixtures for MapGrids.
    /// </summary>
    internal sealed class GridFixtureSystem : SharedGridFixtureSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IConGroupController _conGroup = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedTransformSystem _xformSystem = default!;

        private ISawmill _logger = default!;
        private readonly Dictionary<EntityUid, Dictionary<Vector2i, ChunkNodeGroup>> _nodes = new();

        /// <summary>
        /// Sessions to receive nodes for debug purposes.
        /// </summary>
        private readonly HashSet<ICommonSession> _subscribedSessions = new();

        /// <summary>
        /// Recursion detection to avoid splitting while handling an existing split
        /// </summary>
        private bool _isSplitting;

        internal bool SplitAllowed = true;

        public override void Initialize()
        {
            base.Initialize();
            _logger = Logger.GetSawmill("gsplit");
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);
            SubscribeNetworkEvent<RequestGridNodesMessage>(OnDebugRequest);
            SubscribeNetworkEvent<StopGridNodesMessage>(OnDebugStopRequest);
#if !FULL_RELEASE
            // It makes mapping painful
            _cfg.OverrideDefault(CVars.GridSplitting, false);
#endif
            _cfg.OnValueChanged(CVars.GridSplitting, SetSplitAllowed, true);
        }

        private void SetSplitAllowed(bool value) => SplitAllowed = value;

        public override void Shutdown()
        {
            base.Shutdown();
            _subscribedSessions.Clear();
            _cfg.UnsubValueChanged(CVars.GridSplitting, SetSplitAllowed);
        }

        /// <summary>
        /// Due to how MapLoader works need to ensure grid exists in dictionary before it's initialised.
        /// </summary>
        internal void EnsureGrid(EntityUid uid)
        {
            if (!_nodes.ContainsKey(uid))
                _nodes[uid] = new Dictionary<Vector2i, ChunkNodeGroup>();
        }

        protected override void OnGridInit(GridInitializeEvent ev)
        {
            EnsureGrid(ev.EntityUid);
            base.OnGridInit(ev);
        }

        private void OnGridRemoval(GridRemovalEvent ev)
        {
            _nodes.Remove(ev.EntityUid);
        }

        #region Debug

        private void OnDebugRequest(RequestGridNodesMessage msg, EntitySessionEventArgs args)
        {
            var pSession = (PlayerSession) args.SenderSession;

            if (!_conGroup.CanCommand(pSession, ShowGridNodesCommand)) return;

            AddDebugSubscriber(args.SenderSession);
        }

        private void OnDebugStopRequest(StopGridNodesMessage msg, EntitySessionEventArgs args)
        {
            RemoveDebugSubscriber(args.SenderSession);
        }

        public bool IsSubscribed(ICommonSession session)
        {
            return _subscribedSessions.Contains(session);
        }

        public void AddDebugSubscriber(ICommonSession session)
        {
            if (!_subscribedSessions.Add(session)) return;

            foreach (var (uid, _) in _nodes)
            {
                SendNodeDebug(uid);
            }
        }

        public void RemoveDebugSubscriber(ICommonSession session)
        {
            _subscribedSessions.Remove(session);
        }

        private void SendNodeDebug(EntityUid uid)
        {
            if (_subscribedSessions.Count == 0) return;

            var msg = new ChunkSplitDebugMessage
            {
                Grid = uid,
            };

            foreach (var (index, group) in _nodes[uid])
            {
                var list = new List<List<Vector2i>>();
                // To avoid double-sending connections.
                var conns = new HashSet<ChunkSplitNode>();

                foreach (var node in group.Nodes)
                {
                    conns.Add(node);
                    list.Add(node.Indices.ToList());

                    foreach (var neighbor in node.Neighbors)
                    {
                        if (conns.Contains(neighbor)) continue;

                        msg.Connections.Add((
                            node.GetCentre() + node.Group.Chunk.Indices * node.Group.Chunk.ChunkSize,
                            neighbor.GetCentre() + neighbor.Group.Chunk.Indices * neighbor.Group.Chunk.ChunkSize));
                    }
                }

                msg.Nodes.Add(index, list);
            }

            foreach (var session in _subscribedSessions)
            {
                RaiseNetworkEvent(msg, session.ConnectedClient);
            }
        }

        #endregion

        /// <summary>
        /// Check for any potential splits after maploading is done.
        /// </summary>
        internal void CheckSplits(EntityUid uid)
        {
            var nodes = _nodes[uid];
            var dirtyNodes = new HashSet<ChunkSplitNode>(nodes.Count);

            foreach (var (_, group) in nodes)
            {
                foreach (var node in group.Nodes)
                {
                    dirtyNodes.Add(node);
                }
            }

            CheckSplits(uid, dirtyNodes);
        }

        /// <summary>
        /// Check for splits on the specified nodes.
        /// </summary>
        private void CheckSplits(EntityUid uid, HashSet<ChunkSplitNode> dirtyNodes)
        {
            if (_isSplitting || !SplitAllowed) return;

            _isSplitting = true;
            _logger.Debug($"Started split check for {ToPrettyString(uid)}");
            var splitFrontier = new Queue<ChunkSplitNode>(4);
            var grids = new List<HashSet<ChunkSplitNode>>(1);

            while (dirtyNodes.Count > 0)
            {
                var originEnumerator = dirtyNodes.GetEnumerator();
                originEnumerator.MoveNext();
                var origin = originEnumerator.Current;
                originEnumerator.Dispose();
                splitFrontier.Enqueue(origin);
                var foundSplits = new HashSet<ChunkSplitNode>
                {
                    origin
                };

                while (splitFrontier.TryDequeue(out var split))
                {
                    dirtyNodes.Remove(split);

                    foreach (var neighbor in split.Neighbors)
                    {
                        if (!foundSplits.Add(neighbor)) continue;

                        splitFrontier.Enqueue(neighbor);
                    }
                }

                grids.Add(foundSplits);
            }

            var mapGrid = _mapManager.GetGrid(uid);

            // Split time
            if (grids.Count > 1)
            {
                _logger.Info($"Splitting {ToPrettyString(uid)} into {grids.Count} grids.");
                var sw = new Stopwatch();
                sw.Start();

                // We'll leave the biggest group as the original grid
                // anything smaller gets split off.
                grids.Sort((x, y) =>
                    x.Sum(o => o.Indices.Count)
                        .CompareTo(y.Sum(o => o.Indices.Count)));

                var xformQuery = GetEntityQuery<TransformComponent>();
                var bodyQuery = GetEntityQuery<PhysicsComponent>();
                var gridQuery = GetEntityQuery<MapGridComponent>();
                var oldGridXform = xformQuery.GetComponent(mapGrid.GridEntityId);
                var (gridPos, gridRot) = oldGridXform.GetWorldPositionRotation(xformQuery);
                var mapBody = bodyQuery.GetComponent(mapGrid.GridEntityId);
                var oldGridComp = gridQuery.GetComponent(mapGrid.GridEntityId);
                var newGrids = new EntityUid[grids.Count - 1];
                var mapId = oldGridXform.MapID;

                for (var i = 0; i < grids.Count - 1; i++)
                {
                    var group = grids[i];
                    var splitGrid = _mapManager.CreateGrid(mapId);
                    var splitXform = xformQuery.GetComponent(splitGrid.GridEntityId);
                    newGrids[i] = splitGrid.GridEntityId;

                    // Keep same origin / velocity etc; this makes updating a lot faster and easier.
                    splitXform.WorldPosition = gridPos;
                    splitXform.WorldRotation = gridRot;
                    var splitBody = bodyQuery.GetComponent(splitGrid.GridEntityId);
                    splitBody.LinearVelocity = mapBody.LinearVelocity;
                    splitBody.AngularVelocity = mapBody.AngularVelocity;

                    var gridComp = gridQuery.GetComponent(splitGrid.GridEntityId);
                    var tileData = new List<(Vector2i GridIndices, Tile Tile)>(group.Sum(o => o.Indices.Count));

                    // Gather all tiles up front and set once to minimise fixture change events
                    foreach (var node in group)
                    {
                        var offset = node.Group.Chunk.Indices * node.Group.Chunk.ChunkSize;

                        foreach (var index in node.Indices)
                        {
                            var tilePos = offset + index;
                            tileData.Add((tilePos, mapGrid.GetTileRef(tilePos).Tile));
                        }
                    }

                    splitGrid.SetTiles(tileData);
                    DebugTools.Assert(_mapManager.IsGrid(splitGrid.GridEntityId), "A split grid had no tiles?");

                    // Set tiles on new grid + update anchored entities
                    foreach (var node in group)
                    {
                        var offset = node.Group.Chunk.Indices * node.Group.Chunk.ChunkSize;

                        foreach (var tile in node.Indices)
                        {
                            var tilePos = offset + tile;

                            // Access it directly because we're gonna be hammering it and want to keep allocs down.
                            var snapgrid = node.Group.Chunk.GetSnapGrid((ushort) tile.X, (ushort) tile.Y);
                            if (snapgrid == null || snapgrid.Count == 0) continue;

                            for (var j = snapgrid.Count - 1; j >= 0; j--)
                            {
                                var ent = snapgrid[j];
                                var xform = xformQuery.GetComponent(ent);
                                _xformSystem.ReAnchor(xform, oldGridComp, gridComp, tilePos, oldGridXform, splitXform, xformQuery);
                                DebugTools.Assert(xform.Anchored);
                            }
                        }

                        // Update lookup ents
                        // Needs to be done before setting old tiles as they will be re-parented to the map.
                        // TODO: Combine tiles into larger rectangles or something; this is gonna be the killer bit.
                        foreach (var tile in node.Indices)
                        {
                            var tilePos = offset + tile;
                            var bounds = _lookup.GetLocalBounds(tilePos, mapGrid.TileSize);

                            foreach (var ent in _lookup.GetEntitiesIntersecting(mapGrid.GridEntityId, tilePos, LookupFlags.Dynamic | LookupFlags.Sundries))
                            {
                                // Consider centre of entity position maybe?
                                var entXform = xformQuery.GetComponent(ent);

                                if (entXform.ParentUid != mapGrid.GridEntityId ||
                                    !bounds.Contains(entXform.LocalPosition)) continue;

                                _xformSystem.SetParent(entXform, splitXform.Owner, xformQuery, splitXform);
                            }
                        }

                        _nodes[mapGrid.GridEntityId][node.Group.Chunk.Indices].Nodes.Remove(node);
                    }

                    var eevee = new PostGridSplitEvent(mapGrid.GridEntityId, splitGrid.GridEntityId);
                    RaiseLocalEvent(uid, ref eevee, true);

                    for (var j = 0; j < tileData.Count; j++)
                    {
                        var (index, _) = tileData[j];
                        tileData[j] = (index, Tile.Empty);
                    }

                    // Set tiles on old grid
                    mapGrid.SetTiles(tileData);
                    GenerateSplitNodes((MapGridComponent) splitGrid);
                    SendNodeDebug(splitGrid.GridEntityId);
                }

                // Cull all of the old chunk nodes.
                var toRemove = new RemQueue<ChunkNodeGroup>();

                foreach (var (_, group) in _nodes[mapGrid.GridEntityId])
                {
                    if (group.Nodes.Count > 0) continue;
                    toRemove.Add(group);
                }

                foreach (var group in toRemove)
                {
                    _nodes[mapGrid.GridEntityId].Remove(group.Chunk.Indices);
                }

                // Allow content to react to the grid being split...
                var ev = new GridSplitEvent(newGrids, mapGrid.GridEntityId);
                RaiseLocalEvent(uid, ref ev, true);

                _logger.Debug($"Split {grids.Count} grids in {sw.Elapsed}");
            }

            _logger.Debug($"Stopped split check for {ToPrettyString(uid)}");
            _isSplitting = false;
            SendNodeDebug(mapGrid.GridEntityId);
        }

        private void GenerateSplitNodes(MapGridComponent grid)
        {
            foreach (var (_, chunk) in grid.GetMapChunks())
            {
                var group = CreateNodes(grid.GridEntityId, grid, chunk);
                _nodes[grid.GridEntityId].Add(chunk.Indices, group);
            }
        }

        /// <summary>
        /// Creates all of the splitting nodes within this chunk; also consider neighbor chunks.
        /// </summary>
        private ChunkNodeGroup CreateNodes(EntityUid gridEuid, MapGridComponent grid, MapChunk chunk)
        {
            var group = new ChunkNodeGroup
            {
                Chunk = chunk,
            };

            var tiles = new HashSet<Vector2i>(chunk.ChunkSize * chunk.ChunkSize);

            for (var x = 0; x < chunk.ChunkSize; x++)
            {
                for (var y = 0; y < chunk.ChunkSize; y++)
                {
                    tiles.Add(new Vector2i(x, y));
                }
            }

            var frontier = new Queue<Vector2i>();
            var node = new ChunkSplitNode
            {
                Group = group,
            };

            // Simple BFS search to get all of the nodes in the chunk.
            while (tiles.Count > 0)
            {
                var originEnumerator = tiles.GetEnumerator();
                originEnumerator.MoveNext();
                var origin = originEnumerator.Current;
                frontier.Enqueue(origin);
                originEnumerator.Dispose();

                // Just reuse the node if we couldn't use it last time.
                // This is in case weh ave 1 chunk with 255 empty tiles and 1 valid tile.
                if (node.Indices.Count > 0)
                {
                    node = new ChunkSplitNode
                    {
                        Group = group,
                    };
                }

                tiles.Remove(origin);

                // Check for valid neighbours and add them to the frontier.
                while (frontier.TryDequeue(out var index))
                {
                    var tile = chunk.GetTile((ushort) index.X, (ushort) index.Y);
                    if (tile.IsEmpty) continue;

                    node.Indices.Add(index);
                    var enumerator = new NeighborEnumerator(chunk, index);

                    while (enumerator.MoveNext(out var neighbor))
                    {
                        // Already iterated this tile before so just ignore it.
                        if (!tiles.Remove(neighbor.Value)) continue;
                        frontier.Enqueue(neighbor.Value);
                    }
                }

                if (node.Indices.Count == 0) continue;

                group.Nodes.Add(node);
            }

            // Build neighbors
            ChunkSplitNode? neighborNode;
            MapChunk? neighborChunk;

            // Check each tile for node neighbours on other chunks (not possible for us to have neighbours on the same chunk
            // as they would already be in our node).
            // TODO: This could be better (maybe only check edges of the chunk or something).
            foreach (var chunkNode in group.Nodes)
            {
                foreach (var index in chunkNode.Indices)
                {
                    // Check for edge tiles.
                    if (index.X == 0)
                    {
                        // Check West
                        if (grid.TryGetChunk(new Vector2i(chunk.Indices.X - 1, chunk.Indices.Y), out neighborChunk) &&
                            TryGetNode(gridEuid, neighborChunk, new Vector2i(chunk.ChunkSize - 1, index.Y), out neighborNode))
                        {
                            chunkNode.Neighbors.Add(neighborNode);
                            neighborNode.Neighbors.Add(chunkNode);
                        }
                    }

                    if (index.Y == 0)
                    {
                        // Check South
                        if (grid.TryGetChunk(new Vector2i(chunk.Indices.X, chunk.Indices.Y - 1), out neighborChunk) &&
                            TryGetNode(gridEuid, neighborChunk, new Vector2i(index.X, chunk.ChunkSize - 1), out neighborNode))
                        {
                            chunkNode.Neighbors.Add(neighborNode);
                            neighborNode.Neighbors.Add(chunkNode);
                        }
                    }

                    if (index.X == chunk.ChunkSize - 1)
                    {
                        // Check East
                        if (grid.TryGetChunk(new Vector2i(chunk.Indices.X + 1, chunk.Indices.Y), out neighborChunk) &&
                            TryGetNode(gridEuid, neighborChunk, new Vector2i(0, index.Y), out neighborNode))
                        {
                            chunkNode.Neighbors.Add(neighborNode);
                            neighborNode.Neighbors.Add(chunkNode);
                        }
                    }

                    if (index.Y == chunk.ChunkSize - 1)
                    {
                        // Check North
                        if (grid.TryGetChunk(new Vector2i(chunk.Indices.X, chunk.Indices.Y + 1), out neighborChunk) &&
                            TryGetNode(gridEuid, neighborChunk, new Vector2i(index.X, 0), out neighborNode))
                        {
                            chunkNode.Neighbors.Add(neighborNode);
                            neighborNode.Neighbors.Add(chunkNode);
                        }
                    }
                }
            }

            return group;
        }

        /// <summary>
        /// Checks for grid split with 1 chunk updated.
        /// </summary>
        internal override void CheckSplit(EntityUid gridEuid, MapChunk chunk, List<Box2i> rectangles)
        {
            HashSet<ChunkSplitNode> nodes;

            if (chunk.FilledTiles == 0)
            {
                nodes = RemoveSplitNode(gridEuid, chunk);
            }
            else
            {
                nodes = GenerateSplitNode(gridEuid, chunk);
            }

            CheckSplits(gridEuid, nodes);
        }

        /// <summary>
        /// Checks for grid split with many chunks updated.
        /// </summary>
        internal override void CheckSplit(EntityUid gridEuid, Dictionary<MapChunk, List<Box2i>> mapChunks, List<MapChunk> removedChunks)
        {
            var nodes = new HashSet<ChunkSplitNode>();

            foreach (var chunk in removedChunks)
            {
                nodes.UnionWith(RemoveSplitNode(gridEuid, chunk));
            }

            foreach (var (chunk, _) in mapChunks)
            {
                nodes.UnionWith(GenerateSplitNode(gridEuid, chunk));
            }

            var toRemove = new ValueList<ChunkSplitNode>();

            // Some of the neighbour nodes may have been added that were since deleted during the above enumeration
            // e.g. if NodeA and NodeB both had their counts set to 0 and are neighbours then either might add
            // the other to dirtynodes.
            foreach (var node in nodes)
            {
                if (node.Indices.Count > 0) continue;
                toRemove.Add(node);
            }

            foreach (var node in toRemove)
            {
                nodes.Remove(node);
            }

            CheckSplits(gridEuid, nodes);
        }

        /// <summary>
        /// Removes this chunk from nodes and dirties its neighbours.
        /// </summary>
        private HashSet<ChunkSplitNode> RemoveSplitNode(EntityUid gridEuid, MapChunk chunk)
        {
            var dirtyNodes = new HashSet<ChunkSplitNode>();

            if (_isSplitting) return new HashSet<ChunkSplitNode>();

            Cleanup(gridEuid, chunk, dirtyNodes);
            DebugTools.Assert(dirtyNodes.All(o => o.Group.Chunk != chunk));
            return dirtyNodes;
        }

        /// <summary>
        /// Re-adds this chunk to nodes and dirties its neighbours and itself.
        /// </summary>
        private HashSet<ChunkSplitNode> GenerateSplitNode(EntityUid gridEuid, MapChunk chunk)
        {
            var dirtyNodes = RemoveSplitNode(gridEuid, chunk);

            if (_isSplitting) return dirtyNodes;

            DebugTools.Assert(chunk.FilledTiles > 0);

            var grid = _mapManager.GetGrid(gridEuid);
            var group = CreateNodes(gridEuid, grid, chunk);
            _nodes[gridEuid][chunk.Indices] = group;

            foreach (var chunkNode in group.Nodes)
            {
                dirtyNodes.Add(chunkNode);
            }

            return dirtyNodes;
        }

        /// <summary>
        /// Tries to get the relevant split node from a neighbor chunk.
        /// </summary>
        private bool TryGetNode(EntityUid gridEuid, MapChunk chunk, Vector2i index, [NotNullWhen(true)] out ChunkSplitNode? node)
        {
            if (!_nodes[gridEuid].TryGetValue(chunk.Indices, out var neighborGroup))
            {
                node = null;
                return false;
            }

            foreach (var neighborNode in neighborGroup.Nodes)
            {
                if (!neighborNode.Indices.Contains(index)) continue;
                node = neighborNode;
                return true;
            }

            node = null;
            return false;
        }

        private void Cleanup(EntityUid gridEuid, MapChunk chunk, HashSet<ChunkSplitNode> dirtyNodes)
        {
            if (!_nodes[gridEuid].TryGetValue(chunk.Indices, out var group)) return;

            foreach (var node in group.Nodes)
            {
                // Most important thing is updating our neighbor nodes.
                foreach (var neighbor in node.Neighbors)
                {
                    neighbor.Neighbors.Remove(node);
                    // If neighbor is on a different chunk mark it for checking connections later.
                    if (neighbor.Group.Equals(group)) continue;
                    dirtyNodes.Add(neighbor);
                }

                node.Indices.Clear();
                node.Neighbors.Clear();
            }

            _nodes[gridEuid].Remove(chunk.Indices);
        }

        internal sealed class ChunkNodeGroup
        {
            internal MapChunk Chunk = default!;
            public HashSet<ChunkSplitNode> Nodes = new();
        }

        internal sealed class ChunkSplitNode
        {
            public ChunkNodeGroup Group = default!;
            public HashSet<Vector2i> Indices { get; set; } = new();
            public HashSet<ChunkSplitNode> Neighbors { get; set; } = new();

            public Vector2 GetCentre()
            {
                var centre = Vector2.Zero;

                foreach (var index in Indices)
                {
                    centre += index;
                }

                centre /= Indices.Count;
                return centre;
            }
        }

        private struct NeighborEnumerator
        {
            private MapChunk _chunk;
            private Vector2i _index;
            private int _count = -1;

            public NeighborEnumerator(MapChunk chunk, Vector2i index)
            {
                _chunk = chunk;
                _index = index;
            }

            public bool MoveNext([NotNullWhen(true)] out Vector2i? neighbor)
            {
                _count++;

                // Just go through S E N W
                switch (_count)
                {
                    case 0:
                        if (_index.Y == 0) break;
                        neighbor = new Vector2i(_index.X, _index.Y - 1);
                        return true;
                    case 1:
                        if (_index.X == _chunk.ChunkSize - 1) break;
                        neighbor = new Vector2i(_index.X + 1, _index.Y);
                        return true;
                    case 2:
                        if (_index.Y == _chunk.ChunkSize + 1) break;
                        neighbor = new Vector2i(_index.X, _index.Y + 1);
                        return true;
                    case 3:
                        if (_index.X == 0) break;
                        neighbor = new Vector2i(_index.X - 1, _index.Y);
                        return true;
                    default:
                        neighbor = null;
                        return false;
                }

                return MoveNext(out neighbor);
            }
        }
    }
}

/// <summary>
///     Event raised on a grid after it has been split but before the old grid has been cleaned up.
/// </summary>
[ByRefEvent]
public readonly struct PostGridSplitEvent
{
    /// <summary>
    ///     The grid it was part of previously.
    /// </summary>
    public readonly EntityUid OldGrid;

    /// <summary>
    ///     The grid that has been split.
    /// </summary>
    public readonly EntityUid Grid;

    public PostGridSplitEvent(EntityUid oldGrid, EntityUid grid)
    {
        OldGrid = oldGrid;
        Grid = grid;
    }
}

/// <summary>
///     Event raised on a grid that has been split into multiple grids.
/// </summary>
[ByRefEvent]
public readonly struct GridSplitEvent
{
    /// <summary>
    ///     Contains the IDs of the newly created grids.
    /// </summary>
    public readonly EntityUid[] NewGrids;

    /// <summary>
    ///     The grid that has been split.
    /// </summary>
    public readonly EntityUid Grid;

    public GridSplitEvent(EntityUid[] newGrids, EntityUid grid)
    {
        NewGrids = newGrids;
        Grid = grid;
    }
}
