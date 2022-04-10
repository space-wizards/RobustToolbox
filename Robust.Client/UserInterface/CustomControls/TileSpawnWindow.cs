using Robust.Client.UserInterface.Controls;
using Robust.Shared.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    public sealed class TileSpawnWindow : DefaultWindow
    {
        private readonly ITileDefinitionManager _tileDefinitionManager;
        private readonly IPlacementManager _placementManager;
        private readonly IResourceCache _resourceCache;

        private readonly ItemList _tileList;
        private readonly LineEdit _searchBar;
        private readonly Button _clearButton;
        private readonly Label _flag;

        private readonly List<ITileDefinition> _shownItems = new();

        private bool _clearingSelections;

        public TileSpawnWindow(ITileDefinitionManager tileDefinitionManager, IPlacementManager placementManager,
            IResourceCache resourceCache)
        {
            _tileDefinitionManager = tileDefinitionManager;
            _placementManager = placementManager;
            _resourceCache = resourceCache;

            var vBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            Contents.AddChild(vBox);
            var hBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal
            };
            vBox.AddChild(hBox);
            _searchBar = new LineEdit {PlaceHolder = "Search", HorizontalExpand = true};
            _searchBar.OnTextChanged += OnSearchBarTextChanged;
            hBox.AddChild(_searchBar);

            _clearButton = new Button {Text = "Clear"};
            _clearButton.OnPressed += OnClearButtonPressed;
            hBox.AddChild(_clearButton);

            _tileList = new ItemList {VerticalExpand = true};
            _tileList.OnItemSelected += TileListOnOnItemSelected;
            _tileList.OnItemDeselected += TileListOnOnItemDeselected;
            vBox.AddChild(_tileList);

            BuildTileList();

            _flag = new Label()
            {
                Text = "None",
            };

            vBox.AddChild(_flag);

            _placementManager.PlacementChanged += OnPlacementCanceled;

            OnClose += OnWindowClosed;

            Title = "Place Tiles";
            _searchBar.GrabKeyboardFocus();

            SetSize = (300, 300);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _placementManager.PlacementChanged -= OnPlacementCanceled;
            }
        }

        public void SetFlagLabel(TileFlag flag)
        {
            _flag.Text = flag.ToString();
        }

        private void OnClearButtonPressed(BaseButton.ButtonEventArgs args)
        {
            _tileList.ClearSelected();
            _placementManager.Clear();
            _searchBar.Clear();
            BuildTileList("");
            _clearButton.Disabled = true;
        }

        private void OnSearchBarTextChanged(LineEdit.LineEditEventArgs args)
        {
            _tileList.ClearSelected();
            _placementManager.Clear();
            BuildTileList(args.Text);
            _clearButton.Disabled = string.IsNullOrEmpty(args.Text);
        }

        private void BuildTileList(string? searchStr = null)
        {
            _tileList.Clear();

            IEnumerable<ITileDefinition> tileDefs = _tileDefinitionManager;

            if (!string.IsNullOrEmpty(searchStr))
            {
                tileDefs = tileDefs.Where(s =>
                    s.Name.IndexOf(searchStr, StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                    s.ID.IndexOf(searchStr, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            tileDefs = tileDefs.OrderBy(d => d.Name);

            _shownItems.Clear();
            _shownItems.AddRange(tileDefs);

            foreach (var entry in _shownItems)
            {
                Texture? texture = null;
                if (!string.IsNullOrEmpty(entry.SpriteName))
                {
                    texture = _resourceCache.GetResource<TextureResource>(new ResourcePath(entry.Path) / $"{entry.SpriteName}.png");
                }
                _tileList.AddItem(entry.Name, texture);
            }
        }

        private void OnWindowClosed()
        {
            _tileList.ClearSelected();
            _placementManager.Clear();
        }

        private void OnPlacementCanceled(object? sender, EventArgs e)
        {
            _clearingSelections = true;
            _tileList.ClearSelected();
            _clearingSelections = false;
        }
        private void TileListOnOnItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            var definition = _shownItems[args.ItemIndex];

            var newObjInfo = new PlacementInformation
            {
                PlacementOption = "AlignTileAny",
                TileType = definition.TileId,
                Range = 400,
                IsTile = true,
                TileFlags = TileFlag.None,
            };

            _placementManager.BeginPlacing(newObjInfo);
        }

        private void TileListOnOnItemDeselected(ItemList.ItemListDeselectedEventArgs args)
        {
            if (_clearingSelections)
            {
                return;
            }

            _placementManager.Clear();
        }
    }
}
