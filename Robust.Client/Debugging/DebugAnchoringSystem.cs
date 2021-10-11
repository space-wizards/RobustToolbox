#if DEBUG
using System.Text;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Debugging
{
    public sealed class DebugAnchoringSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        private Label? _label;

        private (GridId GridId, TileRef Tile)? _hovered;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;

                _enabled = value;

                if (_enabled)
                {
                    _label = new Label();
                    IoCManager.Resolve<IUserInterfaceManager>().StateRoot.AddChild(_label);
                }
                else
                {
                    IoCManager.Resolve<IUserInterfaceManager>().StateRoot.RemoveChild(_label!);
                    _label = null;
                    _hovered = null;
                }
            }
        }

        private bool _enabled;

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            if (!Enabled) return;

            if (_label == null)
            {
                DebugTools.Assert($"Debug Label for {nameof(DebugAnchoringSystem)} is null!");
                return;
            }

            var mouseSpot = _inputManager.MouseScreenPosition;
            var spot = _eyeManager.ScreenToMap(mouseSpot);

            if (!_mapManager.TryFindGridAt(spot, out var grid))
            {
                _label.Text = string.Empty;
                _hovered = null;
                return;
            }

            var tile = grid.GetTileRef(spot);
            _label.Position = mouseSpot.Position + new Vector2(32, 0);

            if (_hovered?.GridId == grid.Index && _hovered?.Tile == tile) return;

            _hovered = (grid.Index, tile);

            var text = new StringBuilder();

            foreach (var ent in grid.GetAnchoredEntities(spot))
            {
                if (!EntityManager.TryGetEntity(ent, out var entity))
                {
                    text.AppendLine($"uid: {ent}, invalid");
                }
                else
                {
                    text.AppendLine($"uid: {ent}, {entity.Name}");
                }
            }

            _label.Text = text.ToString();
        }
    }
}
#endif
