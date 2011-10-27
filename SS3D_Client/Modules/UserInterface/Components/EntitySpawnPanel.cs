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
        public EntitySpawnPanel(Size _size)
            : base("Entity Spawn Panel", _size)
        {
            Button closeButton = new Button("Close");
            closeButton.Position = new Point(5, 5);
            closeButton.Clicked += new Button.ButtonPressHandler(closeButton_Clicked);
            components.Add(closeButton);

            Button clearButton = new Button("Deselect");
            clearButton.Position = new Point(80, 5);
            clearButton.Clicked += new Button.ButtonPressHandler(clearButton_Clicked);
            components.Add(clearButton);

            BuildEntityList();

            position = new Point((int)(Gorgon.Screen.Width / 2f) - (int)(this.ClientArea.Width / 2f), (int)(Gorgon.Screen.Height / 2f) - (int)(this.ClientArea.Height / 2f));
        }

        void clearButton_Clicked(Button sender)
        {
            foreach (GuiComponent curr in components)
                if (curr.GetType() == typeof(EntitySpawnSelectButton))
                    ((EntitySpawnSelectButton)curr).selected = false;

            PlacementManager.Singleton.Clear();
        }

        private void BuildEntityList()
        {
            int y_offset = 40;
            foreach (KeyValuePair<string, EntityTemplate> entry in EntityManager.Singleton.TemplateDB.Templates)
            {
                EntitySpawnSelectButton newButton = new EntitySpawnSelectButton(entry.Value, entry.Key);
                components.Add(newButton);
                newButton.Position = new Point(5, y_offset);
                newButton.Update();
                y_offset += 5 + newButton.ClientArea.Height;
                newButton.Clicked += new EntitySpawnSelectButton.EntitySpawnSelectPress(newButton_Clicked);
            }
        }

        void newButton_Clicked(EntitySpawnSelectButton sender, EntityTemplate template, string templateName)
        {
            foreach (GuiComponent curr in components)
                if (curr.GetType() == typeof(EntitySpawnSelectButton))
                    ((EntitySpawnSelectButton)curr).selected = false;

            PlacementInformation newObjInfo = new PlacementInformation();
            newObjInfo.AlignOption = AlignmentOptions.AlignNone;
            newObjInfo.entityType = templateName;
            newObjInfo.isTile = false;

            sender.selected = true;

            PlacementManager.Singleton.BeginPlacing(newObjInfo);
        }

        void closeButton_Clicked(Button sender)
        {
            this.Dispose();
        }

        public override void Update()
        {
            if (disposing || !IsVisible()) return;
            base.Update();
        }

        public override void Render()
        {
            if (disposing || !IsVisible()) return;
            base.Render();
        }

        public override void Dispose()
        {
            if (disposing) return;
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
            return;
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
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