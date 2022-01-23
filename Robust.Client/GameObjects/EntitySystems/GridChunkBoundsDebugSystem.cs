using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    public class GridChunkBoundsDebugSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;

        private GridChunkBoundsOverlay? _overlay;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;

                _enabled = value;

                if (_enabled)
                {
                    DebugTools.Assert(_overlay == null);
                    _overlay = new GridChunkBoundsOverlay(
                        EntityManager,
                        _eyeManager,
                        _mapManager);

                    _overlayManager.AddOverlay(_overlay);
                }
                else
                {
                    _overlayManager.RemoveOverlay(_overlay!);
                    _overlay = null;
                }
            }
        }

        private bool _enabled;
    }

    internal sealed class GridChunkBoundsOverlay : Overlay
    {
        private readonly IEntityManager _entityManager;
        private readonly IEyeManager _eyeManager;
        private readonly IMapManager _mapManager;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public GridChunkBoundsOverlay(IEntityManager entManager, IEyeManager eyeManager, IMapManager mapManager)
        {
            _entityManager = entManager;
            _eyeManager = eyeManager;
            _mapManager = mapManager;
        }

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            var currentMap = _eyeManager.CurrentMap;
            var viewport = args.WorldBounds;
            var worldHandle = args.WorldHandle;

            foreach (var grid in _mapManager.FindGridsIntersecting(currentMap, viewport))
            {
                var gridInternal = (IMapGridInternal)grid;
                var worldMatrix = _entityManager.GetComponent<TransformComponent>(grid.GridEntityId).WorldMatrix;
                worldHandle.SetTransform(worldMatrix);
                var transform = new Transform(Vector2.Zero, Angle.Zero);

                gridInternal.GetMapChunks(viewport, out var chunkEnumerator);

                while (chunkEnumerator.MoveNext(out var chunk))
                {
                    foreach (var fixture in chunk.Fixtures)
                    {
                        var poly = (PolygonShape) fixture.Shape;

                        var verts = new Vector2[poly.Vertices.Length];

                        for (var i = 0; i < poly.Vertices.Length; i++)
                        {
                            verts[i] = Transform.Mul(transform, poly.Vertices[i]);
                        }

                        worldHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, Color.Green.WithAlpha(0.2f));

                        for (var i = 0; i < fixture.Shape.ChildCount; i++)
                        {
                            var aabb = fixture.Shape.ComputeAABB(transform, i);

                            args.WorldHandle.DrawRect(aabb, Color.Red.WithAlpha(0.5f), false);
                        }
                    }
                }
            }
        }
    }
}
