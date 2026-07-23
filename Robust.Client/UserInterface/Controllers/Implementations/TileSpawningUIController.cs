using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Client.Placement.Modes;
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

public sealed partial class TileSpawningUIController : UIController
{
    [Dependency] private IPlacementManager _placement = default!;
    [Dependency] private IResourceCache _resources = default!;
    [Dependency] private ITileDefinitionManager _tiles = default!;

    private TileSpawnWindow? _window;
    private bool _init;

    private readonly List<ITileDefinition> _shownTiles = [];

    private int? _currentTileType;

    /// <summary>
    /// Indicates whether _placement was modified in this UI
    /// When true, PlacementChanged event should not modify any UI
    /// And any select/deselect events should not modify placement
    /// </summary>
    private bool _placementLock;

    private bool _mirrorableTile; // Tracks if the chosen tile even can be mirrored.
    private bool _mirroredTile;

    public override void Initialize()
    {
        DebugTools.Assert(_init == false);
        _init = true;
        _placement.PlacementChanged += OnPlacementChanged;
        _placement.DirectionChanged += OnDirectionChanged;
        _placement.MirroredChanged += OnMirroredChanged;
    }

    private void StartTilePlacement(int tileType)
    {
        if (_window == null || _window.Disposed)
            return;

        var newObjInfo = new PlacementInformation
        {
            PlacementOption = nameof(AlignTileAny),
            TileType = tileType,
            Range = 400,
            IsTile = true
        };

        _currentTileType = tileType;
        _placementLock = true;
        _placement.BeginPlacing(newObjInfo);
        if (tileType == 0)
        {
            _window.TileList.ClearSelected();
        }
        else
        {
            _window.EraseButton.Pressed = false;
        }
        _placementLock = false;
    }

    private void StopTilePlacement()
    {
        if (_currentTileType == null) return;
        _currentTileType = null;
        _placement.Clear();
    }

    private void OnTileEraseToggled(ButtonToggledEventArgs args)
    {
        if (args.Pressed)
        {
            StartTilePlacement(0);
        }
        else
        {
            StopTilePlacement();
        }
    }

    private void OnTileMirroredToggled(ButtonToggledEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _placement.Mirrored = args.Pressed;
        _mirroredTile = _placement.Mirrored;

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
            UpdateEntityDirectionLabel();
            UpdateMirroredButton();
            _window.SearchBar.GrabKeyboardFocus();
        }
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;
        _window = UIManager.CreateWindow<TileSpawnWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterLeft);
        _window.OnClose += WindowClosed;
        _window.ClearButton.OnPressed += OnTileClearPressed;
        _window.SearchBar.OnTextChanged += OnTileSearchChanged;
        _window.TileList.OnItemSelected += OnTileItemSelected;
        _window.TileList.OnItemDeselected += OnTileItemDeselected;
        _window.EraseButton.Pressed = _currentTileType == 0;
        _window.EraseButton.OnToggled += OnTileEraseToggled;
        _window.MirroredButton.Disabled = !_mirrorableTile;
        _window.RotationLabel.FontColorOverride = _mirrorableTile ? Color.White : Color.Gray;
        _window.MirroredButton.Pressed = _mirroredTile;
        _window.MirroredButton.OnToggled += OnTileMirroredToggled;
        BuildTileList();
    }

    public void CloseWindow()
    {
        if (_window == null || _window.Disposed) return;

        _window?.Close();
    }

    private void WindowClosed()
    {
        if (_window == null || _window.Disposed)
            return;

        StopTilePlacement();
    }

    private void OnPlacementChanged(object? sender, EventArgs e)
    {
        if (_window == null || _window.Disposed) return;
        if (_placementLock) return;
        _currentTileType = null;
        _placementLock = true;
        _window.TileList.ClearSelected();
        _placementLock = false;
        _window.EraseButton.Pressed = false;
        _window.MirroredButton.Pressed = _placement.Mirrored;
    }

    private void OnTileClearPressed(ButtonEventArgs args)
    {
        if (_window == null || _window.Disposed) return;
        StopTilePlacement();
        _window.SearchBar.Clear();
        BuildTileList(string.Empty);
    }

    private void OnTileSearchChanged(LineEdit.LineEditEventArgs args)
    {
        BuildTileList(args.Text);
    }

    private void OnTileItemSelected(ItemList.ItemListSelectedEventArgs args)
    {
        if (_placementLock) return;
        var definition = _shownTiles[args.ItemIndex];
        StartTilePlacement(definition.TileId);
        UpdateMirroredButton();
    }

    private void OnTileItemDeselected(ItemList.ItemListDeselectedEventArgs args)
    {
        if (_placementLock) return;

        /* Need to turn on placement lock here to avoid running OnPlacementChanged
           Because if ClearSelected is called when the user is selecting another item,
           the selection will not be visible
        */
        _placementLock = true;
        StopTilePlacement();
        _placementLock = false;
    }

    private void OnDirectionChanged(object? sender, EventArgs e)
    {
        UpdateEntityDirectionLabel();
    }

    private void UpdateEntityDirectionLabel()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.RotationLabel.Text = _placement.Direction.ToString();
    }

    private void OnMirroredChanged(object? sender, EventArgs e)
    {
        UpdateMirroredButton();
    }

    private void UpdateMirroredButton()
    {
        if (_window == null || _window.Disposed)
            return;

        if (_placement.CurrentPermission != null && _placement.CurrentPermission.IsTile)
        {
            var allowed = _tiles[_placement.CurrentPermission.TileType].AllowRotationMirror;
            _mirrorableTile = allowed;
            _window.MirroredButton.Disabled = !_mirrorableTile;
            _window.RotationLabel.FontColorOverride = _mirrorableTile ? Color.White : Color.Gray;
        }

        _mirroredTile = _placement.Mirrored;
        _window.MirroredButton.Pressed = _mirroredTile;
    }

    private void BuildTileList(string? searchStr = null)
    {
        if (_window == null || _window.Disposed) return;

        _placementLock = true;

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
            var item = _window.TileList.AddItem(Loc.GetString(entry.Name), texture);
            if (entry.TileId == _currentTileType)
            {
                item.Selected = true;
            }
        }

        _placementLock = false;
    }
}
