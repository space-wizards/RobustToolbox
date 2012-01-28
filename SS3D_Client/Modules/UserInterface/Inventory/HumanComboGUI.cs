using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;
using SS3D.HelperClasses;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS3D.Modules;
using Lidgren.Network;
using CGO;
using SS3D_shared.GO;
using SS3D_shared;
using ClientResourceManager;

namespace SS3D.UserInterface
{
    public struct UiHandInfo
    {
        public Hand hand;
        public Entity entity;
        public Sprite heldSprite;
    }

    public class HumanComboGUI : GuiComponent
    {
        public override Point Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        #region Hand Slot UI
        public UiHandInfo leftHand = new UiHandInfo();
        public UiHandInfo rightHand = new UiHandInfo();

        public Hand activeHand { get; private set; } 
        #endregion

        #region Inventory UI
        private Dictionary<EquipmentSlot, GuiItemSlot> inventorySlots = new Dictionary<EquipmentSlot,GuiItemSlot>();

        InventorySlotUi slot_head; 
        InventorySlotUi slot_eyes; 
        InventorySlotUi slot_outer; 
        InventorySlotUi slot_hands; 
        InventorySlotUi slot_feet; 

        InventorySlotUi slot_mask;
        InventorySlotUi slot_ears;
        InventorySlotUi slot_inner;
        InventorySlotUi slot_belt;
        InventorySlotUi slot_back;
        #endregion

        private byte currentTab = 1; //1 = Inventory, 2 = Health, 3 = Crafting

        private bool showTabbedWindow = false;

        TextSprite txtDbg = new TextSprite("comboDlgDbg", "Combo Debug", ResMgr.Singleton.GetFont("CALIBRI"));

        Sprite combo_BG;
        SimpleImageButton combo_close;
        SimpleImageButton combo_open;

        SimpleImageButton tab_equip;
        SimpleImageButton tab_health;
        SimpleImageButton tab_craft;

        Sprite hand_l_bg;
        Sprite hand_r_bg;

        Sprite equip_BG;

        Color col_inactive = Color.FromArgb(255, 90, 90, 90);

        public HumanComboGUI(PlayerController _playerController)
            : base(_playerController)
        {
            componentClass = GuiComponentType.ComboGUI;

            leftHand.hand = Hand.Left;
            rightHand.hand = Hand.Right;

            equip_BG = ResMgr.Singleton.GetSprite("outline");

            combo_BG = ResMgr.Singleton.GetSprite("combo_bg");
            combo_close = new SimpleImageButton("button_closecombo");
            combo_open = new SimpleImageButton("button_inv");

            tab_equip = new SimpleImageButton("tab_equip");
            tab_equip.Clicked += new SimpleImageButton.SimpleImageButtonPressHandler(tab_Clicked);

            tab_health = new SimpleImageButton("tab_health");
            tab_health.Clicked += new SimpleImageButton.SimpleImageButtonPressHandler(tab_Clicked);

            tab_craft = new SimpleImageButton("tab_craft");
            tab_craft.Clicked += new SimpleImageButton.SimpleImageButtonPressHandler(tab_Clicked);

            hand_l_bg = ResMgr.Singleton.GetSprite("hand_l");
            hand_r_bg = ResMgr.Singleton.GetSprite("hand_r");

            combo_close.Clicked += new SimpleImageButton.SimpleImageButtonPressHandler(combo_close_Clicked);
            combo_open.Clicked += new SimpleImageButton.SimpleImageButtonPressHandler(combo_open_Clicked);

            //Left Side - head, eyes, outer, hands, feet
            slot_head = new InventorySlotUi(EquipmentSlot.Head, _playerController);
            slot_head.Dropped += new InventorySlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_eyes = new InventorySlotUi(EquipmentSlot.Eyes, _playerController);
            slot_eyes.Dropped += new InventorySlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_outer = new InventorySlotUi(EquipmentSlot.Outer, _playerController);
            slot_outer.Dropped += new InventorySlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_hands = new InventorySlotUi(EquipmentSlot.Hands, _playerController);
            slot_hands.Dropped += new InventorySlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_feet = new InventorySlotUi(EquipmentSlot.Feet, _playerController);
            slot_feet.Dropped += new InventorySlotUi.InventorySlotUiDropHandler(slot_Dropped);


            //Right Side - mask, ears, inner, belt, back
            slot_mask = new InventorySlotUi(EquipmentSlot.Mask, _playerController);
            slot_mask.Dropped += new InventorySlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_ears = new InventorySlotUi(EquipmentSlot.Ears, _playerController);
            slot_ears.Dropped += new InventorySlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_inner = new InventorySlotUi(EquipmentSlot.Inner, _playerController);
            slot_inner.Dropped += new InventorySlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_belt = new InventorySlotUi(EquipmentSlot.Belt, _playerController);
            slot_belt.Dropped += new InventorySlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_back = new InventorySlotUi(EquipmentSlot.Back, _playerController);
            slot_back.Dropped += new InventorySlotUi.InventorySlotUiDropHandler(slot_Dropped);

        }

        void slot_Dropped(InventorySlotUi sender, Entity dropped)
        {
            UiManager.Singleton.dragInfo.Reset();

            if (sender.currentEntity == dropped) return; //Dropped from us to us.

            if (playerController.controlledAtom == null)
                return;

            var entity = (Entity)playerController.controlledAtom;

            EquipmentComponent equipment = (EquipmentComponent)entity.GetComponent(ComponentFamily.Equipment);
            HumanHandsComponent hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);

            if (hands == null || entity == null) return;

            if (hands.HandSlots.ContainsValue(dropped)) //Comes from one of our hands. 
            {                //THIS NEEDS BETTER HANDLING. SERVER SHOULD AUTOMATICALLY REMOVE OBJECTS FROM HANDS WHEN EQUIPPED (So we can just equip them here without worrying about hands).//BZZZ
                Hand containingHand = hands.HandSlots.First(x => x.Value == dropped).Key;

                if (containingHand != hands.currentHand)
                    SendSwitchHandTo(containingHand);

                equipment.DispatchEquipFromHand();
            }
            else //Comes from somewhere else. Not sure what somewhere could be. Maybe another slot? If we have items that can go in diff slots? Need to remember to unequip it from that something before equipping. Unless server does. See above.
            {
                equipment.DispatchEquipToPart(dropped.Uid, sender.assignedSlot);
            }

            //This will autmatically equip items to their proper slot if dropped on a wrong one. Server does that. Oh well.
        }

        void tab_Clicked(SimpleImageButton sender)
        {
            if (sender == tab_equip) currentTab = 1;
            if (sender == tab_health) currentTab = 2;
            if (sender == tab_craft) currentTab = 3;
        }

        void combo_open_Clicked(SimpleImageButton sender)
        {
            showTabbedWindow = !showTabbedWindow;
        }

        void combo_close_Clicked(SimpleImageButton sender)
        {
            showTabbedWindow = false;
        }

        public override void ComponentUpdate(params object[] args)
        {
            base.ComponentUpdate(args);

            ComboGuiMessage messageType = (ComboGuiMessage)args[0]; 

            switch (messageType)
            {
                case ComboGuiMessage.UpdateHands:
                    UpdateHandIcons();
                    break;
                default: 
                    break;
            }
        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
            ComboGuiMessage messageType = (ComboGuiMessage)message.ReadByte();

            switch (messageType)
            {
                default: 
                    break;
            }
        }

        public override void Render()
        {
            if (showTabbedWindow)
            {
                combo_BG.Draw();
                combo_close.Render();
                tab_health.Render();
                tab_equip.Render();
                tab_craft.Render();

                txtDbg.Position = new Vector2D(position.X + 20, position.Y + 15);
                txtDbg.Color = Color.NavajoWhite;
                if (currentTab == 1) txtDbg.Text = "Equipment";
                if (currentTab == 2) txtDbg.Text = "Health";
                if (currentTab == 3) txtDbg.Text = "Crafting";
                txtDbg.Draw();

                switch (currentTab)
                {
                    case (1): //Equip tab
                        {
                            equip_BG.Draw();

                            //Left Side - head, eyes, outer, hands, feet
                            slot_head.Render();
                            slot_eyes.Render();
                            slot_outer.Render();
                            slot_hands.Render();
                            slot_feet.Render();

                            //Right Side - mask, ears, inner, belt, back
                            slot_mask.Render();
                            slot_ears.Render();
                            slot_inner.Render();
                            slot_belt.Render();
                            slot_back.Render();
                            break;
                        }
                    case (2): //Health tab
                        {
                            break;
                        }
                    case (3): //Craft tab
                        {
                            break;
                        }
                }
            }
            combo_open.Render();

            hand_l_bg.Draw();
            if (leftHand.entity != null && leftHand.heldSprite != null) 
                leftHand.heldSprite.Draw(new Rectangle((int)hand_l_bg.Position.X + (int)(hand_l_bg.AABB.Width / 4f - leftHand.heldSprite.AABB.Width / 2f), (int)hand_l_bg.Position.Y + (int)(hand_l_bg.AABB.Height / 2f - leftHand.heldSprite.AABB.Height / 2f), (int)leftHand.heldSprite.AABB.Width, (int)leftHand.heldSprite.AABB.Height));

            hand_r_bg.Draw(); //Change to something more sane.
            if (rightHand.entity != null && rightHand.heldSprite != null) 
                rightHand.heldSprite.Draw(new Rectangle((int)hand_r_bg.Position.X + (int)((hand_r_bg.AABB.Width / 4f) * 3 - rightHand.heldSprite.AABB.Width / 2f), (int)hand_r_bg.Position.Y + (int)(hand_r_bg.AABB.Height / 2f - rightHand.heldSprite.AABB.Height / 2f), (int)rightHand.heldSprite.AABB.Width, (int)rightHand.heldSprite.AABB.Height));

        }

        public override void Update()
        {
            combo_BG.Position = position;

            Point equip_bg_pos = position;
            equip_BG.Position = position;
            equip_bg_pos.Offset((int)(combo_BG.AABB.Width / 2f - equip_BG.AABB.Width / 2f), 40);
            equip_BG.Position = equip_bg_pos;

            Point combo_open_pos = position;
            combo_open_pos.Offset((int)(combo_BG.Width - combo_open.ClientArea.Width), (int)combo_BG.Height - 1);
            combo_open.Position = combo_open_pos;
            combo_open.Update();

            Point combo_close_pos = position;
            combo_close_pos.Offset(264, 11); //Magic photoshop ruler numbers.
            combo_close.Position = combo_close_pos;
            combo_close.Update();

            Point tab_equip_pos = position;
            tab_equip_pos.Offset(-26 , 76); //Magic photoshop ruler numbers.
            tab_equip.Position = tab_equip_pos;
            tab_equip.Color = currentTab == 1 ? Color.White : col_inactive;
            tab_equip.Update();

            Point tab_health_pos = tab_equip_pos;
            tab_health_pos.Offset(0, 3 + tab_equip.ClientArea.Height);
            tab_health.Position = tab_health_pos;
            tab_health.Color = currentTab == 2 ? Color.White : col_inactive;
            tab_health.Update();
            
            Point tab_craft_pos = tab_health_pos;
            tab_craft_pos.Offset(0, 3 + tab_health.ClientArea.Height);
            tab_craft.Position = tab_craft_pos;
            tab_craft.Color = currentTab == 3 ? Color.White : col_inactive;
            tab_craft.Update();

            Point hands_pos = position;
            hands_pos.Offset(1, (int)combo_BG.Height);
            hand_l_bg.Position = hands_pos;
            hand_r_bg.Position = hands_pos;

            this.ClientArea = new Rectangle((int)position.X, (int)position.Y, (int)combo_BG.AABB.Width, (int)combo_BG.AABB.Height + (int)combo_open.ClientArea.Height);

            //Only set position for topmost 2 slots directly. Rest uses these to position themselves.
            Point slot_left_start = position;
            slot_left_start.Offset(28, 40);
            slot_head.Position = slot_left_start;
            slot_head.Update();

            Point slot_right_start = position;
            slot_right_start.Offset((int)(combo_BG.AABB.Width - slot_mask.ClientArea.Width - 28), 40);
            slot_mask.Position = slot_right_start;
            slot_mask.Update();

            int vert_spacing = 6 + slot_head.ClientArea.Height;

            //Left Side - head, eyes, outer, hands, feet
            slot_left_start.Offset(0, vert_spacing);
            slot_eyes.Position = slot_left_start;
            slot_eyes.Update();

            slot_left_start.Offset(0, vert_spacing);
            slot_outer.Position = slot_left_start;
            slot_outer.Update();

            slot_left_start.Offset(0, vert_spacing);
            slot_hands.Position = slot_left_start;
            slot_hands.Update();

            slot_left_start.Offset(0, vert_spacing);
            slot_feet.Position = slot_left_start;
            slot_feet.Update();

            //Right Side - mask, ears, inner, belt, back
            slot_right_start.Offset(0, vert_spacing);
            slot_ears.Position = slot_right_start;
            slot_ears.Update();

            slot_right_start.Offset(0, vert_spacing);
            slot_inner.Position = slot_right_start;
            slot_inner.Update();

            slot_right_start.Offset(0, vert_spacing);
            slot_belt.Position = slot_right_start;
            slot_belt.Update();

            slot_right_start.Offset(0, vert_spacing);
            slot_back.Position = slot_right_start;
            slot_back.Update();

            #region Hands UI
            if (playerController.controlledAtom == null)
                return;

            var entity = (Entity)playerController.controlledAtom;
            HumanHandsComponent hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);

            if (hands.currentHand == Hand.Left)
            {
                hand_l_bg.Color = Color.White;
                hand_r_bg.Color = col_inactive;
            }
            else
            {
                hand_r_bg.Color = Color.White;
                hand_l_bg.Color = col_inactive;
            } 
            #endregion
        }

        public override void Dispose()
        {
        }

        public void UpdateHandIcons()
        {
            if (playerController.controlledAtom == null)
                return;

            var entity = (Entity)playerController.controlledAtom;
            HumanHandsComponent hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);

            if (hands == null) return;

            if (hands.HandSlots.Keys.Contains(Hand.Left))
            {
                Entity EntityL = hands.HandSlots[Hand.Left];
                leftHand.entity = EntityL;
                leftHand.heldSprite = Utilities.GetSpriteComponentSprite(EntityL);
            }
            else
            {
                leftHand.entity = null;
                leftHand.heldSprite = null;
            }

            if (hands.HandSlots.Keys.Contains(Hand.Right))
            {
                Entity EntityR = hands.HandSlots[Hand.Right];
                rightHand.entity = EntityR;
                rightHand.heldSprite = Utilities.GetSpriteComponentSprite(EntityR);
            }
            else
            {
                rightHand.entity = null;
                rightHand.heldSprite = null;
            }
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            RectangleF mouseAABB = new RectangleF(e.Position.X, e.Position.Y, 1, 1);
            if (combo_open.MouseDown(e)) return true;
            if (showTabbedWindow)
            {
                if (combo_close.MouseDown(e)) return true;
                if (tab_equip.MouseDown(e)) return true;
                if (tab_health.MouseDown(e)) return true;
                if (tab_craft.MouseDown(e)) return true;
            }

            #region Hands UI, Switching
            var entity = (Entity)playerController.controlledAtom;
            HumanHandsComponent hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);
            if (e.Buttons == MouseButtons.Right)
            {
                if (Utilities.SpritePixelHit(hand_l_bg, e.Position))
                {
                    SendSwitchHandTo(Hand.Left);
                    return true;
                }

                if (Utilities.SpritePixelHit(hand_r_bg, e.Position))
                {
                    SendSwitchHandTo(Hand.Right);
                    return true;
                }
            }
            else if (e.Buttons == MouseButtons.Left)
            {
                if (Utilities.SpritePixelHit(hand_l_bg, e.Position))
                {
                    if (hands.HandSlots.Keys.Contains(Hand.Left))
                    {
                        Entity EntityL = hands.HandSlots[Hand.Left];
                        UiManager.Singleton.dragInfo.StartDrag(EntityL);
                    }
                    return true;
                }

                if (Utilities.SpritePixelHit(hand_r_bg, e.Position))
                {
                    if (hands.HandSlots.Keys.Contains(Hand.Right))
                    {
                        Entity EntityR = hands.HandSlots[Hand.Right];
                        UiManager.Singleton.dragInfo.StartDrag(EntityR);
                    }
                    return true;
                }
            }
            #endregion

            switch (currentTab)
            {
                case (1): //Equip tab
                    {
                        //Left Side - head, eyes, outer, hands, feet
                        if (slot_head.MouseDown(e)) return true;
                        if (slot_eyes.MouseDown(e)) return true;
                        if (slot_outer.MouseDown(e)) return true;
                        if (slot_hands.MouseDown(e)) return true;
                        if (slot_feet.MouseDown(e)) return true;

                        //Right Side - mask, ears, inner, belt, back
                        if (slot_mask.MouseDown(e)) return true;
                        if (slot_ears.MouseDown(e)) return true;
                        if (slot_inner.MouseDown(e)) return true;
                        if (slot_belt.MouseDown(e)) return true;
                        if (slot_back.MouseDown(e)) return true;
                        break;
                    }
                case (2): //Health tab
                    {
                        break;
                    }
                case (3): //Craft tab
                    {
                        break;
                    }
            }

            return false;
        }

        private void SendSwitchHandTo(Hand hand)
        {
            Entity playerEntity = (Entity)playerController.controlledAtom;
            HumanHandsComponent equipComponent = (HumanHandsComponent)playerEntity.GetComponent(ComponentFamily.Hands);
            equipComponent.SendSwitchHands(hand);
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            PointF mouseAABB = new PointF(e.Position.X, e.Position.Y);

            if (UiManager.Singleton.dragInfo.isEntity && UiManager.Singleton.dragInfo.dragEntity != null)
            {
                if (playerController.controlledAtom == null)
                    return false;

                var entity = (Entity)playerController.controlledAtom;

                EquipmentComponent equipment = (EquipmentComponent)entity.GetComponent(ComponentFamily.Equipment);
                HumanHandsComponent hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);

                if (hands == null || entity == null) return false;

                if (Utilities.SpritePixelHit(hand_l_bg, e.Position)) //Needs better handling, Server should automatically unequip items from equip component when equiped to hand. //BZZZ
                {
                    if (!hands.HandSlots.ContainsKey(Hand.Left)) //Is it empty? Wont contain it if the slot is empty.
                    {
                        if (hands.currentHand != Hand.Left) SendSwitchHandTo(Hand.Left);
                        equipment.DispatchUnEquipToHand(UiManager.Singleton.dragInfo.dragEntity.Uid);
                        UiManager.Singleton.dragInfo.Reset();
                        return true;
                    }
                }

                if (Utilities.SpritePixelHit(hand_r_bg, e.Position))
                {
                    if (!hands.HandSlots.ContainsKey(Hand.Right))
                    {
                        if (hands.currentHand != Hand.Right) SendSwitchHandTo(Hand.Right);
                        equipment.DispatchUnEquipToHand(UiManager.Singleton.dragInfo.dragEntity.Uid);
                        UiManager.Singleton.dragInfo.Reset();
                        return true;
                    }
                }
            }

            switch (currentTab)
            {
                case (1): //Equip tab
                    {
                        //Left Side - head, eyes, outer, hands, feet
                        if (slot_head.MouseUp(e)) return true;
                        if (slot_eyes.MouseUp(e)) return true;
                        if (slot_outer.MouseUp(e)) return true;
                        if (slot_hands.MouseUp(e)) return true;
                        if (slot_feet.MouseUp(e)) return true;

                        //Right Side - mask, ears, inner, belt, back
                        if (slot_mask.MouseUp(e)) return true;
                        if (slot_ears.MouseUp(e)) return true;
                        if (slot_inner.MouseUp(e)) return true;
                        if (slot_belt.MouseUp(e)) return true;
                        if (slot_back.MouseUp(e)) return true;
                        break;
                    }
                case (2): //Health tab
                    {
                        break;
                    }
                case (3): //Craft tab
                    {
                        break;
                    }
            }

            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {      
            switch (currentTab)
            {
                case (1): //Equip tab
                    {
                        //Left Side - head, eyes, outer, hands, feet
                        slot_head.MouseMove(e);
                        slot_eyes.MouseMove(e);
                        slot_outer.MouseMove(e);
                        slot_hands.MouseMove(e);
                        slot_feet.MouseMove(e);

                        //Right Side - mask, ears, inner, belt, back
                        slot_mask.MouseMove(e);
                        slot_ears.MouseMove(e);
                        slot_inner.MouseMove(e);
                        slot_belt.MouseMove(e);
                        slot_back.MouseMove(e);
                        break;
                    }
                case (2): //Health tab
                    {
                        break;
                    }
                case (3): //Craft tab
                    {
                        break;
                    }
            }
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (e.Key == KeyboardKeys.I)
            {
                showTabbedWindow = !showTabbedWindow;
                return true;
            }
            return false;
        }
    }
}
