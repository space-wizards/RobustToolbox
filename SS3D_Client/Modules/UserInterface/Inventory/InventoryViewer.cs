using System;
using System.Collections.Generic;
using System.Drawing;
using GorgonLibrary.InputDevices;
using CGO;
using SS13.Modules;

namespace SS13.UserInterface
{
    class InventoryViewer : GuiComponent
    {
        private readonly InventoryComponent _inventoryComponent;
        private readonly ScrollableContainer _inventoryContainer;

        public InventoryViewer(InventoryComponent assignedCompo, PlayerController controler)
            : base(controler)
        {
            _inventoryContainer = new ScrollableContainer(assignedCompo.Owner.Uid + "InvViewer", new Size(270, 125));
            _inventoryComponent = assignedCompo;
            _inventoryComponent.Changed += component_Changed;
            _inventoryComponent.UpdateRequired += component_UpdateRequired;
            _inventoryComponent.SendRequestListing();
        }

        void component_UpdateRequired(InventoryComponent sender)
        {
            _inventoryComponent.SendRequestListing();
        }

        void component_Changed(InventoryComponent sender, int maxSlots, List<Entity> entities)
        {
            RebuildInventoryView(maxSlots, entities);
        }

        public void RebuildInventoryView(int maxSlots, List<Entity> entities)
        {
            var curr_x = 0;
            var curr_y = 0;

            var spacing = 50;

            var x_offset = 12;
            var y_offset = 5;

            _inventoryContainer.components.Clear();

            foreach (Entity curr in entities)
            {
                var slot = new InventorySlotUi(curr);
                slot.Position = new Point(curr_x * spacing + x_offset, curr_y * spacing + y_offset);
                slot.Clicked += slot_Clicked;

                _inventoryContainer.components.Add(slot);

                curr_x++;
                if (curr_x >= 5)
                {
                    curr_x = 0;
                    curr_y++;
                }
            }

            for (int i = 0; i < (maxSlots - entities.Count); i++)
            {
                var slot = new InventorySlotUi(null);
                slot.Position = new Point(curr_x * spacing + x_offset, curr_y * spacing + y_offset);
                slot.Clicked += slot_Clicked;

                _inventoryContainer.components.Add(slot);

                curr_x++;
                if (curr_x >= 5)
                {
                    curr_x = 0;
                    curr_y++;
                }
            }

            _inventoryContainer.ResetScrollbars();
        }

        void slot_Clicked(InventorySlotUi sender)
        {
            if (sender.containingEntity != null)
                UiManager.dragInfo.StartDrag(sender.containingEntity);
        }

        public override void Update()
        {
            _inventoryContainer.Position = position;
            _inventoryContainer.Update();
        }

        public override void Render()
        {
            _inventoryContainer.Render();
        }

        public override void Dispose()
        {
            _inventoryComponent.Changed -= component_Changed;
            _inventoryComponent.UpdateRequired -= component_UpdateRequired;
            _inventoryContainer.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (_inventoryContainer.MouseDown(e)) 
                return true;
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            //If dropped on container add to inventory.
            if (_inventoryContainer.MouseUp(e)) return true;
            if (_inventoryContainer.ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)) && UiManager.dragInfo.isEntity && UiManager.dragInfo.dragEntity != null)
            {
                if (!_inventoryComponent.containsEntity(UiManager.dragInfo.dragEntity))
                {
                    _inventoryComponent.SendInventoryAdd(UiManager.dragInfo.dragEntity);
                    UiManager.dragInfo.Reset();
                }
                else
                {
                    UiManager.dragInfo.Reset();
                }
                return true;
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            _inventoryContainer.MouseMove(e);
        }
    }
}
