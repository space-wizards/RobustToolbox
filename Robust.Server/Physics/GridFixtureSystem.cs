using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Robust.Server.Physics
{
    /// <summary>
    /// Handles generating fixtures for MapGrids.
    /// </summary>
    internal sealed class GridFixtureSystem : SharedGridFixtureSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;

        private readonly Dictionary<EntityUid, Dictionary<Vector2i, ChunkNodeGroup>> _nodes = new();

        /// <summary>
        /// Sessions to receive nodes for debug purposes.
        /// </summary>
        private readonly HashSet<ICommonSession> _subscribedSessions = new();

        /// <summary>
        /// Recursion detection to avoid splitting while handling an existing split
        /// </summary>
        private bool _splitting;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);
            SubscribeNetworkEvent<RequestGridNodesMessage>(OnDebugRequest);
            SubscribeNetworkEvent<StopGridNodesMessage>(OnDebugStopRequest);
        }

        private void OnDebugRequest(RequestGridNodesMessage msg, EntitySessionEventArgs args)
        {
            var adminManager = IoCManager.Resolve<IConGroupController>();
            var pSession = (PlayerSession)args.SenderSession;

            if (!adminManager.CanCommand(pSession, ShowGridNodesCommand)) return;

            AddDebugSubscriber(args.SenderSession);
        }

        private void OnDebugStopRequest(StopGridNodesMessage msg, EntitySessionEventArgs args)
        {
            RemoveDebugSubscriber(args.SenderSession);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _subscribedSessions.Clear();
        }

        /// <summary>
        /// Due to how MapLoader works need to ensure grid exists in dictionary before it's initialised.
        /// </summary>
        internal void EnsureGrid(EntityUid uid)
        {
            if (!_nodes.ContainsKey(uid))
                _nodes[uid] = new Dictionary<Vector2i, ChunkNodeGroup>();
        }

        private void OnGridInit(GridInitializeEvent ev)
        {
            EnsureGrid(ev.EntityUid);
        }

        private void OnGridRemoval(GridRemovalEvent ev)
        {
            _nodes.Remove(ev.EntityUid);
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

        private void CheckSplits(EntityUid uid, HashSet<ChunkSplitNode> dirtyNodes)
        {
            if (_splitting) return;

            _splitting = true;
            var splitFrontier = new Queue<ChunkSplitNode>();
            var grids = new List<HashSet<ChunkSplitNode>>(1);

            // TODO: At this point detect splits.
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
                // We'll leave the biggest group as the original grid
                // anything smaller gets split off.
                grids.Sort((x, y) =>
                    x.Sum(o => o.Indices.Count)
                        .CompareTo(y.Sum(o => o.Indices.Count)));

                var xformQuery = GetEntityQuery<TransformComponent>();
                var bodyQuery = GetEntityQuery<PhysicsComponent>();
                var (gridPos, gridRot) = xformQuery.GetComponent(mapGrid.GridEntityId).GetWorldPositionRotation(xformQuery);
                var mapBody = bodyQuery.GetComponent(mapGrid.GridEntityId);

                for (var i = 0; i < grids.Count - 1; i++)
                {
                    var group = grids[i];
                    var splitGrid = _mapManager.CreateGrid(mapGrid.ParentMapId);

                    splitGrid.WorldPosition = gridPos;
                    splitGrid.WorldRotation = gridRot;
                    var splitBody = bodyQuery.GetComponent(splitGrid.GridEntityId);
                    var splitXform = xformQuery.GetComponent(splitGrid.GridEntityId);
                    splitBody.LinearVelocity = mapBody.LinearVelocity;
                    splitBody.AngularVelocity = mapBody.AngularVelocity;

                    var gridComp = Comp<IMapGridComponent>(splitGrid.GridEntityId);

                    // Set tiles on new grid + update anchored entities
                    foreach (var node in group)
                    {
                        var offset = node.Group.Chunk.Indices * node.Group.Chunk.ChunkSize;

                        // TODO: Use the group version
                        foreach (var tile in node.Indices)
                        {
                            var tilePos = offset + tile;

                            // TODO: Could be faster getting tile data.
                            splitGrid.SetTile(tilePos, mapGrid.GetTileRef(tilePos).Tile);

                            // TODO: This is gonna allocate out the ass.
                            var anchored = mapGrid.GetAnchoredEntities(tilePos).ToArray();

                            foreach (var ent in anchored)
                            {
                                var xform = xformQuery.GetComponent(ent);
                                xform.Anchored = false;
                                gridComp.AnchorEntity(xform);
                            }
                        }

                        // Update lookup ents
                        // Needs to be done before setting old tiles as they will be re-parented to the map.
                        // TODO: Combine tiles into larger rectangles or something; this is gonna be the killer bit.
                        foreach (var tile in node.Indices)
                        {
                            var tilePos = offset + tile;
                            var bounds = _lookup.GetLocalBounds(tilePos, mapGrid.TileSize);

                            foreach (var ent in _lookup.GetEntitiesIntersecting(mapGrid.Index, tilePos, LookupFlags.None))
                            {
                                // Consider centre of entity position maybe?
                                var entXform = xformQuery.GetComponent(ent);

                                if (entXform.ParentUid != mapGrid.GridEntityId ||
                                    !bounds.Contains(entXform.LocalPosition)) continue;

                                entXform.AttachParent(splitXform);
                            }
                        }

                        _nodes[mapGrid.GridEntityId][node.Group.Chunk.Indices].Nodes.Remove(node);
                    }

                    // Set tiles on old grid
                    foreach (var node in group)
                    {
                        var offset = node.Group.Chunk.Indices * node.Group.Chunk.ChunkSize;

                        // TODO: Use the group version
                        foreach (var tile in node.Indices)
                        {
                            mapGrid.SetTile(offset + tile, Tile.Empty);
                        }
                    }

                    GenerateSplitNodes((IMapGridInternal) splitGrid);
                    SendNodeDebug(splitGrid.GridEntityId);
                }

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
            }

            _splitting = false;
            SendNodeDebug(mapGrid.GridEntityId);
        }

        private void GenerateSplitNodes(IMapGridInternal grid)
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
        private ChunkNodeGroup CreateNodes(EntityUid gridEuid, IMapGridInternal grid, MapChunk chunk)
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
            ChunkSplitNode? neighborNode = null;
            MapChunk? neighborChunk = null;

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

        internal override void GenerateSplitNode(EntityUid gridEuid, MapChunk chunk, bool checkSplit = true)
        {
            if (_splitting) return;

            var grid = (IMapGridInternal) IoCManager.Resolve<IMapManager>().GetGrid(gridEuid);
            var dirtyNodes = new HashSet<ChunkSplitNode>();

            Cleanup(gridEuid, chunk, dirtyNodes);
            var group = CreateNodes(gridEuid, grid, chunk);
            _nodes[grid.GridEntityId][chunk.Indices] = group;

            foreach (var chunkNode in group.Nodes)
            {
                dirtyNodes.Add(chunkNode);
            }

            if (checkSplit)
                CheckSplits(gridEuid, dirtyNodes);
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
                            node.GetCentre() + (node.Group.Chunk.Indices * node.Group.Chunk.ChunkSize),
                            neighbor.GetCentre() + (neighbor.Group.Chunk.Indices * neighbor.Group.Chunk.ChunkSize)));
                    }
                }

                msg.Nodes.Add(index, list);
            }

            foreach (var session in _subscribedSessions)
            {
                RaiseNetworkEvent(msg, session.ConnectedClient);
            }
        }

        private sealed class ChunkNodeGroup
        {
            internal MapChunk Chunk = default!;
            public HashSet<ChunkSplitNode> Nodes = new();
        }

        private sealed class ChunkSplitNode
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
