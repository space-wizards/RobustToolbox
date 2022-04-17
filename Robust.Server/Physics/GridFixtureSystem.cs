using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Robust.Server.Physics
{
    /// <summary>
    /// Handles generating fixtures for MapGrids.
    /// </summary>
    internal sealed class GridFixtureSystem : SharedGridFixtureSystem
    {
        private readonly Dictionary<EntityUid, Dictionary<Vector2i, ChunkNodeGroup>> _nodes = new();

        /// <summary>
        /// Sessions to receive nodes for debug purposes.
        /// </summary>
        private readonly HashSet<ICommonSession> _subscribedSessions = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);
            SubscribeNetworkEvent<RequestGridNodesMessage>(OnDebugRequest);
        }

        private void OnDebugRequest(RequestGridNodesMessage msg, EntitySessionEventArgs args)
        {
            var adminManager = IoCManager.Resolve<IConGroupController>();
            var pSession = (PlayerSession)args.SenderSession;

            if (!adminManager.CanCommand(pSession, ShowGridNodesCommand)) return;


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

        internal override void GenerateSplitNode(EntityUid gridEuid, MapChunk chunk)
        {
            var nodes = _nodes[gridEuid];
            nodes.Remove(chunk.Indices);
            var group = new ChunkNodeGroup();
            var tiles = new HashSet<Vector2i>(chunk.ChunkSize * chunk.ChunkSize);

            for (var x = 0; x < chunk.ChunkSize; x++)
            {
                for (var y = 0; y < chunk.ChunkSize; y++)
                {
                    tiles.Add(new Vector2i(x, y));
                }
            }

            var frontier = new Queue<Vector2i>();
            var node = new ChunkSplitNode();

            // Simple BFS search
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
                    node = new ChunkSplitNode();

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

            nodes[chunk.Indices] = group;

            SendNodeDebug(gridEuid);
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

                foreach (var node in group.Nodes)
                {
                    list.Add(node.Indices.ToList());
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
            public HashSet<ChunkSplitNode> Nodes = new();
        }

        private sealed class ChunkSplitNode
        {
            public HashSet<Vector2i> Indices { get; set; } = new();
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
