using System;
using System.Collections.Generic;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Robust.Client.UserInterface.CustomControls
{
    public sealed class EntitySpawnWindow : SS14Window
    {
        private readonly IPlacementManager placementManager;
        private readonly IPrototypeManager prototypeManager;
        private readonly IResourceCache resourceCache;
        private readonly ILocalizationManager _loc;

        private VBoxContainer MainVBox;
        private VBoxContainer PrototypeList;
        private LineEdit SearchBar;
        private OptionButton OverrideMenu;
        private Button ClearButton;
        private Button EraseButton;
        protected override Vector2 ContentsMinimumSize => MainVBox?.CombinedMinimumSize ?? Vector2.Zero;

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

        private const int TARGET_ICON_HEIGHT = 32;

        private EntitySpawnButton SelectedButton;

        public EntitySpawnWindow(IPlacementManager placementManager,
            IPrototypeManager prototypeManager,
            IResourceCache resourceCache,
            ILocalizationManager loc)
        {
            this.placementManager = placementManager;
            this.prototypeManager = prototypeManager;
            this.resourceCache = resourceCache;

            _loc = loc;

            Size = new Vector2(250.0f, 300.0f);
            Title = _loc.GetString("Entity Spawn Panel");

            Contents.AddChild(MainVBox = new VBoxContainer
            {
                Children =
                {
                    new HBoxContainer
                    {
                        Children =
                        {
                            (SearchBar = new LineEdit
                            {
                                MouseFilter = MouseFilterMode.Stop,
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                PlaceHolder = _loc.GetString("Search")
                            }),

                            (ClearButton = new Button
                            {
                                Disabled = true,
                                Text = _loc.GetString("Clear"),
                            })
                        }
                    },
                    new ScrollContainer
                    {
                        CustomMinimumSize = new Vector2(200.0f, 0.0f),
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        Children =
                        {
                            (PrototypeList = new VBoxContainer
                            {
                                MouseFilter = MouseFilterMode.Ignore,
                                SeparationOverride = 2,
                            })
                        }
                    },
                    new HBoxContainer
                    {
                        Children =
                        {
                            (EraseButton = new Button
                            {
                                ToggleMode = true,
                                Text = _loc.GetString("Erase Mode")
                            }),

                            (OverrideMenu = new OptionButton
                            {
                                ToggleMode = false,
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                ToolTip = _loc.GetString("Override placement")
                            })
                        }
                    }
                }
            });

            for (var i = 0; i < initOpts.Length; i++)
            {
                OverrideMenu.AddItem(initOpts[i], i);
            }

            EraseButton.OnToggled += OnEraseButtonToggled;
            OverrideMenu.OnItemSelected += OnOverrideMenuItemSelected;
            SearchBar.OnTextChanged += OnSearchBarTextChanged;
            ClearButton.OnPressed += OnClearButtonPressed;

            BuildEntityList();

            this.placementManager.PlacementCanceled += OnPlacementCanceled;
        }

        public override void Close()
        {
            base.Close();

            Dispose();
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
            PrototypeList.RemoveAllChildren();
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
                var button = new EntitySpawnButton
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

            public EntitySpawnButton()
            {
                ActualButton = new Button
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    ToggleMode = true,
                };
                AddChild(ActualButton);

                var hBoxContainer = new HBoxContainer
                {
                    MouseFilter = MouseFilterMode.Ignore,
                };
                var textureWrap = new Control
                {
                    CustomMinimumSize = new Vector2(32.0f, 32.0f),
                    MouseFilter = MouseFilterMode.Ignore,
                    RectClipContent = true
                };
                EntityTextureRect = new TextureRect
                {
                    AnchorRight = 1.0f,
                    AnchorBottom = 1.0f,
                    MouseFilter = MouseFilterMode.Ignore,
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter
                };
                textureWrap.AddChild(EntityTextureRect);

                EntityLabel = new Label
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
