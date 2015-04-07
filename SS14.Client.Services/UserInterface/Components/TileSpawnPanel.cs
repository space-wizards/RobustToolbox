using GorgonLibrary;
using GorgonLibrary.InputDevices;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class TileSpawnPanel : Window
    {
        private readonly Label _clearLabel;
        private readonly IPlacementManager _placementManager;
        private readonly IResourceManager _resourceManager;
        private readonly ScrollableContainer _tileList;
        private readonly Textbox _tileSearchTextbox;

        public TileSpawnPanel(Size size, IResourceManager resourceManager, IPlacementManager placementManager)
            : base("Tile Spawn Panel", size, resourceManager)
        {
            _resourceManager = resourceManager;
            _placementManager = placementManager;

            _tileList = new ScrollableContainer("tilespawnlist", new Size(200, 400), _resourceManager)
                            {Position = new Point(5, 5)};
            components.Add(_tileList);

            var searchLabel = new Label("Tile Search:", "CALIBRI", _resourceManager) {Position = new Point(210, 0)};
            components.Add(searchLabel);

            _tileSearchTextbox = new Textbox(125, _resourceManager) {Position = new Point(210, 20)};
            _tileSearchTextbox.OnSubmit += tileSearchTextbox_OnSubmit;
            components.Add(_tileSearchTextbox);

            _clearLabel = new Label("[Clear Filter]", "CALIBRI", _resourceManager)
                              {
                                  DrawBackground = true,
                                  DrawBorder = true,
                                  Position = new Point(210, 55)
                              };

            _clearLabel.Clicked += ClearLabelClicked;
            _clearLabel.BackgroundColor = Color.Gray;
            components.Add(_clearLabel);

            BuildTileList();

            Position = new Point((int) (Gorgon.CurrentRenderTarget.Width/2f) - (int) (ClientArea.Width/2f),
                                 (int) (Gorgon.CurrentRenderTarget.Height/2f) - (int) (ClientArea.Height/2f));
            _placementManager.PlacementCanceled += PlacementManagerPlacementCanceled;
        }

        private void ClearLabelClicked(Label sender, MouseInputEventArgs e)
        {
            _clearLabel.BackgroundColor = Color.Gray;
            BuildTileList();
        }

        private void tileSearchTextbox_OnSubmit(string text, Textbox sender)
        {
            BuildTileList(text);
        }

        private void PlacementManagerPlacementCanceled(object sender, EventArgs e)
        {
            foreach (GuiComponent curr in _tileList.components.Where(curr => curr.GetType() == typeof (Label)))
                ((Label) curr).BackgroundColor = Color.Gray;
        }

        private void BuildTileList(string searchStr = null)
        {
            int maxWidth = 0;
            int yOffset = 5;

            _tileList.components.Clear();
            _tileList.ResetScrollbars();

            var tileDefs = IoCManager.Resolve<ITileDefinitionManager>().Select(td => td.Name);

            if (!string.IsNullOrEmpty(searchStr))
            {
                tileDefs = tileDefs.Where(s => s.IndexOf(searchStr, StringComparison.InvariantCultureIgnoreCase) >= 0);
                _clearLabel.BackgroundColor = Color.LightGray;
            }

            foreach (string entry in tileDefs)
            {
                var tileLabel = new Label(entry, "CALIBRI", _resourceManager);
                _tileList.components.Add(tileLabel);
                tileLabel.Position = new Point(5, yOffset);
                tileLabel.DrawBackground = true;
                tileLabel.DrawBorder = true;
                tileLabel.Update(0);
                yOffset += 5 + tileLabel.ClientArea.Height;
                tileLabel.Clicked += TileLabelClicked;
                if (tileLabel.ClientArea.Width > maxWidth) maxWidth = tileLabel.ClientArea.Width;
            }

            foreach (GuiComponent curr in _tileList.components.Where(curr => curr.GetType() == typeof (Label)))
                ((Label) curr).FixedWidth = maxWidth;
        }

        private void TileLabelClicked(Label sender, MouseInputEventArgs e)
        {
            foreach (GuiComponent curr in _tileList.components.Where(curr => curr.GetType() == typeof (Label)))
                ((Label) curr).BackgroundColor = Color.Gray;

            var newObjInfo = new PlacementInformation
                                 {
                                     PlacementOption = "AlignTileAny",
                                     TileType = IoCManager.Resolve<ITileDefinitionManager>()[sender.Text.Text].TileId,
                                     Range = 400,
                                     IsTile = true
                                 };

            _placementManager.BeginPlacing(newObjInfo);

            sender.BackgroundColor = Color.ForestGreen;
        }

        public override void Update(float frameTime)
        {
            if (disposing || !IsVisible()) return;
            base.Update(frameTime);
        }

        public override void Render()
        {
            if (disposing || !IsVisible()) return;
            base.Render();
        }

        public override void Dispose()
        {
            if (disposing) return;
            _placementManager.PlacementCanceled -= PlacementManagerPlacementCanceled;
            _tileList.Dispose();
            base.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (base.MouseDown(e)) return true;
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (base.MouseUp(e)) return true;
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return;
            base.MouseMove(e);
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            if (_tileList.MouseWheelMove(e)) return true;
            if (base.MouseWheelMove(e)) return true;
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (base.KeyDown(e)) return true;
            return false;
        }
    }
}