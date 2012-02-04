using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS3D.UserInterface;
using Lidgren.Network;
using SS3D_shared;
using SS3D_shared.GO;
using CGO;
using ClientResourceManager;
using SS3D.Modules;

namespace SS3D.UserInterface
{
    class InventoryViewer : GuiComponent
    {
        InventoryComponent component;
        ScrollableContainer invContainer;

        public InventoryViewer(InventoryComponent assignedCompo, PlayerController controler)
            : base(controler)
        {
            invContainer = new ScrollableContainer(assignedCompo.Owner.Uid.ToString() + "InvViewer", new Size(270, 125));
            component = assignedCompo;
            component.Changed += new InventoryComponent.InventoryComponentUpdateHandler(component_Changed);
            component.UpdateRequired += new InventoryComponent.InventoryUpdateRequiredHandler(component_UpdateRequired);
            component.SendRequestListing();
        }

        void component_UpdateRequired(InventoryComponent sender)
        {
            component.SendRequestListing();
        }

        void component_Changed(InventoryComponent sender, int maxSlots, List<Entity> entities)
        {
            RebuildInventoryView(maxSlots, entities);
        }

        public void RebuildInventoryView(int maxSlots, List<Entity> entities)
        {
            int curr_x = 0;
            int curr_y = 0;

            int spacing = 50;

            int x_offset = 12;
            int y_offset = 5;

            invContainer.components.Clear();

            foreach (Entity curr in entities)
            {
                InventorySlotUi slot = new InventorySlotUi(curr);
                slot.Position = new Point(curr_x * spacing + x_offset, curr_y * spacing + y_offset);
                slot.Clicked += new InventorySlotUi.InventoryClickHandler(slot_Clicked);

                invContainer.components.Add(slot);

                curr_x++;
                if (curr_x >= 5)
                {
                    curr_x = 0;
                    curr_y++;
                }
            }

            for (int i = 0; i < (maxSlots - entities.Count); i++)
            {
                InventorySlotUi slot = new InventorySlotUi(null);
                slot.Position = new Point(curr_x * spacing + x_offset, curr_y * spacing + y_offset);
                slot.Clicked += new InventorySlotUi.InventoryClickHandler(slot_Clicked);

                invContainer.components.Add(slot);

                curr_x++;
                if (curr_x >= 5)
                {
                    curr_x = 0;
                    curr_y++;
                }
            }

            invContainer.ResetScrollbars();
        }

        void slot_Clicked(InventorySlotUi sender)
        {
            if (sender.containingEntity != null)
                UiManager.Singleton.dragInfo.StartDrag(sender.containingEntity);
        }

        public override void Update()
        {
            invContainer.Position = position;
            invContainer.Update();
        }

        public override void Render()
        {
            invContainer.Render();
        }

        public override void Dispose()
        {
            component.Changed -= new InventoryComponent.InventoryComponentUpdateHandler(component_Changed);
            component.UpdateRequired -= new InventoryComponent.InventoryUpdateRequiredHandler(component_UpdateRequired);
            invContainer.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (invContainer.MouseDown(e)) 
                return true;
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            //If dropped on container add to inventory.
            if (invContainer.MouseUp(e)) return true;
            if (invContainer.ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)) && UiManager.Singleton.dragInfo.isEntity && UiManager.Singleton.dragInfo.dragEntity != null)
            {
                if (!component.containsEntity(UiManager.Singleton.dragInfo.dragEntity))
                {
                    component.SendInventoryAdd(UiManager.Singleton.dragInfo.dragEntity);
                    UiManager.Singleton.dragInfo.Reset();
                }
                else
                {
                    UiManager.Singleton.dragInfo.Reset();
                }
                return true;
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            invContainer.MouseMove(e);
        }
    }
}
