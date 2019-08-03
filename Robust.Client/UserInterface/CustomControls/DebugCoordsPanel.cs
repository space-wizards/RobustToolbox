using System.Text;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.State;
using Robust.Client.Player;
using Robust.Client.State.States;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    internal class DebugCoordsPanel : Panel
    {
        private readonly IPlayerManager playerManager;
        private readonly IEyeManager eyeManager;
        private readonly IInputManager inputManager;
        private readonly IResourceCache resourceCache;
        private readonly IStateManager stateManager;
        private readonly IClyde _displayManager;
        private readonly IMapManager _mapManager;

        private Label contents;

        //TODO: Think about a factory for this
        public DebugCoordsPanel(IPlayerManager playerMan,
            IEyeManager eyeMan,
            IInputManager inputMan,
            IResourceCache resCache,
            IStateManager stateMan,
            IClyde displayMan,
            IMapManager mapMan)
        {
            playerManager = playerMan;
            eyeManager = eyeMan;
            inputManager = inputMan;
            resourceCache = resCache;
            stateManager = stateMan;
            _displayManager = displayMan;
            _mapManager = mapMan;

            PerformLayout();
        }

        protected override void Initialize()
        {
            base.Initialize();

            contents = new Label();
        }

        private void PerformLayout()
        {
            SizeFlagsHorizontal = SizeFlags.None;

            contents = new Label
            {
                FontColorShadowOverride = Color.Black,
                MarginTop = 5,
                MarginLeft = 5
            };
            AddChild(contents);

            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = new Color(67, 105, 255, 138),
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
            int mouseWorldGrid;
            GridCoordinates mouseWorldPos;
            ScreenCoordinates worldToScreen;
            IEntity mouseEntity = null;
            TileRef tile;
            try
            {
                var coords = eyeManager.ScreenToWorld(new ScreenCoordinates(mouseScreenPos));
                mouseWorldMap = (int) _mapManager.GetGrid(coords.GridID).ParentMapId;
                mouseWorldGrid = (int) coords.GridID;
                mouseWorldPos = coords;
                worldToScreen = eyeManager.WorldToScreen(coords);
                if (stateManager.CurrentState is GameScreen gameScreen)
                {
                    mouseEntity = gameScreen.GetEntityUnderPosition(coords);
                }

                tile = _mapManager.GetGrid(coords.GridID).GetTileRef(coords);
            }
            catch
            {
                mouseWorldPos = eyeManager.ScreenToWorld(mouseScreenPos);
                mouseWorldGrid = 0;
                mouseWorldMap = 0;
                worldToScreen = new ScreenCoordinates();
                tile = new TileRef();
            }

            stringBuilder.AppendFormat(@"Positioning Debug:
Screen Size: {0}
Mouse Pos:
    Screen: {1}
    World: {2}
    W2S: {3}
    Grid: {4}
    Tile: {8}
    Map: {5}
    Entity: {6}
    GUI: {7}
", screenSize, mouseScreenPos, mouseWorldPos, worldToScreen, mouseWorldGrid, mouseWorldMap, mouseEntity,
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
