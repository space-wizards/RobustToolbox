using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    public sealed class GridChunkBoundsDebugSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;

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
                        _mapManager,
                        _transform,
                        _map);

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
        private readonly SharedTransformSystem _transformSystem;
        private readonly SharedMapSystem _mapSystem;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        private List<Entity<MapGridComponent>> _grids = new();

        public GridChunkBoundsOverlay(IEntityManager entManager, IEyeManager eyeManager, IMapManager mapManager, SharedTransformSystem transformSystem, SharedMapSystem mapSystem)
        {
            _entityManager = entManager;
            _eyeManager = eyeManager;
            _mapManager = mapManager;
            _transformSystem = transformSystem;
            _mapSystem = mapSystem;
        }

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            var currentMap = args.MapId;
            var viewport = args.WorldBounds;
            var worldHandle = args.WorldHandle;

            var fixturesQuery = _entityManager.GetEntityQuery<FixturesComponent>();
            _grids.Clear();
            _mapManager.FindGridsIntersecting(currentMap, viewport, ref _grids);
            foreach (var grid in _grids)
            {
                var worldMatrix = _transformSystem.GetWorldMatrix(grid);
                worldHandle.SetTransform(worldMatrix);
                var transform = new Transform(Vector2.Zero, Angle.Zero);
                var fixtures = fixturesQuery.Comp(grid.Owner);

                var chunkEnumerator = _mapSystem.GetMapChunks(grid.Owner, grid.Comp, viewport);

                while (chunkEnumerator.MoveNext(out var chunk))
                {
                    foreach (var id in chunk.Fixtures)
                    {
                        var fixture = fixtures.Fixtures[id];
                        var poly = (PolygonShape) fixture.Shape;

                        var verts = new Vector2[poly.VertexCount];

                        for (var i = 0; i < poly.VertexCount; i++)
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

            worldHandle.SetTransform(Matrix3x2.Identity);
        }
    }
}
