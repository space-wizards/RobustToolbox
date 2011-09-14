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

namespace SS3D.Modules.UI.Components
{
    public class HumanInventory : GuiComponent
    {
        private Dictionary<GUIBodyPart, ItemSlot> inventorySlots;
        private HumanHandsGui handsGUI;
        private WindowComponent window;
        private Atom.Atom heldAtom;
        private GUIBodyPart lastSlot = GUIBodyPart.None;
        private Vector2D mousePos;
        private Sprite outline;
        private int slotWidth;

        public HumanInventory(PlayerController _playerController)
            : base(_playerController)
        {
            componentClass = SS3D_shared.GuiComponentType.HumanInventory;
            inventorySlots = new Dictionary<GUIBodyPart, ItemSlot>();
            outline = ResMgr.Singleton.GetSprite("outline");
            slotWidth = (int)UIDesktop.Singleton.Skin.Elements["Window.InventorySlot"].Dimensions.Width;
            int width = 48 + slotWidth + (int)outline.Width + slotWidth;
            int height = 64 + (int)(outline.Height);
            clientArea = new Rectangle(Gorgon.Screen.Width - 25 - width, 600, width, height);
            mousePos = Vector2D.Zero;
            window = new WindowComponent(_playerController, clientArea.X, clientArea.Y, clientArea.Width, clientArea.Height);

            SetVisible(false);
        }

        // Set up all the slots for the body
        private void SetUpSlots()
        {
            inventorySlots.Clear();
            if (!playerController.controlledAtom.IsChildOfType(typeof(Atom.Mob.Mob)))
            {
                return;
            }

            Atom.Mob.Mob m = (Atom.Mob.Mob)playerController.controlledAtom;
            // Make one slot for each body part
            foreach (GUIBodyPart part in m.equippedAtoms.Keys)
            {
                inventorySlots.Add(part, new ItemSlot(playerController, part));
            }

            // Position them (just temporary atm)
            int i = 0;
            bool second = false;
            foreach (ItemSlot slot in inventorySlots.Values)
            {
                if (i >= inventorySlots.Count / 2)
                {
                    second = true;
                    i = 0;
                }
                if(!second)
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

        public override bool MouseDown(GorgonLibrary.InputDevices.MouseInputEventArgs e)
        {
            Atom.Mob.Mob m = (Atom.Mob.Mob)playerController.controlledAtom;
            // Check which slot we clicked (if any) and get the atom from in there
            if (heldAtom == null)
            {
                int i = handsGUI.GetSelectedAppendage();
                heldAtom = m.GetItemOnAppendage(i);
                lastSlot = GUIBodyPart.None;
            }
            foreach (ItemSlot slot in inventorySlots.Values)
            {
                if (slot.MouseDown(e))
                {
                    if (!AttemptEquipInSlot(m, slot))
                    {
                        heldAtom = m.GetEquippedAtom(slot.GetBodyPart());
                        lastSlot = slot.GetBodyPart();
                    }
                    return true;
                }
            }

            if (handsGUI != null) // Otherwise see if we clicked on one of our hands and get that atom if so
            {
                if (handsGUI.MouseDown(e))
                {
                    int i = handsGUI.GetSelectedAppendage();
                    heldAtom = m.GetItemOnAppendage(i);
                    lastSlot = GUIBodyPart.None;
                    return true;
                }
            }
            


            return false;
        }

        public override bool MouseUp(GorgonLibrary.InputDevices.MouseInputEventArgs e)
        {
            if (heldAtom == null)
                return false;
            Atom.Mob.Mob m = (Atom.Mob.Mob)playerController.controlledAtom;

            // Check which slot we released the mouse on, and equip the item there
            // (remembering to unequip it from wherever it came from)
            foreach (ItemSlot slot in inventorySlots.Values)
            {
                if (slot.MouseUp(e))
                {
                    AttemptEquipInSlot(m, slot);
                }
            }

            // If we dropped it on a hand we call Click which will equip it
            if (handsGUI != null)
            {
                if (handsGUI.MouseDown(e))
                {
                    if (lastSlot != GUIBodyPart.None) // It came from the inventory
                    {
                        m.SendUnequipItem(lastSlot);
                        heldAtom.SendClick();
                    }
                }
            }

            heldAtom = null;
            return false;
        }

        public bool AttemptEquipInSlot(Atom.Mob.Mob m, ItemSlot slot)
        {
            if (slot.CanAccept(heldAtom))
            {
                if (lastSlot != GUIBodyPart.None) // It came from the inventory
                {
                    m.SendUnequipItem(lastSlot);
                    lastSlot = GUIBodyPart.None;
                }
                m.SendEquipItem((Atom.Item.Item)heldAtom, slot.GetBodyPart());
                heldAtom = null;
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

            window.Render();

            if (inventorySlots.Count == 0 &&
                playerController.controlledAtom != null)
                SetUpSlots();

            outline.Position = new Vector2D(clientArea.X + (int)(clientArea.Width / 2) - (int)(outline.Width / 2), clientArea.Y + (clientArea.Height / 2) - (outline.Height / 2));
            outline.Draw();

            foreach (ItemSlot slot in inventorySlots.Values)
            {
                if (slot.CanAccept(heldAtom))
                    slot.Highlight();
                slot.Render();
            }

            if (heldAtom != null)
            {
                heldAtom.sprite.Position = mousePos;
                heldAtom.sprite.Draw();
            }
        }
    }

    
}
