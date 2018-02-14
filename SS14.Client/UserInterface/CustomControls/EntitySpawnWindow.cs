using System;
using System.Linq;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;
using SS14.Shared.Reflection;

namespace SS14.Client.UserInterface.CustomControls
{
    [Reflect(false)]
    class EntitySpawnWindow : SS14Window
    {
        [Dependency]
        private readonly IPlacementManager placementManager;
        [Dependency]
        private readonly IPrototypeManager prototypeManager;
        [Dependency]
        private readonly IResourceCache resourceCache;

        private Control HSplitContainer;
        private Control PrototypeList;
        private LineEdit SearchBar;
        private OptionButton OverrideMenu;
        private Button ClearButton;
        private Button EraseButton;
        protected override Vector2 ContentsMinimumSize => HSplitContainer.MinimumSize;

        protected override Godot.Control SpawnSceneControl()
        {
            return LoadScene("res://Scenes/Placement/EntitySpawnPanel.tscn");
        }

        private static readonly string[] initOpts = new string[]
        {
            "PlaceFree",
            "PlaceNearby",
            "SnapgridCenter",
            "SnapgridBorder",
            "AlignSimilar",
            "AlignTileAny",
            "AlignTileEmpty",
            "AlignTileNonSolid",
            "AlignTileSolid",
            "AlignWall",
        };

        private const int TARGET_ICON_HEIGHT = 32;

        private EntitySpawnButton SelectedButton;

        protected override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);

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
            if (searchStr != null)
            {
                searchStr = searchStr.ToLower();
            }

            foreach (var prototype in prototypeManager.EnumeratePrototypes<EntityPrototype>())
            {
                if (searchStr != null && !prototype.ID.ToLower().Contains(searchStr))
                {
                    continue;
                }

                var button = new EntitySpawnButton()
                {
                    Prototype = prototype,
                };
                var container = button.GetChild("HBoxContainer");
                button.ActualButton.OnToggled += OnItemButtonToggled;
                container.GetChild<Label>("Label").Text = prototype.Name;

                var spriteNameParam = prototype.GetBaseSpriteParameters().FirstOrDefault();
                var spriteName = "";
                if (spriteNameParam != null)
                    spriteName = spriteNameParam.GetValue<string>();

                var tex = resourceCache.GetResource<TextureResource>("Textures/" + spriteName);
                var rect = container.GetChild("TextureWrap").GetChild<TextureRect>("TextureRect");
                if (tex != null)
                {
                    rect.Texture = tex.Texture;
                    // Ok I can't find a way to make this TextureRect scale down sanely so let's do this.
                    var scale = (float)TARGET_ICON_HEIGHT / tex.Texture.Height;
                    rect.Scale = new Vector2(scale, scale);
                }
                else
                {
                    rect.Dispose();
                }

                PrototypeList.AddChild(button);
            }
        }

        private void OnItemButtonToggled(BaseButton.ButtonToggledEventArgs args)
        {
            var item = (EntitySpawnButton)args.Button.Parent;
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
                PlacementOption = overrideMode.Length > 0 ? overrideMode : item.Prototype.PlacementMode,
                EntityType = item.PrototypeID,
                Range = 2,
                IsTile = false
            };

            placementManager.BeginPlacing(newObjInfo);

            SelectedButton = item;
        }

        [Reflect(false)]
        private class EntitySpawnButton : Control
        {
            public string PrototypeID => Prototype.ID;
            public EntityPrototype Prototype { get; set; }
            public Button ActualButton { get; private set; }

            protected override Godot.Control SpawnSceneControl()
            {
                return LoadScene("res://Scenes/Placement/EntitySpawnItem.tscn");
            }

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
