using System;
using System.Collections.Generic;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    public sealed class EntitySpawnWindow : SS14Window
    {
        private readonly IPlacementManager placementManager;
        private readonly IPrototypeManager prototypeManager;
        private readonly IResourceCache resourceCache;

        private Control HSplitContainer;
        private VBoxContainer PrototypeList;
        private LineEdit SearchBar;
        private OptionButton OverrideMenu;
        private Button ClearButton;
        private Button EraseButton;
        protected override Vector2 ContentsMinimumSize => HSplitContainer?.CombinedMinimumSize ?? Vector2.Zero;

        private static readonly string[] initOpts = new string[]
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
        };

        private const int TARGET_ICON_HEIGHT = 32;

        private EntitySpawnButton SelectedButton;

        public EntitySpawnWindow(IPlacementManager placementManager,
            IPrototypeManager prototypeManager,
            IResourceCache resourceCache)
        {
            this.placementManager = placementManager;
            this.prototypeManager = prototypeManager;
            this.resourceCache = resourceCache;

            PerformLayout();
        }

        protected override void Initialize()
        {
            base.Initialize();

            Title = "Entity Spawn Panel";

            HSplitContainer = new HSplitContainer
            {
                Name = "HSplitContainer",
                MouseFilter = MouseFilterMode.Pass
            };

            // Left side
            var prototypeListScroll = new ScrollContainer("PrototypeListScrollContainer")
            {
                CustomMinimumSize = new Vector2(200.0f, 0.0f),
                RectClipContent = true,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                HScrollEnabled = true,
                VScrollEnabled = true
            };
            PrototypeList = new VBoxContainer("PrototypeList")
            {
                MouseFilter = MouseFilterMode.Ignore,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                SeparationOverride = new int?(2),
                Align = BoxContainer.AlignMode.Begin
            };
            prototypeListScroll.AddChild(PrototypeList);
            HSplitContainer.AddChild(prototypeListScroll);

            // Right side
            var options = new VBoxContainer("Options")
            {
                CustomMinimumSize = new Vector2(200.0f, 0.0f), MouseFilter = MouseFilterMode.Ignore
            };

            SearchBar = new LineEdit("SearchBar") {MouseFilter = MouseFilterMode.Stop, PlaceHolder = "Search Entities"};
            SearchBar.OnTextChanged += OnSearchBarTextChanged;
            options.AddChild(SearchBar);

            var buttons = new HBoxContainer("Buttons!")
            {
                MouseFilter = MouseFilterMode.Ignore
            };
            ClearButton = new Button("ClearButton")
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                Disabled = true,
                ToggleMode = false,
                Text = "Clear Search",
            };
            ClearButton.OnPressed += OnClearButtonPressed;
            EraseButton = new Button("EraseButton")
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand,
                ToggleMode = true,
                Text = "Erase Mode"
            };
            EraseButton.OnToggled += OnEraseButtonToggled;
            buttons.AddChild(ClearButton);
            buttons.AddChild(EraseButton);
            options.AddChild(buttons);

            var overridePlacementText = new Label("OverridePlacementText")
            {
                MouseFilter = MouseFilterMode.Ignore,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                Text = "Override Placement:"
            };
            OverrideMenu = new OptionButton("OverrideMenu") {ToggleMode = false};//, TextAlign = Button.AlignMode.Left};
            OverrideMenu.OnItemSelected += OnOverrideMenuItemSelected;
            for (var i = 0; i < initOpts.Length; i++)
            {
                OverrideMenu.AddItem(initOpts[i], i);
            }

            options.AddChild(overridePlacementText);
            options.AddChild(OverrideMenu);
            HSplitContainer.AddChild(options);
            Contents.AddChild(HSplitContainer);

            Size = new Vector2(400.0f, 300.0f);
        }

        private void PerformLayout()
        {
            BuildEntityList();

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
                    EntityType = placementManager.CurrentPermission.EntityType,
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
        }

        private void OnEraseButtonToggled(BaseButton.ButtonToggledEventArgs args)
        {
            placementManager.ToggleEraser();
        }

        private void BuildEntityList(string searchStr = null)
        {
            PrototypeList.DisposeAllChildren();
            SelectedButton = null;
            searchStr = searchStr?.ToLowerInvariant();

            var prototypes = new List<EntityPrototype>();
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

                prototypes.Add(prototype);
            }

            prototypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            foreach (var prototype in prototypes)
            {
                var button = new EntitySpawnButton()
                {
                    Prototype = prototype,
                };
                button.ActualButton.OnToggled += OnItemButtonToggled;
                button.EntityLabel.Text = prototype.Name;

                var tex = IconComponent.GetPrototypeIcon(prototype, resourceCache);
                var rect = button.EntityTextureRect;
                if (tex != null)
                {
                    rect.Texture = tex.Default;
                }
                else
                {
                    rect.Dispose();
                }

                PrototypeList.AddChild(button);
            }
        }

        private static bool _doesPrototypeMatchSearch(EntityPrototype prototype, string searchStr)
        {
            if (prototype.ID.ToLowerInvariant().Contains(searchStr))
            {
                return true;
            }

            if (prototype.Name.ToLowerInvariant().Contains(searchStr))
            {
                return true;
            }

            return false;
        }

        private void OnItemButtonToggled(BaseButton.ButtonToggledEventArgs args)
        {
            var item = (EntitySpawnButton) args.Button.Parent;
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
        }

        private class EntitySpawnButton : PanelContainer
        {
            public string PrototypeID => Prototype.ID;
            public EntityPrototype Prototype { get; set; }
            public Button ActualButton { get; private set; }
            public Label EntityLabel { get; private set; }
            public TextureRect EntityTextureRect { get; private set; }

            protected override void Initialize()
            {
                base.Initialize();

                ActualButton = new Button("Button")
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    ToggleMode = true,
                };
                AddChild(ActualButton);

                var hBoxContainer = new HBoxContainer("HBoxContainer")
                {
                    MouseFilter = MouseFilterMode.Ignore,
                };
                var textureWrap = new Control("TextureWrap")
                {
                    CustomMinimumSize = new Vector2(32.0f, 32.0f),
                    MouseFilter = MouseFilterMode.Ignore,
                    RectClipContent = true
                };
                EntityTextureRect = new TextureRect("TextureRect")
                {
                    AnchorRight = 1.0f,
                    AnchorBottom = 1.0f,
                    MouseFilter = MouseFilterMode.Ignore,
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter
                };
                textureWrap.AddChild(EntityTextureRect);

                EntityLabel = new Label("Label")
                {
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    Text = "Backpack"
                };

                hBoxContainer.AddChild(textureWrap);
                hBoxContainer.AddChild(EntityLabel);
                AddChild(hBoxContainer);
            }
        }

        private void OnPlacementCanceled(object sender, EventArgs e)
        {
            if (SelectedButton != null)
            {
                SelectedButton.ActualButton.Pressed = false;
                SelectedButton = null;
            }

            EraseButton.Pressed = false;
        }
    }
}
