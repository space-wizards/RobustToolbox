using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    public class GridChunkBoundsDebugSystem : EntitySystem
    {
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
                        IoCManager.Resolve<IEntityManager>(),
                        IoCManager.Resolve<IEyeManager>(),
                        IoCManager.Resolve<IMapManager>());

                    IoCManager.Resolve<IOverlayManager>().AddOverlay(_overlay);
                }
                else
                {
                    IoCManager.Resolve<IOverlayManager>().RemoveOverlay(_overlay!);
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
            var viewport = _eyeManager.GetWorldViewport();

            foreach (var grid in _mapManager.FindGridsIntersecting(currentMap, viewport))
            {
                if (!_entityManager.TryGetComponent<PhysicsComponent>(grid.GridEntityId, out var body)) continue;

                var transform = body.GetTransform();

                foreach (var fixture in body.Fixtures)
                {
                    for (var i = 0; i < fixture.Shape.ChildCount; i++)
                    {
                        var aabb = fixture.Shape.ComputeAABB(transform, i);

                        args.WorldHandle.DrawRect(aabb, Color.Green.WithAlpha(0.2f));
                        args.WorldHandle.DrawRect(aabb, Color.Red.WithAlpha(0.5f), false);
                    }
                }
            }
        }
    }
}
