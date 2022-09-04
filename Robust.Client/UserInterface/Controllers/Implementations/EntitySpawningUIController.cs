using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.GameObjects;
using Robust.Client.Placement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Robust.Client.UserInterface.Controllers.Implementations;

public sealed class EntitySpawningUIController : UIController
{
    [Dependency] private readonly IPlacementManager _placement = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IResourceCache _resources = default!;

    private EntitySpawnWindow? _window;
    private readonly List<EntityPrototype> _shownEntities = new();
    private bool _init;

    public override void Initialize()
    {
        DebugTools.Assert(_init == false);
        _init = true;

        _placement.DirectionChanged += OnDirectionChanged;
        _placement.PlacementChanged += ClearSelection;
    }

    // The indices of the visible prototypes last time UpdateVisiblePrototypes was ran.
    // This is inclusive, so end is the index of the last prototype, not right after it.
    private (int start, int end) _lastEntityIndices;

    private void OnEntityEraseToggled(ButtonToggledEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _placement.Clear();
        // Only toggle the eraser back if the button is pressed.
        if(args.Pressed)
            _placement.ToggleEraser();
        // clearing will toggle the erase button off...
        args.Button.Pressed = args.Pressed;
        _window.OverrideMenu.Disabled = args.Pressed;
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
            _window.SearchBar.GrabKeyboardFocus();
        }
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<EntitySpawnWindow>();
        LayoutContainer.SetAnchorPreset(_window,LayoutContainer.LayoutPreset.CenterLeft);
        _window.OnClose += WindowClosed;
        _window.EraseButton.Pressed = _placement.Eraser;
        _window.EraseButton.OnToggled += OnEntityEraseToggled;
        _window.OverrideMenu.OnItemSelected += OnEntityOverrideSelected;
        _window.SearchBar.OnTextChanged += OnEntitySearchChanged;
        _window.ClearButton.OnPressed += OnEntityClearPressed;
        _window.PrototypeScrollContainer.OnScrolled += UpdateVisiblePrototypes;
        _window.OnResized += UpdateVisiblePrototypes;
        BuildEntityList();
    }

    public void CloseWindow()
    {
        if (_window == null || _window.Disposed)
            return;

        _window?.Close();
    }

    private void WindowClosed()
    {
        if (_window == null || _window.Disposed)
            return;

        if (_window.SelectedButton != null)
        {
            _window.SelectedButton.ActualButton.Pressed = false;
            _window.SelectedButton = null;
        }

        _placement.Clear();
    }

    private void ClearSelection(object? sender, EventArgs e)
    {
        if (_window == null || _window.Disposed)
            return;

        if (_window.SelectedButton != null)
        {
            _window.SelectedButton.ActualButton.Pressed = false;
            _window.SelectedButton = null;
        }

        _window.EraseButton.Pressed = false;
        _window.OverrideMenu.Disabled = false;
    }

    private void OnEntityOverrideSelected(OptionButton.ItemSelectedEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _window.OverrideMenu.SelectId(args.Id);

        if (_placement.CurrentMode != null)
        {
            var newObjInfo = new PlacementInformation
            {
                PlacementOption = EntitySpawnWindow.InitOpts[args.Id],
                EntityType = _placement.CurrentPermission!.EntityType,
                Range = 2,
                IsTile = _placement.CurrentPermission.IsTile
            };

            _placement.Clear();
            _placement.BeginPlacing(newObjInfo);
        }
    }

    private void OnEntitySearchChanged(LineEditEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _placement.Clear();
        BuildEntityList(args.Text);
        _window.ClearButton.Disabled = string.IsNullOrEmpty(args.Text);
    }

    private void OnEntityClearPressed(ButtonEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _placement.Clear();
        _window.SearchBar.Clear();
        BuildEntityList("");
    }

    private void BuildEntityList(string? searchStr = null)
    {
        if (_window == null || _window.Disposed)
            return;

        _shownEntities.Clear();
        _window.PrototypeList.RemoveAllChildren();
        // Reset last prototype indices so it automatically updates the entire list.
        _lastEntityIndices = (0, -1);
        _window.PrototypeList.RemoveAllChildren();
        _window.SelectedButton = null;
        searchStr = searchStr?.ToLowerInvariant();

        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract)
            {
                continue;
            }

            if (prototype.NoSpawn)
            {
                continue;
            }

            if (searchStr != null && !DoesEntityMatchSearch(prototype, searchStr))
            {
                continue;
            }

            _shownEntities.Add(prototype);
        }

        _shownEntities.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        _window.PrototypeList.TotalItemCount = _shownEntities.Count;
        UpdateVisiblePrototypes();
    }

    private static bool DoesEntityMatchSearch(EntityPrototype prototype, string searchStr)
    {
        if (string.IsNullOrEmpty(searchStr))
            return true;

        if (prototype.ID.Contains(searchStr, StringComparison.InvariantCultureIgnoreCase))
            return true;

        if (prototype.EditorSuffix != null &&
            prototype.EditorSuffix.Contains(searchStr, StringComparison.InvariantCultureIgnoreCase))
            return true;

        if (string.IsNullOrEmpty(prototype.Name))
            return false;

        if (prototype.Name.Contains(searchStr, StringComparison.InvariantCultureIgnoreCase))
            return true;

        return false;
    }

    private void UpdateEntityDirectionLabel()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.RotationLabel.Text = _placement.Direction.ToString();
    }

    private void OnDirectionChanged(object? sender, EventArgs e)
    {
        UpdateEntityDirectionLabel();
    }

    // Update visible buttons in the prototype list.
    private void UpdateVisiblePrototypes()
    {
        if (_window == null || _window.Disposed)
            return;

        // Calculate index of first prototype to render based on current scroll.
        var height = _window.MeasureButton.DesiredSize.Y + PrototypeListContainer.Separation;
        var offset = Math.Max(-_window.PrototypeList.Position.Y, 0);
        var startIndex = (int) Math.Floor(offset / height);
        _window.PrototypeList.ItemOffset = startIndex;

        var (prevStart, prevEnd) = _lastEntityIndices;

        // Calculate index of final one.
        var endIndex = startIndex - 1;
        var spaceUsed = -height; // -height instead of 0 because else it cuts off the last button.

        while (spaceUsed < _window.PrototypeList.Parent!.Height)
        {
            spaceUsed += height;
            endIndex += 1;
        }

        endIndex = Math.Min(endIndex, _shownEntities.Count - 1);

        if (endIndex == prevEnd && startIndex == prevStart)
        {
            // Nothing changed so bye.
            return;
        }

        _lastEntityIndices = (startIndex, endIndex);

        // Delete buttons at the start of the list that are no longer visible (scrolling down).
        for (var i = prevStart; i < startIndex && i <= prevEnd; i++)
        {
            var control = (EntitySpawnButton) _window.PrototypeList.GetChild(0);
            DebugTools.Assert(control.Index == i);
            _window.PrototypeList.RemoveChild(control);
        }

        // Delete buttons at the end of the list that are no longer visible (scrolling up).
        for (var i = prevEnd; i > endIndex && i >= prevStart; i--)
        {
            var control = (EntitySpawnButton) _window.PrototypeList.GetChild(_window.PrototypeList.ChildCount - 1);
            DebugTools.Assert(control.Index == i);
            _window.PrototypeList.RemoveChild(control);
        }

        // Create buttons at the start of the list that are now visible (scrolling up).
        for (var i = Math.Min(prevStart - 1, endIndex); i >= startIndex; i--)
        {
            InsertEntityButton(_shownEntities[i], true, i);
        }

        // Create buttons at the end of the list that are now visible (scrolling down).
        for (var i = Math.Max(prevEnd + 1, startIndex); i <= endIndex; i++)
        {
            InsertEntityButton(_shownEntities[i], false, i);
        }
    }

    private void InsertEntityButton(EntityPrototype prototype, bool insertFirst, int index)
    {
        if (_window == null || _window.Disposed)
            return;

        var textures = SpriteComponent.GetPrototypeTextures(prototype, _resources).Select(o => o.Default).ToList();
        var button = _window.InsertEntityButton(prototype, insertFirst, index, textures);

        button.ActualButton.OnToggled += OnEntityButtonToggled;
    }

    private void OnEntityButtonToggled(ButtonToggledEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        var item = (EntitySpawnButton) args.Button.Parent!;
        if (_window.SelectedButton == item)
        {
            _window.SelectedButton = null;
            _window.SelectedPrototype = null;
            _placement.Clear();
            return;
        }

        if (_window.SelectedButton != null)
        {
            _window.SelectedButton.ActualButton.Pressed = false;
        }

        _window.SelectedButton = null;
        _window.SelectedPrototype = null;

        var overrideMode = EntitySpawnWindow.InitOpts[_window.OverrideMenu.SelectedId];
        var newObjInfo = new PlacementInformation
        {
            PlacementOption = overrideMode != "Default" ? overrideMode : item.Prototype.PlacementMode,
            EntityType = item.PrototypeID,
            Range = 2,
            IsTile = false
        };

        _placement.BeginPlacing(newObjInfo);

        _window.SelectedButton = item;
        _window.SelectedPrototype = item.Prototype;
    }
}
