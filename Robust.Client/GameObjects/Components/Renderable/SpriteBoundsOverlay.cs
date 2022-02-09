using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Clyde;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    public sealed class ShowSpriteBBCommand : IConsoleCommand
    {
        public string Command => "showspritebb";
        public string Description => "Toggle whether sprite bounds are shown";
        public string Help => $"{Command}";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<SpriteBoundsSystem>().Enabled ^= true;
        }
    }

    public sealed class SpriteBoundsSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eye = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly RenderingTreeSystem _renderingTree = default!;

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
                    _overlay = new SpriteBoundsOverlay(_renderingTree, _eye, _entityManager);
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

    public class SpriteBoundsOverlay : Overlay
    {
        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        private readonly IEyeManager _eyeManager;
        private readonly IEntityManager _entityManager;
        private RenderingTreeSystem _renderTree;

        public SpriteBoundsOverlay(RenderingTreeSystem renderTree, IEyeManager eyeManager, IEntityManager entityManager)
        {
            _renderTree = renderTree;
            _eyeManager = eyeManager;
            _entityManager = entityManager;
        }

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            var handle = args.WorldHandle;
            var currentMap = _eyeManager.CurrentMap;
            var viewport = _eyeManager.GetWorldViewbounds();

            foreach (var comp in _renderTree.GetRenderTrees(currentMap, viewport))
            {
                var localAABB = _entityManager.GetComponent<TransformComponent>(comp.Owner).InvWorldMatrix.TransformBox(viewport);

                foreach (var sprite in comp.SpriteTree.QueryAabb(localAABB))
                {
                    var (worldPos, worldRot) = _entityManager.GetComponent<TransformComponent>(sprite.Owner).GetWorldPositionRotation();
                    var bounds = sprite.CalculateRotatedBoundingBox(worldPos, worldRot);

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
}
