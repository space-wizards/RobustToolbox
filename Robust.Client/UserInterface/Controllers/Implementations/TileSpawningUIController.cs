using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Robust.Client.UserInterface.Controllers.Implementations;

public sealed class TileSpawningUIController : UIController
{
    [Dependency] private readonly IPlacementManager _placement = default!;
    [Dependency] private readonly IResourceCache _resources = default!;
    [Dependency] private readonly ITileDefinitionManager _tiles = default!;

    private TileSpawnWindow? _window;
    private bool _init;

    private readonly List<ITileDefinition> _shownTiles = new();
    private bool _clearingTileSelections;
    private bool _eraseTile;

    public override void Initialize()
    {
        DebugTools.Assert(_init == false);
        _init = true;
        _placement.PlacementChanged += ClearTileSelection;
    }

    private void StartTilePlacement(int tileType)
    {
        var newObjInfo = new PlacementInformation
        {
            PlacementOption = "AlignTileAny",
            TileType = tileType,
            Range = 400,
            IsTile = true
        };

        _placement.BeginPlacing(newObjInfo);
    }

    private void OnTileEraseToggled(ButtonToggledEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _placement.Clear();

        if (args.Pressed)
        {
            _eraseTile = true;
            StartTilePlacement(0);
        }
        else
            _eraseTile = false;

        args.Button.Pressed = args.Pressed;
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_window!.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.Open();
        }
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;
        _window = UIManager.CreateWindow<TileSpawnWindow>();
        LayoutContainer.SetAnchorPreset(_window,LayoutContainer.LayoutPreset.CenterLeft);
        _window.ClearButton.OnPressed += OnTileClearPressed;
        _window.SearchBar.OnTextChanged += OnTileSearchChanged;
        _window.TileList.OnItemSelected += OnTileItemSelected;
        _window.TileList.OnItemDeselected += OnTileItemDeselected;
        _window.EraseButton.Pressed = _eraseTile;
        _window.EraseButton.OnToggled += OnTileEraseToggled;
        BuildTileList();
    }

    public void CloseWindow()
    {
        if (_window == null || _window.Disposed) return;

        _window?.Close();
    }

    private void ClearTileSelection(object? sender, EventArgs e)
    {
        if (_window == null || _window.Disposed) return;
        _clearingTileSelections = true;
        _window.TileList.ClearSelected();
        _clearingTileSelections = false;
        _window.EraseButton.Pressed = false;
    }

    private void OnTileClearPressed(ButtonEventArgs args)
    {
        if (_window == null || _window.Disposed) return;

        _window.TileList.ClearSelected();
        _placement.Clear();
        _window.SearchBar.Clear();
        BuildTileList(string.Empty);
        _window.ClearButton.Disabled = true;
    }

    private void OnTileSearchChanged(LineEdit.LineEditEventArgs args)
    {
        if (_window == null || _window.Disposed) return;

        _window.TileList.ClearSelected();
        _placement.Clear();
        BuildTileList(args.Text);
        _window.ClearButton.Disabled = string.IsNullOrEmpty(args.Text);
    }

    private void OnTileItemSelected(ItemList.ItemListSelectedEventArgs args)
    {
        var definition = _shownTiles[args.ItemIndex];
        StartTilePlacement(definition.TileId);
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
        if (_window == null || _window.Disposed) return;

        _window.TileList.Clear();

        IEnumerable<ITileDefinition> tileDefs = _tiles.Where(def => !def.EditorHidden);

        if (!string.IsNullOrEmpty(searchStr))
        {
            tileDefs = tileDefs.Where(s =>
                Loc.GetString(s.Name).Contains(searchStr, StringComparison.CurrentCultureIgnoreCase) ||
                s.ID.Contains(searchStr, StringComparison.OrdinalIgnoreCase));
        }

        tileDefs = tileDefs.OrderBy(d => Loc.GetString(d.Name));

        _shownTiles.Clear();
        _shownTiles.AddRange(tileDefs);

        foreach (var entry in _shownTiles)
        {
            Texture? texture = null;
            var path = entry.Sprite?.ToString();

            if (path != null)
            {
                texture = _resources.GetResource<TextureResource>(path);
            }
            _window.TileList.AddItem(Loc.GetString(entry.Name), texture);
        }
    }
}
