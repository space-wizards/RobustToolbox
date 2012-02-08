using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Placement;
using ClientServices.Placement;
using GorgonLibrary;
using GorgonLibrary.InputDevices;
using SS13_Shared;

namespace ClientServices.UserInterface.Components
{
    class TileSpawnPanel : Window
    {
        private readonly IResourceManager _resourceManager;
        private readonly IPlacementManager _placementManager;
        private readonly ScrollableContainer _tileList;
        private readonly Label _clearLabel;
        private readonly Textbox _tileSearchTextbox;

        public TileSpawnPanel(Size size, IResourceManager resourceManager, IPlacementManager placementManager)
            : base("Tile Spawn Panel", size, resourceManager)
        {
            _resourceManager = resourceManager;
            _placementManager = placementManager;

            _tileList = new ScrollableContainer("tilespawnlist", new Size(200, 400), _resourceManager) { Position = new Point(5, 5) };

            Label searchLabel = new Label("Tile Search:", _resourceManager);
            searchLabel.Position = new Point(210, 0);
            components.Add(searchLabel);

            _tileSearchTextbox = new Textbox(125, _resourceManager);
            _tileSearchTextbox.Position = new Point(210, 20);
            _tileSearchTextbox.OnSubmit += tileSearchTextbox_OnSubmit;
            components.Add(_tileSearchTextbox);

            _clearLabel = new Label("[Clear Filter]", _resourceManager)
                              {
                                  DrawBackground = true,
                                  DrawBorder = true,
                                  Position = new Point(210, 55)
                              };

            _clearLabel.Clicked += clearLabel_Clicked;
            _clearLabel.BackgroundColor = Color.Gray;
            components.Add(_clearLabel);

            BuildTileList();

            Position = new Point((int)(Gorgon.Screen.Width / 2f) - (int)(ClientArea.Width / 2f), (int)(Gorgon.Screen.Height / 2f) - (int)(ClientArea.Height / 2f));
            _placementManager.PlacementCanceled += PlacementManagerPlacementCanceled;
        }

        void clearLabel_Clicked(Label sender)
        {
            _clearLabel.BackgroundColor = Color.Gray;
            BuildTileList();
        }

        void tileSearchTextbox_OnSubmit(string text)
        {
            BuildTileList(text);
        }

        void PlacementManagerPlacementCanceled(object sender, EventArgs e)
        {
            foreach (var curr in _tileList.components.Where(curr => curr.GetType() == typeof(Label)))
                ((Label)curr).BackgroundColor = Color.Gray;
        }

        private void BuildTileList(string searchStr = null)
        {
            int max_width = 0;
            int y_offset = 5;

            _tileList.components.Clear();
            _tileList.ResetScrollbars();

            List<string> typeNames = (searchStr == null) ?
                Enum.GetNames(typeof(TileType)).Where(x => x.ToLower() != "none").ToList() :
                Enum.GetNames(typeof(TileType)).Where(x => x.ToLower().Contains(searchStr.ToLower()) && x.ToLower() != "none").ToList();
        
            if (searchStr != null) _clearLabel.BackgroundColor = Color.LightGray;

            foreach (string entry in typeNames)
            {
                var tileLabel = new Label(entry, _resourceManager);
                _tileList.components.Add(tileLabel);
                tileLabel.Position = new Point(5, y_offset);
                tileLabel.DrawBackground = true;
                tileLabel.DrawBorder = true;
                tileLabel.Update();
                y_offset += 5 + tileLabel.ClientArea.Height;
                tileLabel.Clicked += new Label.LabelPressHandler(tileLabel_Clicked);
                if (tileLabel.ClientArea.Width > max_width) max_width = tileLabel.ClientArea.Width;
            }

            foreach (GuiComponent curr in _tileList.components)
                if (curr.GetType() == typeof(Label))
                    ((Label)curr).FixedWidth = max_width;
        }

        void tileLabel_Clicked(Label sender)
        {
            foreach (GuiComponent curr in _tileList.components)
                if (curr.GetType() == typeof(Label))
                    ((Label)curr).BackgroundColor = Color.Gray;

            var newObjInfo = new PlacementInformation();

            newObjInfo.PlacementOption = PlacementOption.AlignTileAnyFree;
            newObjInfo.TileType = (TileType)Enum.Parse(typeof(TileType), sender.Text.Text, true);
            newObjInfo.Range = 400;
            newObjInfo.IsTile = true;

            _placementManager.BeginPlacing(newObjInfo);

            sender.BackgroundColor = Color.ForestGreen;
        }

        public override void Update()
        {
            if (disposing || !IsVisible()) return;
            base.Update();
            if (_tileList != null)
            {
                _tileList.Position = new Point(ClientArea.X + 5, ClientArea.Y + 5);
                _tileList.Update();
            }
        }

        public override void Render()
        {
            if (disposing || !IsVisible()) return;
            base.Render();
            _tileList.Render();
        }

        public override void Dispose()
        {
            if (disposing) return;
            _placementManager.PlacementCanceled -= PlacementManagerPlacementCanceled;
            base.Dispose();
            _tileList.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (_tileList.MouseDown(e)) return true;
            if (base.MouseDown(e)) return true;
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (_tileList.MouseUp(e)) return true;
            if (base.MouseUp(e)) return true;
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return;
            _tileList.MouseMove(e);
            base.MouseMove(e);
            return;
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            if (_tileList.MouseWheelMove(e)) return true;
            if (base.MouseWheelMove(e)) return true;
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (_tileList.KeyDown(e)) return true;
            if (base.KeyDown(e)) return true;
            return false;
        }
    }
}