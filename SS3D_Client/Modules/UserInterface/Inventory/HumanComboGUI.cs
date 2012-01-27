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

        Color col_inactive = Color.FromArgb(255, 90, 90, 90);

        public HumanComboGUI(PlayerController _playerController)
            : base(_playerController)
        {
            componentClass = GuiComponentType.ComboGUI;

            leftHand.hand = Hand.Left;
            rightHand.hand = Hand.Right;

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

            if (playerController.controlledAtom == null)
                return;

            #region Hands UI
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

            #endregion


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
            RectangleF mouseAABB = new RectangleF(e.Position.X, e.Position.Y, 1, 1);
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
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
