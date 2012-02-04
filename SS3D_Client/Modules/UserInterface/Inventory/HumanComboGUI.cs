using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;
using SS13.HelperClasses;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS13.Modules;
using Lidgren.Network;
using CGO;
using SS13.Modules.Network;
using SS13_Shared.GO;
using SS13_Shared;
using ClientResourceManager;

namespace SS13.UserInterface
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
        private Dictionary<EquipmentSlot, EquipmentSlotUi> equipmentSlots = new Dictionary<EquipmentSlot, EquipmentSlotUi>();

        EquipmentSlotUi slot_head; 
        EquipmentSlotUi slot_eyes; 
        EquipmentSlotUi slot_outer; 
        EquipmentSlotUi slot_hands; 
        EquipmentSlotUi slot_feet; 

        EquipmentSlotUi slot_mask;
        EquipmentSlotUi slot_ears;
        EquipmentSlotUi slot_inner;
        EquipmentSlotUi slot_belt;
        EquipmentSlotUi slot_back;

        InventoryViewer inventory;
        #endregion

        #region Crafting UI
        private CraftSlotUi craftSlot1;
        private CraftSlotUi craftSlot2;
        private SimpleImageButton craftButton;
        private Timer_Bar craftTimer;
        private TextSprite craftStatus = new TextSprite("craftText", "Status", ResMgr.Singleton.GetFont("CALIBRI"));
        private ScrollableContainer blueprints;
        private int blueprintsOffset = 0;
        #endregion

        private byte currentTab = 1; //1 = Inventory, 2 = Health, 3 = Crafting

        private bool showTabbedWindow = false;

        TextSprite txtDbg = new TextSprite("comboDlgDbg", "Combo Debug", ResMgr.Singleton.GetFont("CALIBRI"));
        TextSprite healthText = new TextSprite("healthText", "", ResMgr.Singleton.GetFont("CALIBRI"));

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

        private NetworkManager netMgr;

        public HumanComboGUI(PlayerController _playerController, NetworkManager _netMgr)
            : base(_playerController)
        {
            netMgr = _netMgr;

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
            slot_head = new EquipmentSlotUi(EquipmentSlot.Head, _playerController);
            slot_head.Dropped += new EquipmentSlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_eyes = new EquipmentSlotUi(EquipmentSlot.Eyes, _playerController);
            slot_eyes.Dropped += new EquipmentSlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_outer = new EquipmentSlotUi(EquipmentSlot.Outer, _playerController);
            slot_outer.Dropped += new EquipmentSlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_hands = new EquipmentSlotUi(EquipmentSlot.Hands, _playerController);
            slot_hands.Dropped += new EquipmentSlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_feet = new EquipmentSlotUi(EquipmentSlot.Feet, _playerController);
            slot_feet.Dropped += new EquipmentSlotUi.InventorySlotUiDropHandler(slot_Dropped);

            //Right Side - mask, ears, inner, belt, back
            slot_mask = new EquipmentSlotUi(EquipmentSlot.Mask, _playerController);
            slot_mask.Dropped += new EquipmentSlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_ears = new EquipmentSlotUi(EquipmentSlot.Ears, _playerController);
            slot_ears.Dropped += new EquipmentSlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_inner = new EquipmentSlotUi(EquipmentSlot.Inner, _playerController);
            slot_inner.Dropped += new EquipmentSlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_belt = new EquipmentSlotUi(EquipmentSlot.Belt, _playerController);
            slot_belt.Dropped += new EquipmentSlotUi.InventorySlotUiDropHandler(slot_Dropped);

            slot_back = new EquipmentSlotUi(EquipmentSlot.Back, _playerController);
            slot_back.Dropped += new EquipmentSlotUi.InventorySlotUiDropHandler(slot_Dropped);

            craftSlot1 = new CraftSlotUi();
            craftSlot2 = new CraftSlotUi();

            craftButton = new SimpleImageButton("wrenchbutt");
            craftButton.Clicked += new SimpleImageButton.SimpleImageButtonPressHandler(craftButton_Clicked);

            craftStatus.ShadowColor = Color.DimGray;
            craftStatus.ShadowOffset = new Vector2D(1,1);
            craftStatus.Shadowed = true;

            blueprints = new ScrollableContainer("blueprintCont", new Size(210, 100));
        }

        void craftButton_Clicked(SimpleImageButton sender)
        {
            //craftTimer = new Timer_Bar(new Size(200,15), new TimeSpan(0,0,0,10));
            if (craftSlot1.containingEntity == null || craftSlot2.containingEntity == null) return;

            if (playerController != null)
                if (playerController.ControlledEntity != null)
                    if (playerController.ControlledEntity.HasComponent(ComponentFamily.Inventory))
                    {
                        InventoryComponent invComp = (InventoryComponent)playerController.ControlledEntity.GetComponent(ComponentFamily.Inventory);
                        if (invComp.containedEntities.Count >= invComp.maxSlots)
                        {
                            craftStatus.Text = "Status: Not enough Space";
                            craftStatus.Color = Color.DarkRed;
                            return;
                        }
                    }

            NetOutgoingMessage msg = netMgr.netClient.CreateMessage();
            msg.Write((byte)NetMessage.CraftMessage);
            msg.Write((byte)CraftMessage.StartCraft);
            msg.Write((int)craftSlot1.containingEntity.Uid);
            msg.Write((int)craftSlot2.containingEntity.Uid);
            netMgr.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
        }

        void slot_Dropped(EquipmentSlotUi sender, Entity dropped)
        {
            UiManager.Singleton.dragInfo.Reset();

            if (playerController.ControlledEntity == null)
                return;

            var entity = (Entity)playerController.ControlledEntity;

            EquipmentComponent equipment = (EquipmentComponent)entity.GetComponent(ComponentFamily.Equipment);

            equipment.DispatchEquip(dropped.Uid); //Serverside equip component will equip and remove from hands.
        }

        void tab_Clicked(SimpleImageButton sender)
        {
            if (sender == tab_equip) currentTab = 1;
            if (sender == tab_health) currentTab = 2;
            if (sender == tab_craft) currentTab = 3;
            craftStatus.Text = "Status"; //Handle the resetting better.
            craftStatus.Color = Color.White;
        }

        void combo_open_Clicked(SimpleImageButton sender)
        {
            showTabbedWindow = !showTabbedWindow;
            craftStatus.Text = "Status";
            craftStatus.Color = Color.White;
        }

        void combo_close_Clicked(SimpleImageButton sender)
        {
            showTabbedWindow = false;
            craftStatus.Text = "Status";
            craftStatus.Color = Color.White;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (e.Key == KeyboardKeys.I)
            {
                showTabbedWindow = !showTabbedWindow; 
                craftStatus.Text = "Status";
                craftStatus.Color = Color.White;
                return true;
            }
            return false;
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
                case ComboGuiMessage.CancelCraftBar:
                    if(craftTimer != null)
                    {
                        craftTimer.Dispose();
                        craftTimer = null;
                    }
                    craftStatus.Text = "Status: Canceled."; //Temp to debug.
                    craftStatus.Color = Color.DarkRed;
                    break;
                case ComboGuiMessage.ShowCraftBar:
                    int seconds = message.ReadInt32();
                    craftTimer = new Timer_Bar(new Size(210,15), new TimeSpan(0,0,0,seconds));
                    craftStatus.Text = "Status: Crafting...";
                    craftStatus.Color = Color.LightSteelBlue;
                    break;
                case ComboGuiMessage.CraftNoRecipe:
                    craftStatus.Text = "Status: There is no such Recipe.";
                    craftStatus.Color = Color.DarkRed;
                    break;
                case ComboGuiMessage.CraftNeedInventorySpace:
                    if(craftTimer != null)
                    {
                        craftTimer.Dispose();
                        craftTimer = null;
                    }
                    craftStatus.Text = "Status: Not enough Space";
                    craftStatus.Color = Color.DarkRed;
                    break;
                case ComboGuiMessage.CraftItemsMissing:
                    if (craftTimer != null)
                    {
                        craftTimer.Dispose();
                        craftTimer = null;
                    }
                    craftSlot1.ResetEntity();
                    craftSlot2.ResetEntity();
                    craftStatus.Text = "Status: Items missing.";
                    craftStatus.Color = Color.DarkRed;
                    break;
                case ComboGuiMessage.CraftSuccess:
                    if (craftTimer != null)
                    {
                        craftTimer.Dispose();
                        craftTimer = null;
                    }
                    craftSlot1.ResetEntity();
                    craftSlot2.ResetEntity();
                    addBlueprint(message);
                    break;
                case ComboGuiMessage.CraftAlreadyCrafting:
                    craftStatus.Text = "Status: You are already working on an item.";
                    craftStatus.Color = Color.DarkRed;
                    break;
            }
        }

        private void addBlueprint(NetIncomingMessage message)
        {
            string compo1Temp = message.ReadString();
            string compo1Name = message.ReadString();
            string compo2Temp = message.ReadString();
            string compo2Name = message.ReadString();
            string resultTemp = message.ReadString();
            string resultName = message.ReadString();

            craftStatus.Text = "Status: You successfully create '" + resultName + "'";
            craftStatus.Color = Color.Green;

            foreach (BlueprintButton bpbutt in blueprints.components)
            {
                List<string> req = new List<string>();
                req.Add(compo1Temp);
                req.Add(compo2Temp);
                if (req.Exists(x => x.ToLowerInvariant() == bpbutt.compo1.ToLowerInvariant())) req.Remove(req.First(x => x.ToLowerInvariant() == bpbutt.compo1.ToLowerInvariant()));
                if (req.Exists(x => x.ToLowerInvariant() == bpbutt.compo2.ToLowerInvariant())) req.Remove(req.First(x => x.ToLowerInvariant() == bpbutt.compo2.ToLowerInvariant()));
                if (!req.Any()) return;
            }

            BlueprintButton newBpb = new BlueprintButton(compo1Temp, compo1Name, compo2Temp, compo2Name, resultTemp, resultName);
            newBpb.Update();

            newBpb.Clicked += new BlueprintButton.BlueprintButtonPressHandler(blueprint_Clicked);

            newBpb.Position = new Point(0, blueprintsOffset);
            blueprintsOffset += newBpb.ClientArea.Height;

            blueprints.components.Add(newBpb);
        }

        void blueprint_Clicked(BlueprintButton sender)
        {
            //craftTimer = new Timer_Bar(new Size(200,15), new TimeSpan(0,0,0,10));
            if (playerController != null)
                if (playerController.ControlledEntity != null)
                    if (playerController.ControlledEntity.HasComponent(ComponentFamily.Inventory))
                    {
                        InventoryComponent invComp = (InventoryComponent)playerController.ControlledEntity.GetComponent(ComponentFamily.Inventory);
                        if (!invComp.containsEntity(sender.compo1) || !invComp.containsEntity(sender.compo2))
                        {
                            craftStatus.Text = "Status: You do not have the required items.";
                            craftStatus.Color = Color.DarkRed;
                            return;
                        }
                        else
                        {
                            craftSlot1.SetEntity(invComp.getEntity(sender.compo1));
                            craftSlot2.SetEntity(invComp.getEntity(sender.compo2));

                            craftButton_Clicked(null); //This is pretty dumb but i hate duplicate code.
                        }
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
                if (currentTab == 2) txtDbg.Text = "Status";
                if (currentTab == 3) txtDbg.Text = "Crafting";
                txtDbg.Draw();

                switch (currentTab)
                {
                    case (1): //Equip tab
                        {
                            #region Equip
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

                            if (inventory != null) inventory.Render();
                            break; 
                            #endregion
                        }
                    case (2): //Health tab
                        {
                            #region Status
                            healthText.Draw();
                            break; 
                            #endregion
                        }
                    case (3): //Craft tab
                        {
                            #region Crafting
                            if (craftTimer != null) craftTimer.Render();
                            if (inventory != null) inventory.Render();
                            craftSlot1.Render();
                            craftSlot2.Render();
                            craftButton.Render(); 
                            craftStatus.Draw();
                            blueprints.Render();
                            break;
                            #endregion
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
            if(inventory == null && playerController != null) //Gotta do this here because the vars are null in the constructor.
                if(playerController.ControlledEntity != null)
                    if (playerController.ControlledEntity.HasComponent(ComponentFamily.Inventory))
                    {
                        InventoryComponent invComp = (InventoryComponent)playerController.ControlledEntity.GetComponent(ComponentFamily.Inventory);
                        inventory = new InventoryViewer(invComp, playerController);
                    }

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

            switch (currentTab)
            {
                case (1): //Equip tab
                    {
                        #region Equip
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

                        if (inventory != null)
                        {
                            inventory.Position = new Point(position.X + 12, position.Y + 315);
                            inventory.Update();
                        }
                        break; 
                        #endregion
                    }
                case (2): //Health tab
                    {
                        #region Status
                        string healthStr = "";

                        if (playerController.ControlledEntity == null)
                            return;

                        var playerEnt = (Entity)playerController.ControlledEntity;
                        HumanHealthComponent healthcomp = (HumanHealthComponent)playerEnt.GetComponent(ComponentFamily.Damageable); //Unsafe cast. Might not be human health comp.

                        foreach (DamageLocation loc in healthcomp.damageZones)
                        {
                            healthStr += loc.location.ToString() + "   --   ";

                            float healthPct = (float)loc.currentHealth / (float)loc.maxHealth;

                            if (healthPct > 0.75) healthStr += "HEALTHY (" + loc.currentHealth.ToString() + " / " + loc.maxHealth.ToString() + ")" + Environment.NewLine;
                            else if (healthPct > 0.50) healthStr += "INJURED (" + loc.currentHealth.ToString() + " / " + loc.maxHealth.ToString() + ")" + Environment.NewLine;
                            else if (healthPct > 0.25) healthStr += "WOUNDED (" + loc.currentHealth.ToString() + " / " + loc.maxHealth.ToString() + ")" + Environment.NewLine;
                            else if (healthPct > 0) healthStr += "CRITICAL (" + loc.currentHealth.ToString() + " / " + loc.maxHealth.ToString() + ")" + Environment.NewLine;
                            else healthStr += "CRIPPLED (" + loc.currentHealth.ToString() + " / " + loc.maxHealth.ToString() + ")" + Environment.NewLine;
                        }

                        healthStr += Environment.NewLine + "Total Health : " + healthcomp.GetHealth().ToString() + " / " + healthcomp.GetMaxHealth().ToString();

                        healthText.Text = healthStr;
                        healthText.Color = Color.FloralWhite;
                        healthText.Position = new Vector2D(position.X + 40, position.Y + 40);

                        break; 
                        #endregion
                    }
                case (3): //Craft tab
                    {
                        #region Crafting
                        craftSlot1.Position = new Point(position.X + 40, position.Y + 80);
                        craftSlot1.Update();

                        craftSlot2.Position = new Point(position.X + clientArea.Width - craftSlot2.ClientArea.Width - 40, position.Y + 80);
                        craftSlot2.Update();

                        craftButton.Position = new Point(position.X + (int)(clientArea.Width / 2f) - (int)(craftButton.ClientArea.Width / 2f), position.Y + 70);
                        craftButton.Update();

                        if (craftTimer != null) craftTimer.Position = new Point(position.X + (int)(clientArea.Width / 2f) - (int)(craftTimer.ClientArea.Width / 2f), position.Y + 155);

                        craftStatus.Position = new Vector2D(position.X + (int)(clientArea.Width / 2f) - (int)(craftStatus.Width / 2f), position.Y + 40);

                        blueprints.Position = new Point(position.X + 40, position.Y + 180);
                        blueprints.Update();

                        if (inventory != null)
                        {
                            inventory.Position = new Point(position.X + 12, position.Y + 315);
                            inventory.Update();
                        }
                        break; 
                        #endregion
                    }   
            }

            if (craftTimer != null) craftTimer.Update(); //Needs to update even when its not on the crafting tab so it continues to count.

            #region Hands UI
            if (playerController.ControlledEntity == null)
                return;

            var entity = (Entity)playerController.ControlledEntity;
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
            if (playerController.ControlledEntity == null)
                return;

            var entity = (Entity)playerController.ControlledEntity;
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
            var entity = (Entity)playerController.ControlledEntity;
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
                        #region Equip
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

                        if (inventory != null) if (inventory.MouseDown(e)) return true;
                        break; 
                        #endregion
                    }
                case (2): //Health tab
                    {
                        #region Status
                        break; 
                        #endregion
                    }
                case (3): //Craft tab
                    {
                        #region Crafting
                        if (craftTimer != null) if (craftTimer.MouseDown(e)) return true;
                        if (craftSlot1.MouseDown(e)) return true;
                        if (craftSlot2.MouseDown(e)) return true;
                        if (craftButton.MouseDown(e)) return true;
                        if (blueprints.MouseDown(e)) return true;
                        if (inventory != null) if (inventory.MouseDown(e)) return true;

                        break; 
                        #endregion
                    }
            }

            return false;
        }

        private void SendSwitchHandTo(Hand hand)
        {
            Entity playerEntity = (Entity)playerController.ControlledEntity;
            HumanHandsComponent equipComponent = (HumanHandsComponent)playerEntity.GetComponent(ComponentFamily.Hands);
            equipComponent.SendSwitchHands(hand);
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            PointF mouseAABB = new PointF(e.Position.X, e.Position.Y);

            if (UiManager.Singleton.dragInfo.isEntity && UiManager.Singleton.dragInfo.dragEntity != null)
            {
                #region Hands
                if (playerController.ControlledEntity == null)
                    return false;

                var entity = (Entity)playerController.ControlledEntity;

                EquipmentComponent equipment = (EquipmentComponent)entity.GetComponent(ComponentFamily.Equipment);
                HumanHandsComponent hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);

                if (hands == null || entity == null) return false;

                if (Utilities.SpritePixelHit(hand_l_bg, e.Position))
                {
                    if (!hands.HandSlots.ContainsKey(Hand.Left))
                    {
                        if (hands.HandSlots.ContainsValue(UiManager.Singleton.dragInfo.dragEntity))
                        {
                            if (hands.HandSlots.First(x => x.Value == UiManager.Singleton.dragInfo.dragEntity).Key == Hand.Left) //From me to me, ignore.
                                return false;
                            else
                                hands.SendDropEntity(UiManager.Singleton.dragInfo.dragEntity); //Other hand to me.

                        }
                        equipment.DispatchUnEquipItemToSpecifiedHand(UiManager.Singleton.dragInfo.dragEntity.Uid, Hand.Left);
                        UiManager.Singleton.dragInfo.Reset();
                        return true;
                    }
                }

                if (Utilities.SpritePixelHit(hand_r_bg, e.Position))
                {
                    if (!hands.HandSlots.ContainsKey(Hand.Right))
                    {
                        if (hands.HandSlots.ContainsValue(UiManager.Singleton.dragInfo.dragEntity))
                        {
                            if (hands.HandSlots.First(x => x.Value == UiManager.Singleton.dragInfo.dragEntity).Key == Hand.Right) //From me to me, ignore.
                                return false;
                            else
                                hands.SendDropEntity(UiManager.Singleton.dragInfo.dragEntity); //Other hand to me.

                        }
                        equipment.DispatchUnEquipItemToSpecifiedHand(UiManager.Singleton.dragInfo.dragEntity.Uid, Hand.Right);
                        UiManager.Singleton.dragInfo.Reset();
                        return true;
                    }
                } 
                #endregion
            }

            switch (currentTab)
            {
                case (1): //Equip tab
                    {
                        #region Equip
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

                        if (inventory != null) if (inventory.MouseUp(e)) return true;

                        if (combo_BG.AABB.Contains(mouseAABB) && UiManager.Singleton.dragInfo.isEntity && UiManager.Singleton.dragInfo.dragEntity != null)
                        { //Should be refined to only trigger in the equip area. Equip it if they drop it anywhere on the thing. This might make the slots obsolete if we keep it.
                            if (playerController.ControlledEntity == null)
                                return false;

                            var entity = (Entity)playerController.ControlledEntity;

                            EquipmentComponent equipment = (EquipmentComponent)entity.GetComponent(ComponentFamily.Equipment);
                            equipment.DispatchEquip(UiManager.Singleton.dragInfo.dragEntity.Uid);
                            UiManager.Singleton.dragInfo.Reset();
                            return true;
                        }

                        break; 
                        #endregion
                    }
                case (2): //Health tab
                    {
                        #region Status
                        break; 
                        #endregion
                    }
                case (3): //Craft tab
                    {
                        #region Crafting
                        if (craftTimer != null) if (craftTimer.MouseUp(e)) return true;
                        if (craftSlot1.MouseUp(e))
                        {
                            if (craftSlot2.containingEntity == craftSlot1.containingEntity) craftSlot2.ResetEntity();
                            return true;
                        }
                        if (craftSlot2.MouseUp(e))
                        {
                            if (craftSlot1.containingEntity == craftSlot2.containingEntity) craftSlot1.ResetEntity();
                            return true;
                        }
                        if (craftButton.MouseUp(e)) return true;
                        if (blueprints.MouseUp(e)) return true;
                        if (inventory != null) if (inventory.MouseUp(e)) return true;
                        break; 
                        #endregion
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
                        #region Equip
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

                        if (inventory != null) inventory.MouseMove(e);
                        break; 
                        #endregion
                    }
                case (2): //Health tab
                    {
                        #region Status
                        break; 
                        #endregion
                    }
                case (3): //Craft tab
                    {
                        #region Crafting
                        if (craftTimer != null) craftTimer.MouseMove(e);
                        craftSlot1.MouseMove(e);
                        craftSlot2.MouseMove(e);
                        craftButton.MouseMove(e);
                        blueprints.MouseMove(e);
                        if (inventory != null) inventory.MouseMove(e);
                        break; 
                        #endregion
                    }
            }
        }
    }
}
