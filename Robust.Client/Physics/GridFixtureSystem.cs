using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Physics
{
    internal sealed class GridFixtureSystem : SharedGridFixtureSystem
    {
        public bool EnableDebug
        {
            get => _enableDebug;
            set
            {
                if (_enableDebug == value) return;

                _enableDebug = value;
                var overlayManager = IoCManager.Resolve<IOverlayManager>();

                if (_enableDebug)
                {
                    overlayManager.AddOverlay(new GridSplitNodeOverlay());
                    RaiseNetworkEvent(new RequestGridNodesMessage());
                }
                else
                {
                    overlayManager.RemoveOverlay<GridSplitNodeOverlay>();
                }
            }
        }

        private bool _enableDebug = false;
        private Dictionary<EntityUid, Dictionary<Vector2i, List<List<Vector2i>>>> _nodes = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<ChunkSplitDebugMessage>(OnDebugMessage);
        }

        private void OnDebugMessage(ChunkSplitDebugMessage ev)
        {
            if (!_enableDebug) return;

            _nodes[ev.Grid] = ev.Nodes;
        }

        private sealed class GridSplitNodeOverlay : Overlay
        {
            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            private IEntityManager _entManager = default!;
            private IMapManager _mapManager = default!;
            private GridFixtureSystem _system = default!;

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
                    var grid = (MapGrid)iGrid;
                    grid.GetMapChunks(args.WorldBounds, out var chunkEnumerator);

                    while (chunkEnumerator.MoveNext(out var chunk))
                    {
                        if (!nodes.TryGetValue(chunk.Indices, out var chunkNodes)) continue;

                        for (var i = 0; i < chunkNodes.Count; i++)
                        {
                            var group = chunkNodes[i];

                            foreach (var index in group)
                            {
                                worldHandle.DrawRect(new Box2(index, index + 1).Enlarged(-0.1f), GetColor(chunk, i));
                            }
                        }
                    }
                }
            }

            private Color GetColor(MapChunk chunk, int index)
            {
                return Color.Red;
            }
        }
    }
}
