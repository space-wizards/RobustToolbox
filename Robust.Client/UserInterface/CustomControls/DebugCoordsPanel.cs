using System.Text;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.State;
using Robust.Client.Player;
using Robust.Client.ResourceManagement.ResourceTypes;
using Robust.Client.State.States;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Client.ResourceManagement;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    internal class DebugCoordsPanel : Panel
    {
        [Dependency] readonly IPlayerManager playerManager;
        [Dependency] readonly IEyeManager eyeManager;
        [Dependency] readonly IInputManager inputManager;
        [Dependency] readonly IResourceCache resourceCache;
        [Dependency] readonly IStateManager stateManager;

        [Dependency] private readonly IDisplayManager _displayManager;

        private Label contents;

        protected override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);

            SizeFlagsHorizontal = SizeFlags.None;

            contents = new Label
            {
                FontOverride =
                    new VectorFont(resourceCache.GetResource<FontResource>(new ResourcePath("/Fonts/CALIBRI.TTF")), 12),
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

        protected override void FrameUpdate(RenderFrameEventArgs args)
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
                mouseWorldMap = (int) coords.MapID;
                mouseWorldGrid = (int) coords.GridID;
                mouseWorldPos = coords;
                worldToScreen = eyeManager.WorldToScreen(coords);
                if (stateManager.CurrentState is GameScreen gameScreen)
                {
                    mouseEntity = gameScreen.GetEntityUnderPosition(coords);
                }

                tile = coords.Grid.GetTile(coords);
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
