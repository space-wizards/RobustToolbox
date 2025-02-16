#if DEBUG
using Robust.Client.ComponentTrees;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects
{
    internal sealed class DebugLightTreeSystem : EntitySystem
    {
        private DebugLightOverlay? _lightOverlay;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;

                _enabled = value;
                var overlayManager = IoCManager.Resolve<IOverlayManager>();

                if (_enabled)
                {
                    _lightOverlay = new DebugLightOverlay(
                        EntityManager.System<EntityLookupSystem>(),
                        IoCManager.Resolve<IEyeManager>(),
                        IoCManager.Resolve<IMapManager>(),
                        EntityManager.System<LightTreeSystem>());

                    overlayManager.AddOverlay(_lightOverlay);
                }
                else
                {
                    overlayManager.RemoveOverlay(_lightOverlay!);
                    _lightOverlay = null;
                }
            }
        }

        private bool _enabled;

        private sealed class DebugLightOverlay : Overlay
        {
            private EntityLookupSystem _lookup;
            private IEyeManager _eyeManager;
            private IMapManager _mapManager;

            private LightTreeSystem _trees;

            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            public DebugLightOverlay(EntityLookupSystem lookup, IEyeManager eyeManager, IMapManager mapManager, LightTreeSystem trees)
            {
                _lookup = lookup;
                _eyeManager = eyeManager;
                _mapManager = mapManager;
                _trees = trees;
            }

            protected internal override void Draw(in OverlayDrawArgs args)
            {
                var map = args.MapId;
                if (map == MapId.Nullspace) return;

                foreach (var (_, treeComp) in _trees.GetIntersectingTrees(map, args.WorldBounds))
                {
                    foreach (var entry in treeComp.Tree)
                    {
                        var aabb = _lookup.GetWorldAABB(entry.Uid, entry.Transform);
                        if (!aabb.Intersects(args.WorldAABB)) continue;

                        args.WorldHandle.DrawRect(aabb, Color.Green.WithAlpha(0.1f));
                    }
                }
            }
        }
    }
}
#endif
