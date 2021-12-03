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
                    var overlayManager = IoCManager.Resolve<IOverlayManager>();
                    _overlay = new SpriteBoundsOverlay(EntitySystem.Get<RenderingTreeSystem>(), IoCManager.Resolve<IEyeManager>());
                    overlayManager.AddOverlay(_overlay);
                }
                else
                {
                    if (_overlay == null) return;
                    var overlayManager = IoCManager.Resolve<IOverlayManager>();
                    overlayManager.RemoveOverlay(_overlay);
                    _overlay = null;
                }
            }
        }

        private bool _enabled;
    }

    public class SpriteBoundsOverlay : Overlay
    {
        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        private readonly IEyeManager _eyeManager = default!;
        private RenderingTreeSystem _renderTree;

        public SpriteBoundsOverlay(RenderingTreeSystem renderTree, IEyeManager eyeManager)
        {
            _renderTree = renderTree;
            _eyeManager = eyeManager;
        }

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            var handle = args.WorldHandle;
            var currentMap = _eyeManager.CurrentMap;
            var viewport = _eyeManager.GetWorldViewbounds();

            foreach (var comp in _renderTree.GetRenderTrees(currentMap, viewport))
            {
                var localAABB = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(comp.Owner).InvWorldMatrix.TransformBox(viewport);

                foreach (var sprite in comp.SpriteTree.QueryAabb(localAABB))
                {
                    var worldPos = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(sprite.Owner).WorldPosition;
                    var bounds = sprite.CalculateBoundingBox(worldPos);
                    handle.DrawRect(bounds, Color.Red.WithAlpha(0.2f));
                    handle.DrawRect(bounds.Scale(0.2f).Translated(-new Vector2(0f, bounds.Extents.Y)), Color.Blue.WithAlpha(0.5f));
                }
            }
        }
    }
}
