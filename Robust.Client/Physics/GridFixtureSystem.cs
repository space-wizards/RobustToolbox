using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Robust.Client.Physics
{
    internal sealed class GridFixtureSystem : SharedGridFixtureSystem
    {
        [Dependency] private readonly IOverlayManager _overlay = default!;
        [Dependency] private readonly IMapManager _map = default!;

        public bool EnableDebug
        {
            get => _enableDebug;
            set
            {
                if (_enableDebug == value) return;

                Sawmill.Info($"Set grid fixture debug to {value}");
                _enableDebug = value;

                if (_enableDebug)
                {
                    var overlay = new GridSplitNodeOverlay(EntityManager, _map, this);
                    _overlay.AddOverlay(overlay);
                    RaiseNetworkEvent(new RequestGridNodesMessage());
                }
                else
                {
                    _overlay.RemoveOverlay<GridSplitNodeOverlay>();
                    RaiseNetworkEvent(new StopGridNodesMessage());
                }
            }
        }

        private bool _enableDebug = false;
        private readonly Dictionary<EntityUid, Dictionary<Vector2i, List<List<Vector2i>>>> _nodes = new();
        private readonly Dictionary<EntityUid, List<(Vector2, Vector2)>> _connections = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<ChunkSplitDebugMessage>(OnDebugMessage);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _nodes.Clear();
            _connections.Clear();
        }

        private void OnDebugMessage(ChunkSplitDebugMessage ev)
        {
            Sawmill.Info($"Received grid fixture debug data");
            if (!_enableDebug) return;

            _nodes[ev.Grid] = ev.Nodes;
            _connections[ev.Grid] = ev.Connections;
        }

        private sealed class GridSplitNodeOverlay : Overlay
        {
            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            private IEntityManager _entManager;
            private IMapManager _mapManager;
            private GridFixtureSystem _system;

            public GridSplitNodeOverlay(IEntityManager entManager, IMapManager mapManager, GridFixtureSystem system)
            {
                _entManager = entManager;
                _mapManager = mapManager;
                _system = system;
            }

            protected internal override void Draw(in OverlayDrawArgs args)
            {
                var worldHandle = args.WorldHandle;
                var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

                foreach (var iGrid in _mapManager.FindGridsIntersecting(args.MapId, args.WorldBounds))
                {
                    // May not have received nodes yet.
                    if (!_system._nodes.TryGetValue(iGrid.GridEntityId, out var nodes)) continue;

                    var gridXform = xformQuery.GetComponent(iGrid.GridEntityId);
                    worldHandle.SetTransform(gridXform.WorldMatrix);
                    var chunkEnumerator = iGrid.GetMapChunks(args.WorldBounds);

                    while (chunkEnumerator.MoveNext(out var chunk))
                    {
                        if (!nodes.TryGetValue(chunk.Indices, out var chunkNodes)) continue;

                        for (var i = 0; i < chunkNodes.Count; i++)
                        {
                            var group = chunkNodes[i];
                            var offset = chunk.Indices * chunk.ChunkSize;
                            var color = GetColor(chunk, i);

                            foreach (var index in group)
                            {
                                worldHandle.DrawRect(new Box2(offset + index, offset + index + 1).Enlarged(-0.1f), color);
                            }
                        }
                    }

                    var connections = _system._connections[iGrid.GridEntityId];

                    foreach (var (start, end) in connections)
                    {
                        worldHandle.DrawLine(start, end, Color.Aquamarine);
                    }
                }

                worldHandle.SetTransform(Matrix3.Identity);
            }

            private Color GetColor(MapChunk chunk, int index)
            {
                // Just want something that doesn't give similar indices at 0,0 but is also deterministic.
                // Add an offset to yIndex so we at least have some colour that isn't grey at 0,0
                var actualIndex = chunk.Indices.X * 20 + (chunk.Indices.Y + 20) * 35 + index * 50;

                var red = (byte) (actualIndex % 255);
                var green = (byte) (actualIndex * 20 % 255);
                var blue = (byte) (actualIndex * 30 % 255);

                return new Color(red, green, blue, 85);
            }
        }
    }
}
