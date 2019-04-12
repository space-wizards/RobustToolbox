using Robust.Client.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Interfaces.State;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.State.States;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    public class EscapeMenu : SS14Window
    {
        [Dependency]
        readonly IClientConsole console;

        protected override ResourcePath ScenePath => new ResourcePath("/Scenes/EscapeMenu/EscapeMenu.tscn");
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
            console.ProcessCommand("disconnect");
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
            var window = new TileSpawnWindow();
            window.AddToScreen();
            window.OpenToLeft();
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
