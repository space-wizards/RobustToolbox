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
    class EntitySpawnPanel : Window
    {
        ScrollableContainer entityList;
        Label clearLabel;
        Textbox entSearchTextbox;
        SimpleImageButton eraserButton;

        public EntitySpawnPanel(Size _size)
            : base("Entity Spawn Panel", _size)
        {
            entityList = new ScrollableContainer("entspawnlist", new Size(200, 400));
            entityList.Position = new Point(5, 5);

            Label searchLabel = new Label("Entity Search:");
            searchLabel.Position = new Point(210, 0);
            components.Add(searchLabel);

            entSearchTextbox = new Textbox(125);
            entSearchTextbox.Position = new Point(210, 20);
            entSearchTextbox.OnSubmit += new Textbox.TextSubmitHandler(entSearchTextbox_OnSubmit);
            components.Add(entSearchTextbox);

            clearLabel = new Label("[Clear Filter]");
            clearLabel.drawBackground = true;
            clearLabel.drawBorder = true;
            clearLabel.Position = new Point(210, 55);
            clearLabel.Clicked += new Label.LabelPressHandler(clearLabel_Clicked);
            clearLabel.backgroundColor = Color.Gray;
            components.Add(clearLabel);

            eraserButton = new SimpleImageButton("erasericon");
            //eraserButton.Position = new Point(clearLabel.ClientArea.Right + 5, clearLabel.ClientArea.Top); Clientarea not updating properly. FIX THIS
            eraserButton.Position = new Point(clearLabel.Position.X + clearLabel.ClientArea.Width + 5, clearLabel.Position.Y);
            eraserButton.Clicked += new SimpleImageButton.SimpleImageButtonPressHandler(eraserButton_Clicked);
            components.Add(eraserButton);

            BuildEntityList();

            position = new Point((int)(Gorgon.Screen.Width / 2f) - (int)(this.ClientArea.Width / 2f), (int)(Gorgon.Screen.Height / 2f) - (int)(this.ClientArea.Height / 2f));
            PlacementManager.Singleton.PlacementCanceled += new PlacementManager.PlacementCanceledHandler(PlacementMgr_PlacementCanceled);
        }

        void eraserButton_Clicked(SimpleImageButton sender)
        {
            PlacementManager.Singleton.ToggleEraser();
        }

        void clearLabel_Clicked(Label sender)
        {
            clearLabel.backgroundColor = Color.Gray;
            BuildEntityList();
        }

        void entSearchTextbox_OnSubmit(string text)
        {
            BuildEntityList(text);
        }

        void PlacementMgr_PlacementCanceled(PlacementManager mgr)
        {
            foreach (GuiComponent curr in entityList.components)
                if (curr.GetType() == typeof(EntitySpawnSelectButton))
                    ((EntitySpawnSelectButton)curr).selected = false;
        }

        private void BuildEntityList(string searchStr = null)
        {
            int max_width = 0;
            int y_offset = 5;

            entityList.components.Clear();
            entityList.ResetScrollbars();

            List<KeyValuePair<string, EntityTemplate>> templates = (searchStr == null) ? 
                EntityManager.Singleton.TemplateDB.Templates.ToList() : 
                EntityManager.Singleton.TemplateDB.Templates.Where(x => x.Value.Name.ToLower().Contains(searchStr.ToLower())).ToList();
        

            if (searchStr != null) clearLabel.backgroundColor = Color.LightGray;

            foreach (KeyValuePair<string, EntityTemplate> entry in templates)
            {
                EntitySpawnSelectButton newButton = new EntitySpawnSelectButton(entry.Value, entry.Key);
                entityList.components.Add(newButton);
                newButton.Position = new Point(5, y_offset);
                newButton.Update();
                y_offset += 5 + newButton.ClientArea.Height;
                newButton.Clicked += new EntitySpawnSelectButton.EntitySpawnSelectPress(newButton_Clicked);
                if (newButton.ClientArea.Width > max_width) max_width = newButton.ClientArea.Width;
            }

            foreach (GuiComponent curr in entityList.components)
                if (curr.GetType() == typeof(EntitySpawnSelectButton))
                    ((EntitySpawnSelectButton)curr).fixed_width = max_width;
        }

        void newButton_Clicked(EntitySpawnSelectButton sender, EntityTemplate template, string templateName)
        {
            if (sender.selected)
            {
                sender.selected = false;
                PlacementManager.Singleton.Clear();
                return;
            }

            foreach (GuiComponent curr in entityList.components)
                if (curr.GetType() == typeof(EntitySpawnSelectButton))
                    ((EntitySpawnSelectButton)curr).selected = false;

            PlacementInformation newObjInfo = new PlacementInformation();

            newObjInfo.placementOption = template.placementMode;
            newObjInfo.entityType = templateName;
            newObjInfo.range = 400;
            newObjInfo.isTile = false;

            PlacementManager.Singleton.BeginPlacing(newObjInfo);

            sender.selected = true; //This needs to be last.
        }

        public override void Update()
        {
            if (disposing || !IsVisible()) return;
            base.Update();
            if (entityList != null)
            {
                entityList.Position = new Point(clientArea.X + 5, clientArea.Y + 5);
                entityList.Update();
            }
        }

        public override void Render()
        {
            if (disposing || !IsVisible()) return;
            eraserButton.Color = PlacementManager.Singleton.eraser ? Color.Tomato : Color.White;
            base.Render();
            entityList.Render();
        }

        public override void Dispose()
        {
            if (disposing) return;
            PlacementManager.Singleton.PlacementCanceled -= new PlacementManager.PlacementCanceledHandler(PlacementMgr_PlacementCanceled);
            base.Dispose();
            entityList.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (entityList.MouseDown(e)) return true;
            if (base.MouseDown(e)) return true;
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (entityList.MouseUp(e)) return true;
            if (base.MouseUp(e)) return true;
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return;
            entityList.MouseMove(e);
            base.MouseMove(e);
            return;
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            if (entityList.MouseWheelMove(e)) return true;
            if (base.MouseWheelMove(e)) return true;
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (entityList.KeyDown(e)) return true;
            if (base.KeyDown(e)) return true;
            return false;
        }
    }
}