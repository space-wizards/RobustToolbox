using System.Collections.Generic;
using System.Numerics;
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
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;

        public bool EnableDebug
        {
            get => _enableDebug;
            set
            {
                if (_enableDebug == value) return;

                Log.Info($"Set grid fixture debug to {value}");
                _enableDebug = value;

                if (_enableDebug)
                {
                    var overlay = new GridSplitNodeOverlay(_mapManager, this, _transform, _map);
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

        private bool _enableDebug;
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
            Log.Info($"Received grid fixture debug data");
            if (!_enableDebug) return;

            var grid = GetEntity(ev.Grid);
            _nodes[grid] = ev.Nodes;
            _connections[grid] = ev.Connections;
        }

        private sealed class GridSplitNodeOverlay : Overlay
        {
            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            private readonly IMapManager _mapManager;
            private readonly GridFixtureSystem _system;
            private readonly SharedTransformSystem _transform;
            private readonly SharedMapSystem _map;

            public GridSplitNodeOverlay(IMapManager mapManager, GridFixtureSystem system, SharedTransformSystem transform, SharedMapSystem map)
            {
                _mapManager = mapManager;
                _system = system;
                _transform = transform;
                _map = map;
            }

            protected internal override void Draw(in OverlayDrawArgs args)
            {
                var worldHandle = args.WorldHandle;

                var state = (_system, _transform, args.WorldBounds, worldHandle);

                _mapManager.FindGridsIntersecting(args.MapId, args.WorldBounds, ref state,
                    (EntityUid uid, MapGridComponent grid,
                        ref (GridFixtureSystem system, SharedTransformSystem transform, Box2Rotated worldBounds, DrawingHandleWorld worldHandle) tuple) =>
                    {
                        // May not have received nodes yet.
                        if (!tuple.system._nodes.TryGetValue(uid, out var nodes))
                            return true;

                        tuple.worldHandle.SetTransform(tuple.transform.GetWorldMatrix(uid));
                        var chunkEnumerator = _map.GetMapChunks(uid, grid, tuple.worldBounds);

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
                                    tuple.worldHandle.DrawRect(new Box2(offset + index, offset + index + 1).Enlarged(-0.1f), color);
                                }
                            }
                        }

                        var connections = tuple.system._connections[uid];

                        foreach (var (start, end) in connections)
                        {
                            tuple.worldHandle.DrawLine(start, end, Color.Aquamarine);
                        }

                        static Color GetColor(MapChunk chunk, int index)
                        {
                            // Just want something that doesn't give similar indices at 0,0 but is also deterministic.
                            // Add an offset to yIndex so we at least have some colour that isn't grey at 0,0
                            var actualIndex = chunk.Indices.X * 20 + (chunk.Indices.Y + 20) * 35 + index * 50;

                            var red = (byte) (actualIndex % 255);
                            var green = (byte) (actualIndex * 20 % 255);
                            var blue = (byte) (actualIndex * 30 % 255);

                            return new Color(red, green, blue, 85);
                        }

                        return true;
                    }, true);

                worldHandle.SetTransform(Matrix3x2.Identity);
            }
        }
    }
}
