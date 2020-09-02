using System.Text;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.State;
using Robust.Client.Player;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.CustomControls
{
    internal class DebugCoordsPanel : PanelContainer
    {
        private readonly IPlayerManager playerManager;
        private readonly IEyeManager eyeManager;
        private readonly IInputManager inputManager;
        private readonly IStateManager stateManager;
        private readonly IClyde _displayManager;
        private readonly IMapManager _mapManager;

        private readonly Label contents;
        private UIBox2i _uiBox;

        //TODO: Think about a factory for this
        public DebugCoordsPanel(IPlayerManager playerMan,
            IEyeManager eyeMan,
            IInputManager inputMan,
            IStateManager stateMan,
            IClyde displayMan,
            IMapManager mapMan)
        {
            playerManager = playerMan;
            eyeManager = eyeMan;
            inputManager = inputMan;
            stateManager = stateMan;
            _displayManager = displayMan;
            _mapManager = mapMan;

            SizeFlagsHorizontal = SizeFlags.None;

            contents = new Label
            {
                FontColorShadowOverride = Color.Black,
            };
            AddChild(contents);

            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = new Color(67, 105, 255, 138),
                ContentMarginLeftOverride = 5,
                ContentMarginTopOverride = 5
            };

            MouseFilter = contents.MouseFilter = MouseFilterMode.Ignore;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            if (!VisibleInTree)
            {
                return;
            }

            var stringBuilder = new StringBuilder();

            var mouseScreenPos = inputManager.MouseScreenPosition;
            var screenSize = _displayManager.ScreenSize;

            MapCoordinates mouseWorldMap;
            GridCoordinates mouseGridPos;
            TileRef tile;

            mouseWorldMap = eyeManager.ScreenToMap(mouseScreenPos);

            if (_mapManager.TryFindGridAt(mouseWorldMap, out var mouseGrid))
            {
                mouseGridPos = mouseGrid.MapToGrid(mouseWorldMap);
                tile = mouseGrid.GetTileRef(mouseGridPos);
            }
            else
            {
                mouseGridPos = new GridCoordinates(mouseWorldMap.Position, GridId.Invalid);
                tile = default;
            }

            var controlHovered = UserInterfaceManager.CurrentlyHovered;

            stringBuilder.AppendFormat(@"Positioning Debug:
Screen Size: {0}
Mouse Pos:
    Screen: {1}
    {2}
    {3}
    {4}
    GUI: {5}", screenSize, mouseScreenPos, mouseWorldMap, mouseGridPos,
                tile, controlHovered);

            stringBuilder.AppendLine("\nAttached Entity:");
            if (playerManager.LocalPlayer?.ControlledEntity == null)
            {
                stringBuilder.AppendLine("No attached entity.");

            }
            else
            {
                var entityTransform = playerManager.LocalPlayer.ControlledEntity.Transform;
                var playerWorldOffset = entityTransform.MapPosition;
                var playerScreen = eyeManager.WorldToScreen(playerWorldOffset.Position);

                var playerGridPos = playerManager.LocalPlayer.ControlledEntity.Transform.GridPosition;

                stringBuilder.AppendFormat(@"    Screen: {0}
    {1}
    {2}
    EntityUid: {3}", playerScreen, playerWorldOffset, playerGridPos, entityTransform.Owner.Uid);
            }

            if (controlHovered != null)
            {
                _uiBox = UIBox2i.FromDimensions(controlHovered.GlobalPixelPosition, controlHovered.PixelSize);
            }

            contents.Text = stringBuilder.ToString();
            MinimumSizeChanged();
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (!VisibleInTree)
            {
                return;
            }

            handle.DrawRect(_uiBox, Color.Red, false);
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return new Vector2(175, contents.CombinedMinimumSize.Y + 10);
        }
    }
}
