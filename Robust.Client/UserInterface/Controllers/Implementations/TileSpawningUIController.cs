using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Robust.Client.UserInterface.Controllers.Implementations;

// TODO hud refactor BEFORE MERGE fix incorrect ui
public sealed class TileSpawningUIController : UIController
{
    [Dependency] private readonly IPlacementManager _placement = default!;
    [Dependency] private readonly IResourceCache _resources = default!;
    [Dependency] private readonly ITileDefinitionManager _tiles = default!;

    private TileSpawnWindow? _window;

    private readonly List<ITileDefinition> _shownTiles = new();
    private bool _clearingTileSelections;

    public void ToggleWindow()
    {
        if (_window == null)
        {
            _window = new TileSpawnWindow();
            _window.OpenToLeft();
        }
        else if (_window.IsOpen)
        {
            CloseWindow();
            return;
        }

        _window.OnClose += WindowClosed;

        _window.SearchBar.GrabKeyboardFocus();

        _window.ClearButton.OnPressed += OnTileClearPressed;
        _window.SearchBar.OnTextChanged += OnTileSearchChanged;
        _window.TileList.OnItemSelected += OnTileItemSelected;
        _window.TileList.OnItemDeselected += OnTileItemDeselected;

        _placement.PlacementChanged += ClearTileSelection;

        BuildTileList();
    }

    public void CloseWindow()
    {
        _window?.Close();
    }

    private void WindowClosed()
    {
        if (_window == null)
            return;

        _window.TileList.ClearSelected();
        _placement.Clear();

        _window.OnClose -= WindowClosed;
        _window.ClearButton.OnPressed -= OnTileClearPressed;
        _window.SearchBar.OnTextChanged -= OnTileSearchChanged;
        _window.TileList.OnItemSelected -= OnTileItemSelected;

        _placement.PlacementChanged -= ClearTileSelection;

        _window = null;
    }

    private void ClearTileSelection(object? sender, EventArgs e)
    {
        _clearingTileSelections = true;
        _window?.TileList.ClearSelected();
        _clearingTileSelections = false;
    }

    private void OnTileClearPressed(ButtonEventArgs args)
    {
        if (_window == null)
            return;

        _window.TileList.ClearSelected();
        _placement.Clear();
        _window.SearchBar.Clear();
        BuildTileList(string.Empty);
        _window.ClearButton.Disabled = true;
    }

    private void OnTileSearchChanged(LineEdit.LineEditEventArgs args)
    {
        if (_window == null)
            return;

        _window.TileList.ClearSelected();
        _placement.Clear();
        BuildTileList(args.Text);
        _window.ClearButton.Disabled = string.IsNullOrEmpty(args.Text);
    }

    private void OnTileItemSelected(ItemList.ItemListSelectedEventArgs args)
    {
        var definition = _shownTiles[args.ItemIndex];

        var newObjInfo = new PlacementInformation
        {
            PlacementOption = "AlignTileAny",
            TileType = definition.TileId,
            Range = 400,
            IsTile = true
        };

        _placement.BeginPlacing(newObjInfo);
    }

    private void OnTileItemDeselected(ItemList.ItemListDeselectedEventArgs args)
    {
        if (_clearingTileSelections)
        {
            return;
        }

        _placement.Clear();
    }

    private void BuildTileList(string? searchStr = null)
    {
        if (_window == null)
        {
            return;
        }

        _window.TileList.Clear();

        IEnumerable<ITileDefinition> tileDefs = _tiles;

        if (!string.IsNullOrEmpty(searchStr))
        {
            tileDefs = tileDefs.Where(s =>
                s.Name.Contains(searchStr, StringComparison.InvariantCultureIgnoreCase) ||
                s.ID.Contains(searchStr, StringComparison.OrdinalIgnoreCase));
        }

        tileDefs = tileDefs.OrderBy(d => d.Name);

        _shownTiles.Clear();
        _shownTiles.AddRange(tileDefs);

        foreach (var entry in _shownTiles)
        {
            Texture? texture = null;
            if (!string.IsNullOrEmpty(entry.SpriteName))
            {
                texture = _resources.GetResource<TextureResource>(new ResourcePath(entry.Path) / $"{entry.SpriteName}.png");
            }

            _window.TileList.AddItem(entry.Name, texture);
        }
    }
}
