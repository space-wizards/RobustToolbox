using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using GorgonLibrary;
using GorgonLibrary.Framework;
using GorgonLibrary.GUI;
using GorgonLibrary.Graphics;
using GorgonLibrary.Graphics.Utilities;
using GorgonLibrary.InputDevices;
using ClientResourceManager;
using SS3D.Modules;

using CGO;

namespace SS3D.UserInterface
{
    public class HumanInventory : GuiComponent
    {
        private Dictionary<EquipmentSlot, GuiItemSlot> inventorySlots;
        private HumanHandsGui handsGUI;

        private Entity heldEntity;
        private EquipmentSlot lastSlot = EquipmentSlot.None;
        private Vector2D mousePos;

        private Sprite outline;
        private int slotWidth;

        public HumanInventory(PlayerController _playerController)
            : base(_playerController)
        {
            componentClass = SS3D_shared.GuiComponentType.HumanInventory;
            inventorySlots = new Dictionary<EquipmentSlot, GuiItemSlot>();
            outline = ResMgr.Singleton.GetSprite("outline");

            slotWidth = (int)ResMgr.Singleton.GetSprite("slot").Width;

            int width = 48 + slotWidth + (int)outline.Width + slotWidth;
            int height = 64 + (int)(outline.Height);

            clientArea = new Rectangle(Gorgon.Screen.Width - 25 - width, 590, width, height);
            mousePos = Vector2D.Zero;

            SetVisible(false);
        }

        // Set up all the slots for the body
        private void SetUpSlots()
        {
            inventorySlots.Clear();
            if (!playerController.controlledAtom.HasComponent(SS3D_shared.GO.ComponentFamily.Equipment))
                return;

            Entity playerEntity = (Entity)playerController.controlledAtom;
            EquipmentComponent equipComponent = (EquipmentComponent)playerEntity.GetComponent(SS3D_shared.GO.ComponentFamily.Equipment);
            foreach (EquipmentSlot part in equipComponent.activeSlots)
            {
                inventorySlots.Add(part, new GuiItemSlot(playerController, part));
            }
            int i = 0;
            bool second = false;
            foreach (GuiItemSlot slot in inventorySlots.Values)
            {
                if (i >= inventorySlots.Count / 2)
                {
                    second = true;
                    i = 0;
                }
                if (!second)
                    slot.Position = new Point(clientArea.X + slot.Position.X + 12, clientArea.Y + slot.Position.Y + (i * 56));
                else
                    slot.Position = new Point(clientArea.X + clientArea.Width - 12 - slotWidth, clientArea.Y + slot.Position.Y + (i * 56));
                slot.SetOutlinePosition(new Vector2D(clientArea.X + (int)(clientArea.Width / 2) - (int)(outline.Width / 2), clientArea.Y + (clientArea.Height / 2) - (outline.Height / 2)));
                i++;
            }
        }

        // Set up the handsgui reference so we can interact with it
        public void SetHandsGUI(HumanHandsGui _handsGUI)
        {
            handsGUI = _handsGUI;
        }

        public override void HandleNetworkMessage(Lidgren.Network.NetIncomingMessage message)
        {
            base.HandleNetworkMessage(message);
        }

        public override bool KeyDown(GorgonLibrary.InputDevices.KeyboardInputEventArgs e)
        {
            if (e.Key == KeyboardKeys.I)
            {
                ToggleVisible();
                return true;
            }
            return false;
        }

        [Obsolete("TODO: Change to new system")]
        public override bool MouseDown(GorgonLibrary.InputDevices.MouseInputEventArgs e)
        {
            Entity playerEntity = (Entity)playerController.controlledAtom;
            EquipmentComponent equipComponent = (EquipmentComponent)playerEntity.GetComponent(SS3D_shared.GO.ComponentFamily.Equipment);
            HumanHandsComponent humanHandComponent = (HumanHandsComponent)playerEntity.GetComponent(SS3D_shared.GO.ComponentFamily.Hands);
            //// Check which slot we clicked (if any) and get the atom from in there
            if (heldEntity == null)
            {
                heldEntity = handsGUI.GetActiveHandItem();
                lastSlot = EquipmentSlot.None;
            }
            foreach (GuiItemSlot slot in inventorySlots.Values)
            {
                if (slot.MouseDown(e))
                {
                    if (!AttemptEquipInSlot(playerEntity, slot))
                    {
                        if (equipComponent.equippedEntities.ContainsKey(slot.GetBodyPart()))
                        {
                            heldEntity = equipComponent.equippedEntities[slot.GetBodyPart()];
                            lastSlot = slot.GetBodyPart();
                        }
                    }
                    return true;
                }
            }

            if (handsGUI != null) // Otherwise see if we clicked on one of our hands and get that atom if so
            {
                if (handsGUI.MouseDown(e))
                {
                    if (humanHandComponent.HandSlots.ContainsKey(humanHandComponent.currentHand))
                    {
                        heldEntity = humanHandComponent.HandSlots[humanHandComponent.currentHand];
                        lastSlot = EquipmentSlot.None;
                        return true;
                    }
                }
            }
            return false;
        }

        [Obsolete("TODO: Change to new system")]
        public override bool MouseUp(GorgonLibrary.InputDevices.MouseInputEventArgs e)
        {
            if (heldEntity == null)
                return false;

            Entity playerEntity = (Entity)playerController.controlledAtom;
            EquipmentComponent ec = (EquipmentComponent)playerEntity.GetComponent(SS3D_shared.GO.ComponentFamily.Equipment);

            // Check which slot we released the mouse on, and equip the item there
            // (remembering to unequip it from wherever it came from)
            foreach (GuiItemSlot slot in inventorySlots.Values)
            {
                if (slot.MouseUp(e))
                {
                    if (lastSlot == EquipmentSlot.None)
                        ec.DispatchEquipFromHand();
                    else
                        ec.DispatchEquip(heldEntity.Uid);
                }
            }

            // If we dropped it on a hand we call Click which will equip it
            if (handsGUI != null)
            {
                if (handsGUI.MouseDown(e))
                {
                    if (lastSlot != EquipmentSlot.None) // It came from the inventory
                    {
                        ec.DispatchUnEquipToHand(heldEntity.Uid);
                    }
                }
            }

            heldEntity = null;
            return false;
        }

        public bool AttemptEquipInSlot(Entity m, GuiItemSlot slot)
        {
            if (slot.CanAccept(heldEntity))
            {
                if (lastSlot != EquipmentSlot.None) // It came from the inventory
                {
                    lastSlot = EquipmentSlot.None;
                }
                heldEntity = null;
                return true;
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            mousePos = e.Position;
        }

        public override void Render()
        {
            if (!IsVisible())
                return;

            Gorgon.Screen.FilledRectangle(clientArea.X + 1, clientArea.Y + 1, clientArea.Width - 2, clientArea.Height - 2, System.Drawing.Color.FromArgb(51, 56, 64));
            Gorgon.Screen.Rectangle(clientArea.X, clientArea.Y, clientArea.Width, clientArea.Height, Color.Black);

            if (inventorySlots.Count == 0 &&
                playerController.controlledAtom != null)
                SetUpSlots();

            outline.Position = new Vector2D(clientArea.X + (int)(clientArea.Width / 2) - (int)(outline.Width / 2), clientArea.Y + (clientArea.Height / 2) - (outline.Height / 2));
            outline.Draw();

            foreach (GuiItemSlot slot in inventorySlots.Values)
            {
                if (slot.CanAccept(heldEntity))
                    slot.Highlight();
                slot.Render();
            }

            if (heldEntity != null)
            {
                Sprite s = Utilities.GetSpriteComponentSprite(heldEntity);
                s.Position = mousePos;
                s.Draw();
            }
        }
    }

    
}
