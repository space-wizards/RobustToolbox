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
using Robust.Shared.Interfaces.GameObjects;
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

            int mouseWorldMap;
            GridCoordinates mouseWorldPos;
            TileRef tile;
            try
            {
                var coords = eyeManager.ScreenToWorld(new ScreenCoordinates(mouseScreenPos));
                mouseWorldMap = (int) _mapManager.GetGrid(coords.GridID).ParentMapId;
                mouseWorldPos = coords;

                tile = _mapManager.GetGrid(coords.GridID).GetTileRef(coords);
            }
            catch
            {
                mouseWorldPos = GridCoordinates.InvalidGrid;
                mouseWorldMap = 0;
                tile = new TileRef();
            }

            stringBuilder.AppendFormat(@"Positioning Debug:
Screen Size: {0}
Mouse Pos:
    Screen: {1}
    World: {2}
    Map: {3}
    Tile: {5}
    GUI: {4}", screenSize, mouseScreenPos, mouseWorldPos, mouseWorldMap,
                UserInterfaceManager.CurrentlyHovered, tile);

            stringBuilder.AppendLine("\nAttached Entity:");
            if (playerManager.LocalPlayer?.ControlledEntity == null)
            {
                stringBuilder.AppendLine("No attached entity.");

            }
            else
            {
                var entityTransform = playerManager.LocalPlayer.ControlledEntity.Transform;
                var playerWorldOffset = entityTransform.WorldPosition;
                var playerScreen = eyeManager.WorldToScreen(playerWorldOffset);

                stringBuilder.AppendFormat(@"    World: {0}
    Screen: {1}
    Grid: {2}
    Map: {3}", playerWorldOffset, playerScreen, entityTransform.GridID, entityTransform.MapID);
            }

            contents.Text = stringBuilder.ToString();
            MinimumSizeChanged();
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return new Vector2(175, contents.CombinedMinimumSize.Y + 10);
        }
    }
}
