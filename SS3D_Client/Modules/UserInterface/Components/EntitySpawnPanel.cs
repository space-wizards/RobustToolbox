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
            BuildEntityList();
            position = new Point((int)(Gorgon.Screen.Width / 2f) - (int)(this.ClientArea.Width / 2f), (int)(Gorgon.Screen.Height / 2f) - (int)(this.ClientArea.Height / 2f));
            PlacementManager.Singleton.PlacementCanceled += new PlacementManager.PlacementCanceledHandler(PlacementMgr_PlacementCanceled);
        }

        void PlacementMgr_PlacementCanceled(PlacementManager mgr)
        {
            foreach (GuiComponent curr in components)
                if (curr.GetType() == typeof(EntitySpawnSelectButton))
                    ((EntitySpawnSelectButton)curr).selected = false;
        }

        private void BuildEntityList()
        {
            int max_width = 0;
            int y_offset = 5;

            foreach (KeyValuePair<string, EntityTemplate> entry in EntityManager.Singleton.TemplateDB.Templates)
            {
                EntitySpawnSelectButton newButton = new EntitySpawnSelectButton(entry.Value, entry.Key);
                components.Add(newButton);
                newButton.Position = new Point(5, y_offset);
                newButton.Update();
                y_offset += 5 + newButton.ClientArea.Height;
                newButton.Clicked += new EntitySpawnSelectButton.EntitySpawnSelectPress(newButton_Clicked);
                if (newButton.ClientArea.Width > max_width) max_width = newButton.ClientArea.Width;
            }

            foreach (GuiComponent curr in components)
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

            foreach (GuiComponent curr in components)
                if (curr.GetType() == typeof(EntitySpawnSelectButton))
                    ((EntitySpawnSelectButton)curr).selected = false;

            PlacementInformation newObjInfo = new PlacementInformation();

            newObjInfo.placementOption = template.placementMode;
            newObjInfo.entityType = templateName;
            newObjInfo.range = 400;
            newObjInfo.isTile = false;

            sender.selected = true;

            PlacementManager.Singleton.BeginPlacing(newObjInfo);
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
            PlacementManager.Singleton.PlacementCanceled -= new PlacementManager.PlacementCanceledHandler(PlacementMgr_PlacementCanceled);
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