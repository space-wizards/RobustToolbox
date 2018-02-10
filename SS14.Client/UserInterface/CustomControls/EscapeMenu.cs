using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.State.States;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Reflection;

namespace SS14.Client.UserInterface.CustomControls
{
    [Reflect(false)]
    public class EscapeMenu : SS14Window
    {
        [Dependency]
        readonly IClientNetManager netManager;
        [Dependency]
        readonly IStateManager stateManager;

        protected override Godot.Control SpawnSceneControl()
        {
            return LoadScene("res://Scenes/EscapeMenu/EscapeMenu.tscn");
        }

        private BaseButton QuitButton;
        private BaseButton OptionsButton;
        private BaseButton SpawnEntitiesButton;
        private BaseButton SpawnTilesButton;
        private OptionsMenu optionsMenu;

        protected override void Initialize()
        {
            base.Initialize();

            optionsMenu = new OptionsMenu
            {
                Visible = false
            };
            optionsMenu.AddToScreen();

            Resizable = false;
            HideOnClose = true;

            IoCManager.InjectDependencies(this);

            QuitButton = Contents.GetChild<BaseButton>("QuitButton");
            QuitButton.OnPressed += OnQuitButtonClicked;

            OptionsButton = Contents.GetChild<BaseButton>("OptionsButton");
            OptionsButton.OnPressed += OnOptionsButtonClicked;

            SpawnEntitiesButton = Contents.GetChild<BaseButton>("SpawnEntitiesButton");
            SpawnEntitiesButton.OnPressed += OnSpawnEntitiesButtonClicked;

            SpawnTilesButton = Contents.GetChild<BaseButton>("SpawnTilesButton");
            SpawnTilesButton.OnPressed += OnSpawnTilesButtonClicked;
        }

        private void OnQuitButtonClicked(BaseButton.ButtonEventArgs args)
        {
            netManager.ClientDisconnect("Client disconnected from game.");
            stateManager.RequestStateChange<MainScreen>();
            Dispose();
        }

        private void OnOptionsButtonClicked(BaseButton.ButtonEventArgs args)
        {
            optionsMenu.OpenCentered();
        }

        private void OnSpawnEntitiesButtonClicked(BaseButton.ButtonEventArgs args)
        {
            var window = new EntitySpawnWindow();
            window.AddToScreen();
            window.OpenToLeft();
        }

        private void OnSpawnTilesButtonClicked(BaseButton.ButtonEventArgs args)
        {

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                optionsMenu.Dispose();
            }
        }
    }
}
