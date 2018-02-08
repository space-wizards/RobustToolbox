/*
using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Placement;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;

namespace SS14.Client.UserInterface.CustomControls
{
    internal class EntitySpawnWindow : Window
    {
        private readonly Label _clearLabel;
        private readonly ScrollableContainer _entityList;
        private readonly ImageButton _eraserButton;
        private readonly Listbox _lstOverride;
        private readonly IPlacementManager _placementManager;

        public EntitySpawnWindow(Vector2i size)
            : base("Entity Spawn Panel", size)
        {
            _placementManager = IoCManager.Resolve<IPlacementManager>();

            _entityList = new ScrollableContainer(new Vector2i(200, 400));
            _entityList.LocalPosition = new Vector2i(5, 5);
            Container.AddControl(_entityList);

            var searchLabel = new Label("Entity Search:", "CALIBRI");
            searchLabel.LocalPosition = new Vector2i(210, 0);
            Container.AddControl(searchLabel);

            var entSearchTextbox = new Textbox(125);
            entSearchTextbox.LocalPosition = new Vector2i(210, 20);
            entSearchTextbox.OnSubmit += entSearchTextbox_OnSubmit;
            Container.AddControl(entSearchTextbox);

            _clearLabel = new Label("[Clear Filter]", "CALIBRI");
            _clearLabel.DrawBackground = true;
            _clearLabel.DrawBorder = true;
            _clearLabel.LocalPosition = new Vector2i(210, 55);
            _clearLabel.Clicked += ClearLabelClicked;
            _clearLabel.BackgroundColor = Color.Gray;
            Container.AddControl(_clearLabel);

            var overLabel = new Label("Override Placement:", "CALIBRI");
            overLabel.LocalPosition = new Vector2i(0, 15);
            overLabel.Alignment = ControlAlignments.Bottom;
            _clearLabel.AddControl(overLabel);

            var initOpts = new List<string>();
            initOpts.AddRange(new[]
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
            });

            _lstOverride = new Listbox(140, 125, initOpts);
            _lstOverride.ItemSelected += _lstOverride_ItemSelected;
            _lstOverride.SelectItem("PlaceFree");
            _lstOverride.LocalPosition = new Vector2i(0, 0);
            _lstOverride.Alignment = ControlAlignments.Bottom;
            overLabel.AddControl(_lstOverride);

            _eraserButton = new ImageButton();
            _eraserButton.ImageNormal = "erasericon";
            _eraserButton.LocalPosition = new Vector2i(5, 0);
            _eraserButton.Alignment = ControlAlignments.Right;
            _clearLabel.AddControl(_eraserButton);
            _eraserButton.Clicked += EraserButtonClicked;

            BuildEntityList();

            _placementManager.PlacementCanceled += PlacementManagerPlacementCanceled;
        }

        protected override void OnCalcPosition()
        {
            base.OnCalcPosition();

            _screenPos = new Vector2i((int) (CluwneLib.CurrentRenderTarget.Size.X / 2f) - (int) (ClientArea.Width / 2f),
                (int) (CluwneLib.CurrentRenderTarget.Size.Y / 2f) - (int) (ClientArea.Height / 2f));
        }

        public override void Draw()
        {
            if (Disposing || !Visible) return;
            _eraserButton.ForegroundColor = _placementManager.Eraser ? new Color(255, 99, 71) : Color.White;
            base.Draw();
        }

        public override void Destroy()
        {
            if (Disposing) return;
            _placementManager.PlacementCanceled -= PlacementManagerPlacementCanceled;
            _entityList.Destroy();
            base.Destroy();
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (base.KeyDown(e))
                return true;

            if (e.Key == Keyboard.Key.Escape)
            {
                Destroy();
                return true;
            }
            return false;
        }

        private void _lstOverride_ItemSelected(Label item, Listbox sender)
        {
            var pMan = (PlacementManager) _placementManager;

            if (pMan.CurrentMode != null)
            {
                var newObjInfo = new PlacementInformation
                {
                    PlacementOption = item.Text,
                    EntityType = pMan.CurrentPermission.EntityType,
                    Range = 2,
                    IsTile = pMan.CurrentPermission.IsTile
                };

                _placementManager.Clear();
                _placementManager.BeginPlacing(newObjInfo);
            }
        }

        private void EraserButtonClicked(ImageButton sender)
        {
            _placementManager.ToggleEraser();
        }

        private void ClearLabelClicked(Label sender, MouseButtonEventArgs e)
        {
            _clearLabel.BackgroundColor = Color.Gray;
            BuildEntityList();
        }

        private void entSearchTextbox_OnSubmit(Textbox sender, string text)
        {
            BuildEntityList(text);
        }

        private void PlacementManagerPlacementCanceled(object sender, EventArgs e)
        {
            foreach (
                var curr in
                _entityList.Components.Where(curr => curr.GetType() == typeof(EntitySpawnSelectButton)))
            {
                ((EntitySpawnSelectButton) curr).Selected = false;
            }
        }

        private void BuildEntityList(string searchStr = null)
        {
            _entityList.Container.DisposeAllChildren();
            _entityList.ResetScrollbars();

            var manager = IoCManager.Resolve<IPrototypeManager>();
            IEnumerable<KeyValuePair<string, EntityPrototype>> templates;
            if (searchStr == null)
            {
                templates = manager.EnumeratePrototypes<EntityPrototype>()
                    .Select(p => new KeyValuePair<string, EntityPrototype>(p.ID, p));
            }
            else
            {
                var searchStrLower = searchStr.ToLower();
                templates = manager.EnumeratePrototypes<EntityPrototype>()
                    .Where(p => p.ID.ToLower().Contains(searchStrLower))
                    .Select(p => new KeyValuePair<string, EntityPrototype>(p.ID, p));
            }

            if (searchStr != null) _clearLabel.BackgroundColor = new Color(211, 211, 211, 255);

            var maxWidth = 0;
            Control lastControl = _entityList.Container;
            foreach (var newButton in templates.Select(entry => new EntitySpawnSelectButton(entry.Value, entry.Key)))
            {
                lastControl.AddControl(newButton);
                lastControl = newButton;
                newButton.FixedWidth = _entityList.Width;
                newButton.Alignment = ControlAlignments.Bottom;
                newButton.DoLayout();
                newButton.Clicked += NewButtonClicked;

                if (newButton.ClientArea.Width > maxWidth)
                    maxWidth = newButton.ClientArea.Width;
            }
        }

        private void NewButtonClicked(EntitySpawnSelectButton sender, EntityPrototype template, string templateName)
        {
            if (sender.Selected)
            {
                sender.Selected = false;
                _placementManager.Clear();
                return;
            }

            foreach (
                var curr in
                _entityList.Components.Where(curr => curr.GetType() == typeof(EntitySpawnSelectButton)))
            {
                ((EntitySpawnSelectButton) curr).Selected = false;
            }

            var overrideMode = "PlaceFree";
            if (_lstOverride.CurrentlySelected != null)
                overrideMode = _lstOverride.CurrentlySelected.Text;

            var newObjInfo = new PlacementInformation
            {
                PlacementOption = overrideMode.Length > 0 ? overrideMode : template.PlacementMode,
                EntityType = templateName,
                Range = 2,
                IsTile = false
            };

            _placementManager.BeginPlacing(newObjInfo);

            sender.Selected = true; //This needs to be last.
        }
    }
}
*/
