using Robust.Shared.Enums;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Interfaces.Placement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
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

            IEnumerable<ITileDefinition> tileDefs = tileDefinitionManager;

            if (!string.IsNullOrEmpty(searchStr))
            {
                tileDefs = tileDefs.Where(s => s.DisplayName.IndexOf(searchStr, StringComparison.InvariantCultureIgnoreCase) >= 0);
            }

            foreach (var entry in tileDefs)
            {
                var button = new TileSpawnButton()
                {
                    TileDef = entry,
                };
                button.ActualButton.Text = entry.DisplayName;
                button.ActualButton.OnToggled += OnItemButtonToggled;

                TileList.AddChild(button);
            }
        }

        private class TileSpawnButton : PanelContainer
        {
            public ITileDefinition TileDef { get; set; }
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
                TileType = item.TileDef.TileId,
                Range = 400,
                IsTile = true
            };

            placementManager.BeginPlacing(newObjInfo);
            SelectedButton = item;
        }
    }
}
