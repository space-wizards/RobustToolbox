using OpenTK.Graphics;
using SS14.Client.Graphics;
using SS14.Shared.Interfaces.Map;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.IoC;
using System;
using System.Linq;
using Vector2i = SS14.Shared.Maths.Vector2i;
using SS14.Client.Graphics.Input;

namespace SS14.Client.UserInterface.Components
{
    internal class TileSpawnPanel : Window
    {
        private readonly Label _clearLabel;
        private readonly IPlacementManager _placementManager;
        private readonly ScrollableContainer _tileList;
        private readonly Textbox _tileSearchTextbox;

        public TileSpawnPanel(Vector2i size, IResourceCache resourceCache, IPlacementManager placementManager)
            : base("Tile Spawn Panel", size, resourceCache)
        {
            _placementManager = placementManager;

            _tileList = new ScrollableContainer("tilespawnlist", new Vector2i(200, 400), _resourceCache)
            { Position = new Vector2i(5, 5) };
            Components.Add(_tileList);

            var searchLabel = new Label("Tile Search:", "CALIBRI") { Position = new Vector2i(210, 0) };
            Components.Add(searchLabel);

            _tileSearchTextbox = new Textbox(125) { Position = new Vector2i(210, 20) };
            _tileSearchTextbox.OnSubmit += tileSearchTextbox_OnSubmit;
            Components.Add(_tileSearchTextbox);

            _clearLabel = new Label("[Clear Filter]", "CALIBRI")
            {
                DrawBackground = true,
                DrawBorder = true,
                Position = new Vector2i(210, 55)
            };

            _clearLabel.Clicked += ClearLabelClicked;
            _clearLabel.BackgroundColor = Color4.Gray;
            Components.Add(_clearLabel);

            BuildTileList();

            Position = new Vector2i((int)(CluwneLib.CurrentRenderTarget.Size.X / 2f) - (int)(ClientArea.Width / 2f),
                                 (int)(CluwneLib.CurrentRenderTarget.Size.Y / 2f) - (int)(ClientArea.Height / 2f));
            _placementManager.PlacementCanceled += PlacementManagerPlacementCanceled;
        }

        private void ClearLabelClicked(Label sender, MouseButtonEventArgs e)
        {
            _clearLabel.BackgroundColor = Color4.Gray;
            BuildTileList();
        }

        private void tileSearchTextbox_OnSubmit(string text, Textbox sender)
        {
            BuildTileList(text);
        }

        private void PlacementManagerPlacementCanceled(object sender, EventArgs e)
        {
            foreach (Control curr in _tileList.Components.Where(curr => curr.GetType() == typeof(Label)))
                ((Label)curr).BackgroundColor = Color4.Gray;
        }

        private void BuildTileList(string searchStr = null)
        {
            int maxWidth = 0;
            int yOffset = 5;

            _tileList.Components.Clear();
            _tileList.ResetScrollbars();

            var tileDefs = IoCManager.Resolve<ITileDefinitionManager>().Select(td => td.Name);

            if (!string.IsNullOrEmpty(searchStr))
            {
                tileDefs = tileDefs.Where(s => s.IndexOf(searchStr, StringComparison.InvariantCultureIgnoreCase) >= 0);
                _clearLabel.BackgroundColor = new Color4(211, 211, 211, 255);
            }

            foreach (string entry in tileDefs)
            {
                var tileLabel = new Label(entry, "CALIBRI");
                _tileList.Components.Add(tileLabel);
                tileLabel.Position = new Vector2i(5, yOffset);
                tileLabel.DrawBackground = true;
                tileLabel.DrawBorder = true;
                tileLabel.Update(0);
                yOffset += 5 + tileLabel.ClientArea.Height;
                tileLabel.Clicked += TileLabelClicked;
                if (tileLabel.ClientArea.Width > maxWidth) maxWidth = tileLabel.ClientArea.Width;
            }

            foreach (Control curr in _tileList.Components.Where(curr => curr.GetType() == typeof(Label)))
                ((Label)curr).FixedWidth = maxWidth;
        }

        private void TileLabelClicked(Label sender, MouseButtonEventArgs e)
        {
            foreach (Control curr in _tileList.Components.Where(curr => curr.GetType() == typeof(Label)))
                ((Label)curr).BackgroundColor = Color4.Gray;

            var newObjInfo = new PlacementInformation
            {
                PlacementOption = "AlignTileAny",
                TileType = IoCManager.Resolve<ITileDefinitionManager>()[sender.Text].TileId,
                Range = 400,
                IsTile = true
            };

            _placementManager.BeginPlacing(newObjInfo);

            sender.BackgroundColor = new Color4(34, 139, 34, 255);
        }

        public override void Update(float frameTime)
        {
            if (Disposing || !IsVisible()) return;
            base.Update(frameTime);
        }

        public override void Draw()
        {
            if (Disposing || !IsVisible()) return;
            base.Draw();
        }

        public override void Dispose()
        {
            if (Disposing) return;
            _placementManager.PlacementCanceled -= PlacementManagerPlacementCanceled;
            _tileList.Dispose();
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
            if (_tileList.MouseWheelMove(e)) return true;
            if (base.MouseWheelMove(e)) return true;
            return false;
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (e.Key == Keyboard.Key.Escape)
            {
                Dispose();
                return true;
            }
            return false;
        }
    }
}
