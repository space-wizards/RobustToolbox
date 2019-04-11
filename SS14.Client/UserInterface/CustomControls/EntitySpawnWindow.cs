using System;
using System.Collections.Generic;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;

namespace SS14.Client.UserInterface.CustomControls
{
    internal class EntitySpawnWindow : SS14Window
    {
        protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Placement/EntitySpawnPanel.tscn");

        private readonly IPlacementManager placementManager;
        private readonly IPrototypeManager prototypeManager;
        private readonly IResourceCache resourceCache;

        private Control HSplitContainer;
        private Control PrototypeList;
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

        public EntitySpawnWindow(IDisplayManager displayManager, IPlacementManager placementManager,
            IPrototypeManager prototypeManager,
            IResourceCache resourceCache) : base(displayManager)
        {
            this.placementManager = placementManager;
            this.prototypeManager = prototypeManager;
            this.resourceCache = resourceCache;

            PerformLayout();
        }

        protected override void Initialize()
        {
            base.Initialize();
            
            // Get all the controls.
            HSplitContainer = Contents.GetChild("HSplitContainer");
            PrototypeList = HSplitContainer.GetChild("PrototypeListScrollContainer").GetChild("PrototypeList");
            var options = HSplitContainer.GetChild("Options");
            SearchBar = options.GetChild<LineEdit>("SearchBar");
            SearchBar.OnTextChanged += OnSearchBarTextChanged;

            OverrideMenu = options.GetChild<OptionButton>("OverrideMenu");
            OverrideMenu.OnItemSelected += OnOverrideMenuItemSelected;

            for (var i = 0; i < initOpts.Length; i++)
            {
                OverrideMenu.AddItem(initOpts[i], i);
            }

            var buttons = options.GetChild("Buttons!");
            ClearButton = buttons.GetChild<Button>("ClearButton");
            ClearButton.OnPressed += OnClearButtonPressed;

            EraseButton = buttons.GetChild<Button>("EraseButton");
            EraseButton.OnToggled += OnEraseButtonToggled;
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
                var container = button.GetChild("HBoxContainer");
                button.ActualButton.OnToggled += OnItemButtonToggled;
                container.GetChild<Label>("Label").Text = prototype.Name;

                var tex = IconComponent.GetPrototypeIcon(prototype, resourceCache);
                var rect = container.GetChild("TextureWrap").GetChild<TextureRect>("TextureRect");
                if (tex != null)
                {
                    rect.Texture = tex.Default;
                    // Ok I can't find a way to make this TextureRect scale down sanely so let's do this.
                    var scale = (float) TARGET_ICON_HEIGHT / tex.Default.Height;
                    rect.Scale = new Vector2(scale, scale);
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

            protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Placement/EntitySpawnItem.tscn");

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

            EraseButton.Pressed = false;
        }
    }
}
