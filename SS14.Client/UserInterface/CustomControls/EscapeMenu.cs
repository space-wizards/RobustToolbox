using SS14.Client.UserInterface.Controls;
using SS14.Client.Console;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;

namespace SS14.Client.UserInterface.CustomControls
{
    public class EscapeMenu : SS14Window
    {
        private readonly IClientConsole _console;
        private readonly ITileDefinitionManager __tileDefinitionManager;
        private readonly IPlacementManager _placementManager;
        private readonly IPrototypeManager _prototypeManager;
        private readonly IResourceCache _resourceCache;
        private readonly IDisplayManager _displayManager;
        private readonly IConfigurationManager _configSystem;

        protected override ResourcePath ScenePath => new ResourcePath("/Scenes/EscapeMenu/EscapeMenu.tscn");
        private BaseButton QuitButton;
        private BaseButton OptionsButton;
        private BaseButton SpawnEntitiesButton;
        private BaseButton SpawnTilesButton;
        private OptionsMenu optionsMenu;

        public EscapeMenu(IDisplayManager displayManager,
            IClientConsole console,
            ITileDefinitionManager tileDefinitionManager,
            IPlacementManager placementManager,
            IPrototypeManager prototypeManager,
            IResourceCache resourceCache,
            IConfigurationManager configSystem) : base(displayManager)
        {
            _configSystem = configSystem;
            _displayManager = displayManager;
            _console = console;
            __tileDefinitionManager = tileDefinitionManager;
            _placementManager = placementManager;
            _prototypeManager = prototypeManager;
            _resourceCache = resourceCache;

            PerformLayout();
        }

        private void PerformLayout()
        {
            optionsMenu = new OptionsMenu(_displayManager, _configSystem)
            {
                Visible = false
            };
            optionsMenu.AddToScreen();

            Resizable = false;
            HideOnClose = true;

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
            _console.ProcessCommand("disconnect");
            Dispose();
        }

        private void OnOptionsButtonClicked(BaseButton.ButtonEventArgs args)
        {
            optionsMenu.OpenCentered();
        }

        private void OnSpawnEntitiesButtonClicked(BaseButton.ButtonEventArgs args)
        {
            var window = new EntitySpawnWindow(_displayManager, _placementManager, _prototypeManager, _resourceCache);
            window.AddToScreen();
            window.OpenToLeft();
        }

        private void OnSpawnTilesButtonClicked(BaseButton.ButtonEventArgs args)
        {
            var window = new TileSpawnWindow(__tileDefinitionManager, _placementManager, _displayManager);
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
