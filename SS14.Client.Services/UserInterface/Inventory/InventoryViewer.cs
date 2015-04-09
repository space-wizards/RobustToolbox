using SFML.Window;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Services.UserInterface.Components;
using SS14.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SS14.Client.Services.UserInterface.Inventory
{
    internal class InventoryViewer : GuiComponent
    {
        private readonly InventoryComponent _inventoryComponent;
        private readonly ScrollableContainer _inventoryContainer;
        private readonly IResourceManager _resourceManager;
        private readonly IUserInterfaceManager _userInterfaceManager;

        public InventoryViewer(InventoryComponent assignedCompo, IUserInterfaceManager userInterfaceManager,
                               IResourceManager resourceManager)
        {
            _userInterfaceManager = userInterfaceManager;
            _resourceManager = resourceManager;

            _inventoryContainer = new ScrollableContainer(assignedCompo.Owner.Uid + "InvViewer", new Size(270, 125),
                                                          _resourceManager);
            _inventoryComponent = assignedCompo;
            _inventoryComponent.Changed += ComponentChanged;
            RebuildInventoryView(_inventoryComponent.MaxSlots, _inventoryComponent.ContainedEntities);
        }

        private void ComponentChanged(InventoryComponent sender, int maxSlots, List<Entity> entities)
        {
            RebuildInventoryView(maxSlots, entities);
        }

        public void RebuildInventoryView(int maxSlots, List<Entity> entities)
        {
            int currX = 0;
            int currY = 0;

            const int spacing = 50;
            const int xOffset = 12;
            const int yOffset = 5;

            _inventoryContainer.components.Clear();

            foreach (Entity entity in entities)
            {
                var slot = new InventorySlotUi(entity, _resourceManager)
                               {
                                   Position = new Point(currX*spacing + xOffset, currY*spacing + yOffset)
                               };

                slot.Clicked += SlotClicked;

                _inventoryContainer.components.Add(slot);

                currX++;

                if (currX < 5) continue;

                currX = 0;
                currY++;
            }

            for (int i = 0; i < (maxSlots - entities.Count); i++)
            {
                var slot = new InventorySlotUi(null, _resourceManager)
                               {
                                   Position = new Point(currX*spacing + xOffset, currY*spacing + yOffset)
                               };

                slot.Clicked += SlotClicked;

                _inventoryContainer.components.Add(slot);

                currX++;

                if (currX < 5) continue;

                currX = 0;
                currY++;
            }

            _inventoryContainer.ResetScrollbars();
        }

        private void SlotClicked(InventorySlotUi sender)
        {
            if (sender.ContainingEntity != null)
                _userInterfaceManager.DragInfo.StartDrag(sender.ContainingEntity);
        }

        public override void Update(float frameTime)
        {
            _inventoryContainer.Position = Position;
            _inventoryContainer.Update(frameTime);
        }

        public override void Render()
        {
            _inventoryContainer.Render();
        }

        public override void Dispose()
        {
            _inventoryComponent.Changed -= ComponentChanged;
            _inventoryContainer.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

		public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (_inventoryContainer.MouseDown(e))
                return true;
            return false;
        }

		public override bool MouseUp(MouseButtonEventArgs e)
        {
            //If dropped on container add to inventory.
            if (_inventoryContainer.MouseUp(e)) return true;
            if (_inventoryContainer.ClientArea.Contains(new Point((int) e.X, (int) e.Y)) &&
                _userInterfaceManager.DragInfo.IsEntity && _userInterfaceManager.DragInfo.IsActive)
            {
                if (!_inventoryComponent.ContainsEntity(_userInterfaceManager.DragInfo.DragEntity))
                {
                    _inventoryComponent.SendInventoryAdd(_userInterfaceManager.DragInfo.DragEntity);
                    _userInterfaceManager.DragInfo.Reset();
                }
                else
                {
                    _userInterfaceManager.DragInfo.Reset();
                }
                return true;
            }
            return false;
        }

		public override void MouseMove(MouseMoveEventArgs e)
        {
            _inventoryContainer.MouseMove(e);
        }

		public override bool MouseWheelMove(MouseWheelEventArgs e)
        {
            return _inventoryContainer.MouseWheelMove(e);
        }
    }
}