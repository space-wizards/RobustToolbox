using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Robust.Client.GameObjects;
using Robust.Client.Placement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Enums;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    public sealed class EntitySpawnWindow : SS14Window
    {
        private readonly IPlacementManager placementManager;
        private readonly IPrototypeManager prototypeManager;
        private readonly IResourceCache resourceCache;

        private VBoxContainer MainVBox;
        private PrototypeListContainer PrototypeList;
        private LineEdit SearchBar;
        private OptionButton OverrideMenu;
        private Button ClearButton;
        private Button EraseButton;

        private EntitySpawnButton MeasureButton;
        //protected override Vector2 ContentsMinimumSize => MainVBox?.CombinedMinimumSize ?? Vector2.Zero;

        // List of prototypes that are visible based on current filter criteria.
        private readonly List<EntityPrototype> _filteredPrototypes = new();

        // The indices of the visible prototypes last time UpdateVisiblePrototypes was ran.
        // This is inclusive, so end is the index of the last prototype, not right after it.
        private (int start, int end) _lastPrototypeIndices;

        private static readonly string[] initOpts =
        {
            "Default",
            "PlaceFree",
            "PlaceNearby",
            "SnapgridCenter",
            "SnapgridBorder",
            "AlignSimilar",
            "AlignTileAny",
            "AlignTileEmpty",
            "AlignTileNonDense",
            "AlignTileDense",
            "AlignWall",
            "AlignWallProper",
        };

        private EntitySpawnButton? SelectedButton;
        private EntityPrototype? SelectedPrototype;

        public EntitySpawnWindow(IPlacementManager placementManager,
            IPrototypeManager prototypeManager,
            IResourceCache resourceCache)
        {
            this.placementManager = placementManager;
            this.prototypeManager = prototypeManager;
            this.resourceCache = resourceCache;

            Title = Loc.GetString("Entity Spawn Panel");

            SetSize = (250, 300);
            MinSize = (250, 200);

            Contents.AddChild(MainVBox = new VBoxContainer
            {
                Name = "AAAAAA",
                Children =
                {
                    new HBoxContainer
                    {
                        Children =
                        {
                            (SearchBar = new LineEdit
                            {
                                HorizontalExpand = true,
                                PlaceHolder = Loc.GetString("Search")
                            }),

                            (ClearButton = new Button
                            {
                                Disabled = true,
                                Text = Loc.GetString("Clear"),
                            })
                        }
                    },
                    new ScrollContainer
                    {
                        MinSize = new Vector2(200.0f, 0.0f),
                        VerticalExpand = true,
                        Children =
                        {
                            (PrototypeList = new PrototypeListContainer())
                        }
                    },
                    new HBoxContainer
                    {
                        Children =
                        {
                            (EraseButton = new Button
                            {
                                ToggleMode = true,
                                Text = Loc.GetString("Erase Mode")
                            }),

                            (OverrideMenu = new OptionButton
                            {
                                HorizontalExpand = true,
                                ToolTip = Loc.GetString("Override placement")
                            })
                        }
                    },
                    new DoNotMeasure
                    {
                        Visible = false,
                        Children =
                        {
                            (MeasureButton = new EntitySpawnButton())
                        }
                    }
                }
            });

            MeasureButton.Measure(Vector2.Infinity);

            for (var i = 0; i < initOpts.Length; i++)
            {
                OverrideMenu.AddItem(initOpts[i], i);
            }

            EraseButton.Pressed = placementManager.Eraser;
            EraseButton.OnToggled += OnEraseButtonToggled;
            OverrideMenu.OnItemSelected += OnOverrideMenuItemSelected;
            SearchBar.OnTextChanged += OnSearchBarTextChanged;
            ClearButton.OnPressed += OnClearButtonPressed;

            BuildEntityList();

            this.placementManager.PlacementChanged += OnPlacementCanceled;
            SearchBar.GrabKeyboardFocus();
        }

        public override void Close()
        {
            base.Close();

            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            if(EraseButton.Pressed)
                placementManager.Clear();

            placementManager.PlacementChanged -= OnPlacementCanceled;
        }

        private void OnSearchBarTextChanged(LineEdit.LineEditEventArgs args)
        {
            BuildEntityList(args.Text);
            ClearButton.Disabled = string.IsNullOrEmpty(args.Text);
        }

        private void OnOverrideMenuItemSelected(OptionButton.ItemSelectedEventArgs args)
        {
            OverrideMenu.SelectId(args.Id);

            if (placementManager.CurrentMode != null)
            {
                var newObjInfo = new PlacementInformation
                {
                    PlacementOption = initOpts[args.Id],
                    EntityType = placementManager.CurrentPermission!.EntityType,
                    Range = 2,
                    IsTile = placementManager.CurrentPermission.IsTile
                };

                placementManager.Clear();
                placementManager.BeginPlacing(newObjInfo);
            }
        }

        private void OnClearButtonPressed(BaseButton.ButtonEventArgs args)
        {
            SearchBar.Clear();
            BuildEntityList("");
        }

        private void OnEraseButtonToggled(BaseButton.ButtonToggledEventArgs args)
        {
            placementManager.ToggleEraser();
            OverrideMenu.Disabled = args.Pressed;
        }

        private void BuildEntityList(string? searchStr = null)
        {
            _filteredPrototypes.Clear();
            PrototypeList.RemoveAllChildren();
            // Reset last prototype indices so it automatically updates the entire list.
            _lastPrototypeIndices = (0, -1);
            PrototypeList.RemoveAllChildren();
            SelectedButton = null;
            searchStr = searchStr?.ToLowerInvariant();

            foreach (var prototype in prototypeManager.EnumeratePrototypes<EntityPrototype>())
            {
                if (prototype.Abstract)
                {
                    continue;
                }

                if (searchStr != null && !_doesPrototypeMatchSearch(prototype, searchStr))
                {
                    continue;
                }

                _filteredPrototypes.Add(prototype);
            }

            _filteredPrototypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            PrototypeList.TotalItemCount = _filteredPrototypes.Count;
        }

        private void UpdateVisiblePrototypes()
        {
            // Update visible buttons in the prototype list.

            // Calculate index of first prototype to render based on current scroll.
            var height = MeasureButton.DesiredSize.Y + PrototypeListContainer.Separation;
            var offset = Math.Max(-PrototypeList.Position.Y, 0);
            var startIndex = (int) Math.Floor(offset / height);
            PrototypeList.ItemOffset = startIndex;

            var (prevStart, prevEnd) = _lastPrototypeIndices;

            // Calculate index of final one.
            var endIndex = startIndex - 1;
            var spaceUsed = -height; // -height instead of 0 because else it cuts off the last button.

            while (spaceUsed < PrototypeList.Parent!.Height)
            {
                spaceUsed += height;
                endIndex += 1;
            }

            endIndex = Math.Min(endIndex, _filteredPrototypes.Count - 1);

            if (endIndex == prevEnd && startIndex == prevStart)
            {
                // Nothing changed so bye.
                return;
            }

            _lastPrototypeIndices = (startIndex, endIndex);

            // Delete buttons at the start of the list that are no longer visible (scrolling down).
            for (var i = prevStart; i < startIndex && i <= prevEnd; i++)
            {
                var control = (EntitySpawnButton) PrototypeList.GetChild(0);
                DebugTools.Assert(control.Index == i);
                PrototypeList.RemoveChild(control);
            }

            // Delete buttons at the end of the list that are no longer visible (scrolling up).
            for (var i = prevEnd; i > endIndex && i >= prevStart; i--)
            {
                var control = (EntitySpawnButton) PrototypeList.GetChild(PrototypeList.ChildCount - 1);
                DebugTools.Assert(control.Index == i);
                PrototypeList.RemoveChild(control);
            }

            // Create buttons at the start of the list that are now visible (scrolling up).
            for (var i = Math.Min(prevStart - 1, endIndex); i >= startIndex; i--)
            {
                InsertEntityButton(_filteredPrototypes[i], true, i);
            }

            // Create buttons at the end of the list that are now visible (scrolling down).
            for (var i = Math.Max(prevEnd + 1, startIndex); i <= endIndex; i++)
            {
                InsertEntityButton(_filteredPrototypes[i], false, i);
            }
        }

        // Create a spawn button and insert it into the start or end of the list.
        private void InsertEntityButton(EntityPrototype prototype, bool insertFirst, int index)
        {
            var button = new EntitySpawnButton
            {
                Prototype = prototype,
                Index = index // We track this index purely for debugging.
            };
            button.ActualButton.OnToggled += OnItemButtonToggled;
            var entityLabelText = string.IsNullOrEmpty(prototype.Name) ? prototype.ID : prototype.Name;

            if (!string.IsNullOrWhiteSpace(prototype.EditorSuffix))
            {
                entityLabelText += $" [{prototype.EditorSuffix}]";
            }

            button.EntityLabel.Text = entityLabelText;

            if (prototype == SelectedPrototype)
            {
                SelectedButton = button;
                SelectedButton.ActualButton.Pressed = true;
            }

            var rect = button.EntityTextureRects;
            rect.Textures = SpriteComponent.GetPrototypeTextures(prototype, resourceCache).Select(o => o.Default).ToList();

            PrototypeList.AddChild(button);
            if (insertFirst)
            {
                button.SetPositionInParent(0);
            }
        }

        private static bool _doesPrototypeMatchSearch(EntityPrototype prototype, string searchStr)
        {
            if (prototype.ID.ToLowerInvariant().Contains(searchStr))
            {
                return true;
            }

            if (prototype.EditorSuffix != null &&
                prototype.EditorSuffix.Contains(searchStr, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            if (string.IsNullOrEmpty(prototype.Name))
            {
                return false;
            }

            if (prototype.Name.ToLowerInvariant().Contains(searchStr))
            {
                return true;
            }

            return false;
        }

        private void OnItemButtonToggled(BaseButton.ButtonToggledEventArgs args)
        {
            var item = (EntitySpawnButton) args.Button.Parent!;
            if (SelectedButton == item)
            {
                SelectedButton = null;
                SelectedPrototype = null;
                placementManager.Clear();
                return;
            }
            else if (SelectedButton != null)
            {
                SelectedButton.ActualButton.Pressed = false;
            }

            SelectedButton = null;
            SelectedPrototype = null;

            var overrideMode = initOpts[OverrideMenu.SelectedId];
            var newObjInfo = new PlacementInformation
            {
                PlacementOption = overrideMode != "Default" ? overrideMode : item.Prototype.PlacementMode,
                EntityType = item.PrototypeID,
                Range = 2,
                IsTile = false
            };

            placementManager.BeginPlacing(newObjInfo);

            SelectedButton = item;
            SelectedPrototype = item.Prototype;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            UpdateVisiblePrototypes();
        }

        private class PrototypeListContainer : Container
        {
            // Quick and dirty container to do virtualization of the list.
            // Basically, get total item count and offset to put the current buttons at.
            // Get a constant minimum height and move the buttons in the list up to match the scrollbar.
            private int _totalItemCount;
            private int _itemOffset;

            public int TotalItemCount
            {
                get => _totalItemCount;
                set
                {
                    _totalItemCount = value;
                    InvalidateMeasure();
                }
            }

            public int ItemOffset
            {
                get => _itemOffset;
                set
                {
                    _itemOffset = value;
                    InvalidateMeasure();
                }
            }

            public const float Separation = 2;

            protected override Vector2 MeasureOverride(Vector2 availableSize)
            {
                if (ChildCount == 0)
                {
                    return Vector2.Zero;
                }

                var first = GetChild(0);

                first.Measure(availableSize);
                var (minX, minY) = first.DesiredSize;

                return (minX, minY * TotalItemCount + (TotalItemCount - 1) * Separation);
            }

            protected override Vector2 ArrangeOverride(Vector2 finalSize)
            {
                if (ChildCount == 0)
                {
                    return Vector2.Zero;
                }

                var first = GetChild(0);

                var height = first.DesiredSize.Y;
                var offset = ItemOffset * height + (ItemOffset - 1) * Separation;

                foreach (var child in Children)
                {
                    child.Arrange(UIBox2.FromDimensions(0, offset, Width, height));
                    offset += Separation + height;
                }

                return finalSize;
            }
        }

        [DebuggerDisplay("spawnbutton {" + nameof(Index) + "}")]
        private class EntitySpawnButton : Control
        {
            public string PrototypeID => Prototype.ID;
            public EntityPrototype Prototype { get; set; } = default!;
            public Button ActualButton { get; private set; }
            public Label EntityLabel { get; private set; }
            public LayeredTextureRect EntityTextureRects { get; private set; }
            public int Index { get; set; }

            public EntitySpawnButton()
            {
                AddChild(ActualButton = new Button
                {
                    ToggleMode = true,
                });

                AddChild(new HBoxContainer
                {
                    Children =
                    {
                        (EntityTextureRects = new LayeredTextureRect
                        {
                            MinSize = (32, 32),
                            HorizontalAlignment = HAlignment.Center,
                            VerticalAlignment = VAlignment.Center,
                            Stretch = TextureRect.StretchMode.KeepAspectCentered,
                            CanShrink = true
                        }),
                        (EntityLabel = new Label
                        {
                            VerticalAlignment = VAlignment.Center,
                            HorizontalExpand = true,
                            Text = "Backpack",
                            ClipText = true
                        })
                    }
                });
            }
        }

        private void OnPlacementCanceled(object? sender, EventArgs e)
        {
            if (SelectedButton != null)
            {
                SelectedButton.ActualButton.Pressed = false;
                SelectedButton = null;
            }

            EraseButton.Pressed = false;
            OverrideMenu.Disabled = false;
        }

        private class DoNotMeasure : Control
        {
            protected override Vector2 MeasureOverride(Vector2 availableSize)
            {
                return Vector2.Zero;
            }
        }
    }
}
