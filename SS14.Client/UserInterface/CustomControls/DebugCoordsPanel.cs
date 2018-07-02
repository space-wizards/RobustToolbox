using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Player;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Reflection;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Interfaces.State;
using SS14.Client.State.States;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Utility;

namespace SS14.Client.UserInterface.CustomControls
{
    [Reflect(false)]
    class DebugCoordsPanel : Panel
    {
        [Dependency]
        readonly IPlayerManager playerManager;
        [Dependency]
        readonly IEyeManager eyeManager;
        [Dependency]
        readonly IInputManager inputManager;
        [Dependency]
        readonly IResourceCache resourceCache;
        [Dependency]
        readonly IStateManager stateManager;

        private Label contents;

        protected override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);

            SizeFlagsHorizontal = SizeFlags.None;

            contents = new Label();
            AddChild(contents);
            contents.AddFontOverride("font", resourceCache.GetResource<FontResource>(new ResourcePath("/Fonts/CALIBRI.TTF")).MakeDefault());
            contents.AddColorOverride("font_color_shadow", Color.Black);
            contents.MarginLeft = contents.MarginTop = 5;

            var bg = new StyleBoxFlat
            {
                BackgroundColor = new Color(67, 105, 255, 138),
            };
            AddStyleBoxOverride("panel", bg);

            MouseFilter = contents.MouseFilter = MouseFilterMode.Ignore;
        }

        protected override void Update(ProcessFrameEventArgs args)
        {
            if (!Visible)
            {
                return;
            }
            if (playerManager.LocalPlayer?.ControlledEntity == null)
            {
                contents.Text = "No attached entity.";
                return;
            }

            var entityTransform = playerManager.LocalPlayer.ControlledEntity.GetComponent<ITransformComponent>();
            var playerWorldOffset = entityTransform.WorldPosition;
            var playerScreen = eyeManager.WorldToScreen(playerWorldOffset);

            var mouseScreenPos = inputManager.MouseScreenPosition;
            int mouseWorldMap;
            int mouseWorldGrid;
            GridLocalCoordinates mouseWorldPos;
            IEntity mouseEntity = null;
            try
            {
                var coords = eyeManager.ScreenToWorld(new ScreenCoordinates(mouseScreenPos));
                mouseWorldMap = (int)coords.MapID;
                mouseWorldGrid = (int)coords.GridID;
                mouseWorldPos = coords;
                if (stateManager.CurrentState is GameScreen gameScreen)
                {
                    mouseEntity = gameScreen.GetEntityUnderPosition(coords);
                }
            }
            catch
            {
                mouseWorldPos = eyeManager.ScreenToWorld(mouseScreenPos);
                mouseWorldGrid = 0;
                mouseWorldMap = 0;
            }

            contents.Text = $@"Positioning Debug:
Character Pos:
    World: {playerWorldOffset}
    Screen: {playerScreen}
    Grid: {entityTransform.GridID}
    Map: {entityTransform.MapID}

Mouse Pos:
    Screen: {mouseScreenPos}
    World: {mouseWorldPos}
    Grid: {mouseWorldGrid}
    Map: {mouseWorldMap}
    Entity: {mouseEntity}";

            MinimumSizeChanged();
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return new Vector2(175, contents.CombinedMinimumSize.Y + 10);
        }
    }
}
