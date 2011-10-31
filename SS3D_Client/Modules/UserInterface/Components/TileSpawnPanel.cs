using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D.UserInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS3D.Modules.Network;
using Lidgren.Network;
using SS3D_shared;
using CGO;
using SS3D.Modules;
using SS3D_shared.HelperClasses;

namespace SS3D.UserInterface
{
    class TileSpawnPanel : Window
    {
        ScrollableContainer tileList;
        Label clearLabel;
        Textbox tileSearchTextbox;

        public TileSpawnPanel(Size _size)
            : base("Tile Spawn Panel", _size)
        {
            tileList = new ScrollableContainer("tilespawnlist", new Size(200, 400));
            tileList.Position = new Point(5, 5);

            Label searchLabel = new Label("Tile Search:");
            searchLabel.Position = new Point(210, 0);
            components.Add(searchLabel);

            tileSearchTextbox = new Textbox(125);
            tileSearchTextbox.Position = new Point(210, 20);
            tileSearchTextbox.OnSubmit += new Textbox.TextSubmitHandler(tileSearchTextbox_OnSubmit);
            components.Add(tileSearchTextbox);

            clearLabel = new Label("[Clear Filter]");
            clearLabel.drawBackground = true;
            clearLabel.drawBorder = true;
            clearLabel.Position = new Point(210, 55);
            clearLabel.Clicked += new Label.LabelPressHandler(clearLabel_Clicked);
            clearLabel.backgroundColor = Color.Gray;
            components.Add(clearLabel);

            BuildTileList();

            position = new Point((int)(Gorgon.Screen.Width / 2f) - (int)(this.ClientArea.Width / 2f), (int)(Gorgon.Screen.Height / 2f) - (int)(this.ClientArea.Height / 2f));
            PlacementManager.Singleton.PlacementCanceled += new PlacementManager.PlacementCanceledHandler(PlacementMgr_PlacementCanceled);
        }

        void clearLabel_Clicked(Label sender)
        {
            clearLabel.backgroundColor = Color.Gray;
            BuildTileList();
        }

        void tileSearchTextbox_OnSubmit(string text)
        {
            BuildTileList(text);
        }

        void PlacementMgr_PlacementCanceled(PlacementManager mgr)
        {
            foreach (GuiComponent curr in tileList.components)
                if (curr.GetType() == typeof(Label))
                    ((Label)curr).backgroundColor = Color.Gray;
        }

        private void BuildTileList(string searchStr = null)
        {
            int max_width = 0;
            int y_offset = 5;

            tileList.components.Clear();
            tileList.ResetScrollbars();

            List<string> typeNames = (searchStr == null) ? 
                Enum.GetNames(typeof(TileType)).ToList() :
                Enum.GetNames(typeof(TileType)).Where(x => x.ToLower().Contains(searchStr.ToLower())).ToList();
        
            if (searchStr != null) clearLabel.backgroundColor = Color.LightGray;

            foreach (string entry in typeNames)
            {
                Label tileLabel = new Label(entry);
                tileList.components.Add(tileLabel);
                tileLabel.Position = new Point(5, y_offset);
                tileLabel.drawBackground = true;
                tileLabel.drawBorder = true;
                tileLabel.Update();
                y_offset += 5 + tileLabel.ClientArea.Height;
                tileLabel.Clicked += new Label.LabelPressHandler(tileLabel_Clicked);
                if (tileLabel.ClientArea.Width > max_width) max_width = tileLabel.ClientArea.Width;
            }

            foreach (GuiComponent curr in tileList.components)
                if (curr.GetType() == typeof(Label))
                    ((Label)curr).fixed_width = max_width;
        }

        void tileLabel_Clicked(Label sender)
        {
            foreach (GuiComponent curr in tileList.components)
                if (curr.GetType() == typeof(Label))
                    ((Label)curr).backgroundColor = Color.Gray;

            PlacementInformation newObjInfo = new PlacementInformation();

            newObjInfo.placementOption = PlacementOption.AlignTileAnyFree;
            newObjInfo.tileType = (TileType)Enum.Parse(typeof(TileType), sender.Text.Text, true);
            newObjInfo.range = 400;
            newObjInfo.isTile = true;

            PlacementManager.Singleton.BeginPlacing(newObjInfo);

            sender.backgroundColor = Color.ForestGreen;
        }

        public override void Update()
        {
            if (disposing || !IsVisible()) return;
            base.Update();
            if (tileList != null)
            {
                tileList.Position = new Point(clientArea.X + 5, clientArea.Y + 5);
                tileList.Update();
            }
        }

        public override void Render()
        {
            if (disposing || !IsVisible()) return;
            base.Render();
            tileList.Render();
        }

        public override void Dispose()
        {
            if (disposing) return;
            PlacementManager.Singleton.PlacementCanceled -= new PlacementManager.PlacementCanceledHandler(PlacementMgr_PlacementCanceled);
            base.Dispose();
            tileList.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (tileList.MouseDown(e)) return true;
            if (base.MouseDown(e)) return true;
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (tileList.MouseUp(e)) return true;
            if (base.MouseUp(e)) return true;
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return;
            tileList.MouseMove(e);
            base.MouseMove(e);
            return;
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            if (tileList.MouseWheelMove(e)) return true;
            if (base.MouseWheelMove(e)) return true;
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (tileList.KeyDown(e)) return true;
            if (base.KeyDown(e)) return true;
            return false;
        }
    }
}