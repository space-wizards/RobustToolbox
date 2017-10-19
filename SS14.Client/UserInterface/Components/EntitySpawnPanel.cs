using OpenTK.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Placement;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    internal class EntitySpawnPanel : Window
    {
        private readonly Label _clearLabel;
        private readonly Textbox _entSearchTextbox;
        private readonly ScrollableContainer _entityList;
        private readonly ImageButton _eraserButton;
        private readonly Listbox _lstOverride;
        private readonly Label _overLabel;
        private readonly IPlacementManager _placementManager;
        public EntitySpawnPanel(Vector2i size, IResourceCache resourceCache, IPlacementManager placementManager)
            : base("Entity Spawn Panel", size, resourceCache)
        {
            _placementManager = placementManager;

            _entityList = new ScrollableContainer("entspawnlist", new Vector2i(200, 400), ResourceCache)
            { Position = new Vector2i(5, 5) };
            Components.Add(_entityList);

            var searchLabel = new Label("Entity Search:", "CALIBRI") { Position = new Vector2i(210, 0) };
            Components.Add(searchLabel);

            _entSearchTextbox = new Textbox(125) { Position = new Vector2i(210, 20) };
            _entSearchTextbox.OnSubmit += entSearchTextbox_OnSubmit;
            Components.Add(_entSearchTextbox);

            _clearLabel = new Label("[Clear Filter]", "CALIBRI")
            {
                DrawBackground = true,
                DrawBorder = true,
                Position = new Vector2i(210, 55)
            };

            _overLabel = new Label("Override Placement:", "CALIBRI")
            {
                Position = _clearLabel.Position + new Vector2i(0, _clearLabel.ClientArea.Height + 15)
            };

            Components.Add(_overLabel);

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
            _lstOverride.SelectItem("PlaceFree");
            _lstOverride.ItemSelected += _lstOverride_ItemSelected;
            _lstOverride.Position = _overLabel.Position + new Vector2i(0, _overLabel.ClientArea.Height);
            Components.Add(_lstOverride);

            _clearLabel.Clicked += ClearLabelClicked;
            _clearLabel.BackgroundColor = Color4.Gray;
            Components.Add(_clearLabel);

            _eraserButton = new ImageButton
            {
                ImageNormal = "erasericon",
                Position =
                                        new Vector2i(_clearLabel.Position.X + _clearLabel.ClientArea.Width + 5,
                                                  _clearLabel.Position.Y)
            };

            //eraserButton.Position = new Vector2i(clearLabel.ClientArea.Right + 5, clearLabel.ClientArea.Top); Clientarea not updating properly. FIX THIS
            _eraserButton.Clicked += EraserButtonClicked;
            Components.Add(_eraserButton);

            BuildEntityList();

            Position = new Vector2i((int)(CluwneLib.CurrentRenderTarget.Size.X / 2f) - (int)(ClientArea.Width / 2f),
                                 (int)(CluwneLib.CurrentRenderTarget.Size.Y / 2f) - (int)(ClientArea.Height / 2f));
            _placementManager.PlacementCanceled += PlacementManagerPlacementCanceled;
        }

        private void _lstOverride_ItemSelected(Label item, Listbox sender)
        {
            var pMan = (PlacementManager)_placementManager;

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
            _clearLabel.BackgroundColor = Color4.Gray;
            BuildEntityList();
        }

        private void entSearchTextbox_OnSubmit(string text, Textbox sender)
        {
            BuildEntityList(text);
        }

        private void PlacementManagerPlacementCanceled(object sender, EventArgs e)
        {
            foreach (
                Control curr in
                    _entityList.Components.Where(curr => curr.GetType() == typeof(EntitySpawnSelectButton)))
                ((EntitySpawnSelectButton)curr).selected = false;
        }

        private void BuildEntityList(string searchStr = null)
        {
            int maxWidth = 0;
            int yOffset = 5;

            _entityList.Components.Clear();
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

            if (searchStr != null) _clearLabel.BackgroundColor = new Color4(211, 211, 211, 255);

            foreach (
                EntitySpawnSelectButton newButton in
                    templates.Select(entry => new EntitySpawnSelectButton(entry.Value, entry.Key, ResourceCache)))
            {
                _entityList.Components.Add(newButton);
                newButton.Position = new Vector2i(5, yOffset);
                newButton.Update(0);
                yOffset += 5 + newButton.ClientArea.Height;
                newButton.Clicked += NewButtonClicked;

                if (newButton.ClientArea.Width > maxWidth) maxWidth = newButton.ClientArea.Width;
            }

            foreach (
                Control curr in
                    _entityList.Components.Where(curr => curr.GetType() == typeof(EntitySpawnSelectButton)))
                ((EntitySpawnSelectButton)curr).fixed_width = maxWidth;
        }

        private void NewButtonClicked(EntitySpawnSelectButton sender, EntityPrototype template, string templateName)
        {
            if (sender.selected)
            {
                sender.selected = false;
                _placementManager.Clear();
                return;
            }

            foreach (
                Control curr in
                    _entityList.Components.Where(curr => curr.GetType() == typeof(EntitySpawnSelectButton)))
                ((EntitySpawnSelectButton)curr).selected = false;

            string overrideMode = "";
            if (_lstOverride.CurrentlySelected != null)
                if (_lstOverride.CurrentlySelected.Text != "None")
                    overrideMode = _lstOverride.CurrentlySelected.Text;

            var newObjInfo = new PlacementInformation
            {
                PlacementOption = overrideMode.Length > 0 ? overrideMode : template.PlacementMode,
                EntityType = templateName,
                Range = 2,
                IsTile = false
            };

            _placementManager.BeginPlacing(newObjInfo);

            sender.selected = true; //This needs to be last.
        }

        public override void Update(float frameTime)
        {
            if (Disposing || !IsVisible()) return;
            base.Update(frameTime);
        }

        public override void Draw()
        {
            if (Disposing || !IsVisible()) return;
            _eraserButton.ForegroundColor = _placementManager.Eraser ? new Color4(255, 99, 71, 255) : Color4.White;
            base.Draw();
        }

        public override void Dispose()
        {
            if (Disposing) return;
            _placementManager.PlacementCanceled -= PlacementManagerPlacementCanceled;
            _entityList.Dispose();
            base.Dispose();
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (Disposing || !IsVisible()) return false;
            if (base.MouseDown(e)) return true;
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (Disposing || !IsVisible()) return false;
            if (base.MouseUp(e)) return true;
            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            if (Disposing || !IsVisible()) return;
            base.MouseMove(e);
        }

        public override bool MouseWheelMove(MouseWheelScrollEventArgs e)
        {
            if (_entityList.MouseWheelMove(e)) return true;
            if (base.MouseWheelMove(e)) return true;
            return false;
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (e.Code == Keyboard.Key.Escape)
            {
                Dispose();
                return true;
            }
            return false;
        }
    }
}
