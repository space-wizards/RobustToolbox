using SS14.Client.Interfaces.Placement;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Reflection;
using System;
using System.Linq;
using SS14.Shared.Utility;

namespace SS14.Client.UserInterface.CustomControls
{
    internal class TileSpawnWindow : SS14Window
    {
        protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Placement/TileSpawnPanel.tscn");

        [Dependency]
        private readonly ITileDefinitionManager tileDefinitionManager;
        [Dependency]
        private readonly IPlacementManager placementManager;

        private Control TileList;
        private LineEdit SearchBar;
        private Button ClearButton;

        private TileSpawnButton SelectedButton;

        protected override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);

            // Get all the controls.
            var HSplitContainer = Contents.GetChild("HSplitContainer");
            TileList = HSplitContainer.GetChild("TileListScrollContainer").GetChild("TileList");
            var options = HSplitContainer.GetChild("Options");
            SearchBar = options.GetChild<LineEdit>("SearchBar");
            SearchBar.OnTextChanged += OnSearchBarTextChanged;

            ClearButton = options.GetChild<Button>("ClearButton");
            ClearButton.OnPressed += OnClearButtonPressed;

            BuildTileList();

            placementManager.PlacementCanceled += OnPlacementCanceled;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                placementManager.PlacementCanceled -= OnPlacementCanceled;
            }
        }

        private void OnClearButtonPressed(BaseButton.ButtonEventArgs args)
        {
            SearchBar.Clear();
        }

        private void OnSearchBarTextChanged(LineEdit.LineEditEventArgs args)
        {
            BuildTileList(args.Text);
            ClearButton.Disabled = string.IsNullOrEmpty(args.Text);
        }

        private void BuildTileList(string searchStr = null)
        {
            TileList.DisposeAllChildren();

            var tileDefs = tileDefinitionManager.Select(td => td.Name);

            if (!string.IsNullOrEmpty(searchStr))
            {
                tileDefs = tileDefs.Where(s => s.IndexOf(searchStr, StringComparison.InvariantCultureIgnoreCase) >= 0);
            }

            foreach (var entry in tileDefs)
            {
                var button = new TileSpawnButton()
                {
                    TileDef = entry,
                };
                button.ActualButton.Text = entry;
                button.ActualButton.OnToggled += OnItemButtonToggled;

                TileList.AddChild(button);
            }
        }

        private class TileSpawnButton : PanelContainer
        {
            public string TileDef { get; set; }
            public Button ActualButton { get; private set; }

            protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Placement/TileSpawnItem.tscn");

            protected override void Initialize()
            {
                base.Initialize();

                ActualButton = GetChild<Button>("Button");
            }
        }

        private void OnPlacementCanceled(object sender, EventArgs e)
        {
            if (SelectedButton != null)
            {
                SelectedButton.ActualButton.Pressed = false;
                SelectedButton = null;
            }
        }

        private void OnItemButtonToggled(BaseButton.ButtonToggledEventArgs args)
        {
            var item = (TileSpawnButton)args.Button.Parent;
            if (SelectedButton == item)
            {
                SelectedButton = null;
                placementManager.Clear();
                return;
            }
            else if (SelectedButton != null)
            {
                SelectedButton.ActualButton.Pressed = false;
            }

            SelectedButton = null;

            var newObjInfo = new PlacementInformation
            {
                PlacementOption = "AlignTileAny",
                TileType = tileDefinitionManager[item.TileDef].TileId,
                Range = 400,
                IsTile = true
            };

            placementManager.BeginPlacing(newObjInfo);
            SelectedButton = item;
        }
    }
}
