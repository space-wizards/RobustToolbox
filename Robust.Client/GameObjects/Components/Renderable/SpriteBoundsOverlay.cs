using System.Numerics;
using Robust.Client.ComponentTrees;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    public sealed class ShowSpriteBBCommand : LocalizedCommands
    {
        public override string Command => "showspritebb";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<SpriteBoundsSystem>().Enabled ^= true;
        }
    }

    public sealed class SpriteBoundsSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly SpriteTreeSystem _spriteTree = default!;

        private SpriteBoundsOverlay? _overlay;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;

                _enabled = value;

                if (_enabled)
                {
                    DebugTools.AssertNull(_overlay);
                    _overlay = new SpriteBoundsOverlay(_spriteTree, _entityManager);
                    _overlayManager.AddOverlay(_overlay);
                }
                else
                {
                    if (_overlay == null) return;
                    _overlayManager.RemoveOverlay(_overlay);
                    _overlay = null;
                }
            }
        }

        private bool _enabled;
    }

    public sealed class SpriteBoundsOverlay : Overlay
    {
        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        private readonly IEntityManager _entityManager;
        private SpriteTreeSystem _renderTree;

        public SpriteBoundsOverlay(SpriteTreeSystem renderTree, IEntityManager entityManager)
        {
            _renderTree = renderTree;
            _entityManager = entityManager;
        }

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            var handle = args.WorldHandle;
            var currentMap = args.MapId;
            var viewport = args.WorldBounds;

            foreach (var (sprite, xform) in _renderTree.QueryAabb(currentMap, viewport))
            {
                var (worldPos, worldRot) = xform.GetWorldPositionRotation();
                var bounds = sprite.CalculateRotatedBoundingBox(worldPos, worldRot, args.Viewport.Eye?.Rotation ?? default);

                // Get scaled down bounds used to indicate the "south" of a sprite.
                var localBound = bounds.Box;
                var smallLocal = localBound.Scale(0.2f).Translated(-new Vector2(0f, localBound.Extents.Y));
                var southIndicator = new Box2Rotated(smallLocal, bounds.Rotation, bounds.Origin);

                handle.DrawRect(bounds, Color.Red.WithAlpha(0.2f));
                handle.DrawRect(southIndicator, Color.Blue.WithAlpha(0.5f));
            }
        }
    }
}
