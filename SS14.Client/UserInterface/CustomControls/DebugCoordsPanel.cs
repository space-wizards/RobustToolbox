using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Player;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Reflection;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.CustomControls
{
    [Reflect(false)]
    class DebugCoordsPanel : Control
    {
        [Dependency]
        readonly IPlayerManager playerManager;

        [Dependency]
        readonly IEyeManager eyeManager;
        [Dependency]
        readonly IInputManager inputManager;

        private Label contents;

        protected override Godot.Control SpawnSceneControl()
        {
            return LoadScene("res://Scenes/DebugCoordsPanel/DebugCoordsPanel.tscn");
        }

        protected override void Initialize()
        {
            base.Initialize();

            Visible = false;

            IoCManager.InjectDependencies(this);
            contents = GetChild<Label>("Label");
        }

        protected override void Update(FrameEventArgs args)
        {
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
            Vector2 mouseWorldPos;
            try
            {
                var coords = eyeManager.ScreenToWorld(new ScreenCoordinates(mouseScreenPos, entityTransform.MapID));
                mouseWorldMap = (int)coords.MapID;
                mouseWorldGrid = (int)coords.GridID;
                mouseWorldPos = coords.Position;
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

Mouse Pos:
    Screen: {mouseScreenPos}
    World: {mouseWorldPos}
    Grid: {mouseWorldGrid}
    Map: {mouseWorldMap}";
        }
    }
}
