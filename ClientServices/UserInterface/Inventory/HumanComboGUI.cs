using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using CGO.Component.Damageable.Health.LocationalHealth;
using CGO.Component.Hands;
using ClientInterfaces;
using ClientInterfaces.GOC;
using ClientInterfaces.Network;
using ClientInterfaces.Player;
using ClientInterfaces.UserInterface;
using ClientServices.Helpers;
using ClientServices.UserInterface.Components;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using CGO;
using SS13_Shared.GO;
using SS13_Shared;

namespace ClientServices.UserInterface.Inventory
{
    public struct UiHandInfo
    {
        public Hand Hand;
        public IEntity Entity;
        public Sprite HeldSprite;
    }

    public class HumanComboGui : GuiComponent
    {
        #region Hand Slot UI
        public UiHandInfo LeftHand;
        public UiHandInfo RightHand;
        #endregion

        #region Inventory UI
        private Dictionary<EquipmentSlot, EquipmentSlotUi> _equipmentSlots = new Dictionary<EquipmentSlot, EquipmentSlotUi>();

        private readonly EquipmentSlotUi _slotHead;
        private readonly EquipmentSlotUi _slotEyes;
        private readonly EquipmentSlotUi _slotOuter;
        private readonly EquipmentSlotUi _slotHands;
        private readonly EquipmentSlotUi _slotFeet;

        private readonly EquipmentSlotUi _slotMask;
        private readonly EquipmentSlotUi _slotEars;
        private readonly EquipmentSlotUi _slotInner;
        private readonly EquipmentSlotUi _slotBelt;
        private readonly EquipmentSlotUi _slotBack;

        private InventoryViewer _inventory;
        #endregion

        #region Crafting UI
        private readonly CraftSlotUi _craftSlot1;
        private readonly CraftSlotUi _craftSlot2;
        private readonly SimpleImageButton _craftButton;
        private Timer_Bar _craftTimer;
        private readonly TextSprite _craftStatus;
        private readonly ScrollableContainer _blueprints;
        private int _blueprintsOffset;
        #endregion

        private byte _currentTab = 1; //1 = Inventory, 2 = Health, 3 = Crafting
        private bool _showTabbedWindow;

        private readonly TextSprite _txtDbg;
        private readonly TextSprite _healthText;
        private readonly Sprite _comboBg;
        private readonly SimpleImageButton _comboClose;
        private readonly SimpleImageButton _comboOpen;
        private readonly SimpleImageButton _tabEquip;
        private readonly SimpleImageButton _tabHealth;
        private readonly SimpleImageButton _tabCraft;
        private readonly Sprite _handLBg;
        private readonly Sprite _handRBg;
        private readonly Sprite _equipBg;

        private readonly Color _inactiveColor = Color.FromArgb(255, 90, 90, 90);

        private readonly INetworkManager _networkManager;
        private readonly IPlayerManager _playerManager;
        private readonly IResourceManager _resourceManager;
        private readonly IUserInterfaceManager _userInterfaceManager;

        public HumanComboGui(IPlayerManager playerManager, INetworkManager networkManager, IResourceManager resourceManager, IUserInterfaceManager userInterfaceManager)
        {
            _networkManager = networkManager;
            _playerManager = playerManager;
            _resourceManager = resourceManager;
            _userInterfaceManager = userInterfaceManager;

            ComponentClass = GuiComponentType.ComboGui;

            LeftHand.Hand = Hand.Left;
            RightHand.Hand = Hand.Right;

            _equipBg = _resourceManager.GetSprite("outline");

            _comboBg = _resourceManager.GetSprite("combo_bg");
            _comboClose = new SimpleImageButton("button_closecombo", _resourceManager);
            _comboOpen = new SimpleImageButton("button_inv", _resourceManager);

            _tabEquip = new SimpleImageButton("tab_equip", _resourceManager);
            _tabEquip.Clicked += TabClicked;

            _healthText = new TextSprite("healthText", "", _resourceManager.GetFont("CALIBRI"));
            _tabHealth = new SimpleImageButton("tab_health", _resourceManager);
            _tabHealth.Clicked += TabClicked;

            _tabCraft = new SimpleImageButton("tab_craft", _resourceManager);
            _tabCraft.Clicked += TabClicked;

            _handLBg = _resourceManager.GetSprite("hand_l");
            _handRBg = _resourceManager.GetSprite("hand_r");

            _comboClose.Clicked += ComboCloseClicked;
            _comboOpen.Clicked += ComboOpenClicked;

            //Left Side - head, eyes, outer, hands, feet
            _slotHead = new EquipmentSlotUi(EquipmentSlot.Head, _playerManager, _resourceManager,_userInterfaceManager);
            _slotHead.Dropped += SlotDropped;

            _slotEyes = new EquipmentSlotUi(EquipmentSlot.Eyes, _playerManager, _resourceManager, _userInterfaceManager);
            _slotEyes.Dropped += SlotDropped;

            _slotOuter = new EquipmentSlotUi(EquipmentSlot.Outer, _playerManager, _resourceManager, _userInterfaceManager);
            _slotOuter.Dropped += SlotDropped;

            _slotHands = new EquipmentSlotUi(EquipmentSlot.Hands, _playerManager, _resourceManager, _userInterfaceManager);
            _slotHands.Dropped += SlotDropped;

            _slotFeet = new EquipmentSlotUi(EquipmentSlot.Feet, _playerManager, _resourceManager, _userInterfaceManager);
            _slotFeet.Dropped += SlotDropped;

            //Right Side - mask, ears, inner, belt, back
            _slotMask = new EquipmentSlotUi(EquipmentSlot.Mask, _playerManager, _resourceManager, _userInterfaceManager);
            _slotMask.Dropped += SlotDropped;

            _slotEars = new EquipmentSlotUi(EquipmentSlot.Ears, _playerManager, _resourceManager, _userInterfaceManager);
            _slotEars.Dropped += SlotDropped;

            _slotInner = new EquipmentSlotUi(EquipmentSlot.Inner, _playerManager, _resourceManager, _userInterfaceManager);
            _slotInner.Dropped += SlotDropped;

            _slotBelt = new EquipmentSlotUi(EquipmentSlot.Belt, _playerManager, _resourceManager, _userInterfaceManager);
            _slotBelt.Dropped += SlotDropped;

            _slotBack = new EquipmentSlotUi(EquipmentSlot.Back, _playerManager, _resourceManager, _userInterfaceManager);
            _slotBack.Dropped += SlotDropped;

            _txtDbg = new TextSprite("comboDlgDbg", "Combo Debug", _resourceManager.GetFont("CALIBRI"));

            _craftSlot1 = new CraftSlotUi(_resourceManager, _userInterfaceManager);
            _craftSlot2 = new CraftSlotUi(_resourceManager, _userInterfaceManager);

            _craftButton = new SimpleImageButton("wrenchbutt", _resourceManager);
            _craftButton.Clicked += CraftButtonClicked;

            _craftStatus = new TextSprite("craftText", "Status", _resourceManager.GetFont("CALIBRI"))
                               {
                                   ShadowColor = Color.DimGray,
                                   ShadowOffset = new Vector2D(1, 1),
                                   Shadowed = true
                               };

            _blueprints = new ScrollableContainer("blueprintCont", new Size(210, 100), _resourceManager);
        }

        void CraftButtonClicked(SimpleImageButton sender)
        {
            //craftTimer = new Timer_Bar(new Size(200,15), new TimeSpan(0,0,0,10));
            if (_craftSlot1.ContainingEntity == null || _craftSlot2.ContainingEntity == null) return;

            if (_playerManager != null)
                if (_playerManager.ControlledEntity != null)
                    if (_playerManager.ControlledEntity.HasComponent(ComponentFamily.Inventory))
                    {
                        var invComp = (InventoryComponent)_playerManager.ControlledEntity.GetComponent(ComponentFamily.Inventory);
                        if (invComp.containedEntities.Count >= invComp.maxSlots)
                        {
                            _craftStatus.Text = "Status: Not enough Space";
                            _craftStatus.Color = Color.DarkRed;
                            return;
                        }
                    }

            var msg = _networkManager.CreateMessage();
            msg.Write((byte)NetMessage.CraftMessage);
            msg.Write((byte)CraftMessage.StartCraft);
            msg.Write(_craftSlot1.ContainingEntity.Uid);
            msg.Write(_craftSlot2.ContainingEntity.Uid);
            _networkManager.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
        }

        void SlotDropped(EquipmentSlotUi sender, IEntity dropped)
        {
            _userInterfaceManager.DragInfo.Reset();

            if (_playerManager.ControlledEntity == null)
                return;

            var entity = _playerManager.ControlledEntity;

            var equipment = (EquipmentComponent)entity.GetComponent(ComponentFamily.Equipment);

            equipment.DispatchEquip(dropped.Uid); //Serverside equip component will equip and remove from hands.
        }

        void TabClicked(SimpleImageButton sender)
        {
            if (sender == _tabEquip) _currentTab = 1;
            if (sender == _tabHealth) _currentTab = 2;
            if (sender == _tabCraft) _currentTab = 3;
            _craftStatus.Text = "Status"; //Handle the resetting better.
            _craftStatus.Color = Color.White;
        }

        void ComboOpenClicked(SimpleImageButton sender)
        {
            _showTabbedWindow = !_showTabbedWindow;
            _craftStatus.Text = "Status";
            _craftStatus.Color = Color.White;
        }

        void ComboCloseClicked(SimpleImageButton sender)
        {
            _showTabbedWindow = false;
            _craftStatus.Text = "Status";
            _craftStatus.Color = Color.White;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (e.Key == KeyboardKeys.I)
            {
                _showTabbedWindow = !_showTabbedWindow; 
                _craftStatus.Text = "Status";
                _craftStatus.Color = Color.White;
                return true;
            }
            return false;
        }

        public override void ComponentUpdate(params object[] args)
        {
            base.ComponentUpdate(args);

            var messageType = (ComboGuiMessage)args[0]; 

            switch (messageType)
            {
                case ComboGuiMessage.UpdateHands:
                    UpdateHandIcons();
                    break;
            }
        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
            var messageType = (ComboGuiMessage)message.ReadByte();

            switch (messageType)
            {
                case ComboGuiMessage.CancelCraftBar:
                    if(_craftTimer != null)
                    {
                        _craftTimer.Dispose();
                        _craftTimer = null;
                    }
                    _craftStatus.Text = "Status: Canceled."; //Temp to debug.
                    _craftStatus.Color = Color.DarkRed;
                    break;
                case ComboGuiMessage.ShowCraftBar:
                    int seconds = message.ReadInt32();
                    _craftTimer = new Timer_Bar(new Size(210, 15), new TimeSpan(0, 0, 0, seconds), _resourceManager);
                    _craftStatus.Text = "Status: Crafting...";
                    _craftStatus.Color = Color.LightSteelBlue;
                    break;
                case ComboGuiMessage.CraftNoRecipe:
                    _craftStatus.Text = "Status: There is no such Recipe.";
                    _craftStatus.Color = Color.DarkRed;
                    break;
                case ComboGuiMessage.CraftNeedInventorySpace:
                    if(_craftTimer != null)
                    {
                        _craftTimer.Dispose();
                        _craftTimer = null;
                    }
                    _craftStatus.Text = "Status: Not enough Space";
                    _craftStatus.Color = Color.DarkRed;
                    break;
                case ComboGuiMessage.CraftItemsMissing:
                    if (_craftTimer != null)
                    {
                        _craftTimer.Dispose();
                        _craftTimer = null;
                    }
                    _craftSlot1.ResetEntity();
                    _craftSlot2.ResetEntity();
                    _craftStatus.Text = "Status: Items missing.";
                    _craftStatus.Color = Color.DarkRed;
                    break;
                case ComboGuiMessage.CraftSuccess:
                    if (_craftTimer != null)
                    {
                        _craftTimer.Dispose();
                        _craftTimer = null;
                    }
                    _craftSlot1.ResetEntity();
                    _craftSlot2.ResetEntity();
                    AddBlueprint(message);
                    break;
                case ComboGuiMessage.CraftAlreadyCrafting:
                    _craftStatus.Text = "Status: You are already working on an item.";
                    _craftStatus.Color = Color.DarkRed;
                    break;
            }
        }

        private void AddBlueprint(NetIncomingMessage message)
        {
            var compo1Temp = message.ReadString();
            var compo1Name = message.ReadString();
            var compo2Temp = message.ReadString();
            var compo2Name = message.ReadString();
            var resultTemp = message.ReadString();
            var resultName = message.ReadString();

            _craftStatus.Text = "Status: You successfully create '" + resultName + "'";
            _craftStatus.Color = Color.Green;

            foreach (BlueprintButton bpbutt in _blueprints.components)
            {
                var req = new List<string> {compo1Temp, compo2Temp};
                if (req.Exists(x => x.ToLowerInvariant() == bpbutt.Compo1.ToLowerInvariant())) req.Remove(req.First(x => x.ToLowerInvariant() == bpbutt.Compo1.ToLowerInvariant()));
                if (req.Exists(x => x.ToLowerInvariant() == bpbutt.Compo2.ToLowerInvariant())) req.Remove(req.First(x => x.ToLowerInvariant() == bpbutt.Compo2.ToLowerInvariant()));
                if (!req.Any()) return;
            }

            var newBpb = new BlueprintButton(compo1Temp, compo1Name, compo2Temp, compo2Name, resultTemp, resultName, _resourceManager);
            newBpb.Update();

            newBpb.Clicked += BlueprintClicked;

            newBpb.Position = new Point(0, _blueprintsOffset);
            _blueprintsOffset += newBpb.ClientArea.Height;

            _blueprints.components.Add(newBpb);
        }

        void BlueprintClicked(BlueprintButton sender)
        {
            //craftTimer = new Timer_Bar(new Size(200,15), new TimeSpan(0,0,0,10));
            if (_playerManager != null)
                if (_playerManager.ControlledEntity != null)
                    if (_playerManager.ControlledEntity.HasComponent(ComponentFamily.Inventory))
                    {
                        var invComp = (InventoryComponent)_playerManager.ControlledEntity.GetComponent(ComponentFamily.Inventory);
                        if (!invComp.ContainsEntity(sender.Compo1) || !invComp.ContainsEntity(sender.Compo2))
                        {
                            _craftStatus.Text = "Status: You do not have the required items.";
                            _craftStatus.Color = Color.DarkRed;
                        }
                        else
                        {
                            _craftSlot1.SetEntity(invComp.GetEntity(sender.Compo1));
                            _craftSlot2.SetEntity(invComp.GetEntity(sender.Compo2));

                            CraftButtonClicked(null); //This is pretty dumb but i hate duplicate code.
                        }
                    }
        }

        public override void Render()
        {
            if (_showTabbedWindow)
            {
                _comboBg.Draw();
                _comboClose.Render();
                _tabHealth.Render();
                _tabEquip.Render();
                _tabCraft.Render();

                _txtDbg.Position = new Vector2D(Position.X + 20, Position.Y + 15);
                _txtDbg.Color = Color.NavajoWhite;
                if (_currentTab == 1) _txtDbg.Text = "Equipment";
                if (_currentTab == 2) _txtDbg.Text = "Status";
                if (_currentTab == 3) _txtDbg.Text = "Crafting";
                _txtDbg.Draw();

                switch (_currentTab)
                {
                    case (1): //Equip tab
                        {
                            #region Equip
                            _equipBg.Draw();

                            //Left Side - head, eyes, outer, hands, feet
                            _slotHead.Render();
                            _slotEyes.Render();
                            _slotOuter.Render();
                            _slotHands.Render();
                            _slotFeet.Render();

                            //Right Side - mask, ears, inner, belt, back
                            _slotMask.Render();
                            _slotEars.Render();
                            _slotInner.Render();
                            _slotBelt.Render();
                            _slotBack.Render();

                            if (_inventory != null) _inventory.Render();
                            break; 
                            #endregion
                        }
                    case (2): //Health tab
                        {
                            #region Status
                            _healthText.Draw();
                            break; 
                            #endregion
                        }
                    case (3): //Craft tab
                        {
                            #region Crafting
                            if (_craftTimer != null) _craftTimer.Render();
                            if (_inventory != null) _inventory.Render();
                            _craftSlot1.Render();
                            _craftSlot2.Render();
                            _craftButton.Render(); 
                            _craftStatus.Draw();
                            _blueprints.Render();
                            break;
                            #endregion
                        }
                }
            }
            _comboOpen.Render();

            _handLBg.Draw();
            if (LeftHand.Entity != null && LeftHand.HeldSprite != null) 
                LeftHand.HeldSprite.Draw(new Rectangle((int)_handLBg.Position.X + (int)(_handLBg.AABB.Width / 4f - LeftHand.HeldSprite.AABB.Width / 2f), (int)_handLBg.Position.Y + (int)(_handLBg.AABB.Height / 2f - LeftHand.HeldSprite.AABB.Height / 2f), (int)LeftHand.HeldSprite.AABB.Width, (int)LeftHand.HeldSprite.AABB.Height));

            _handRBg.Draw(); //Change to something more sane.
            if (RightHand.Entity != null && RightHand.HeldSprite != null) 
                RightHand.HeldSprite.Draw(new Rectangle((int)_handRBg.Position.X + (int)((_handRBg.AABB.Width / 4f) * 3 - RightHand.HeldSprite.AABB.Width / 2f), (int)_handRBg.Position.Y + (int)(_handRBg.AABB.Height / 2f - RightHand.HeldSprite.AABB.Height / 2f), (int)RightHand.HeldSprite.AABB.Width, (int)RightHand.HeldSprite.AABB.Height));

        }

        public override void Update()
        {
            if (_inventory == null && _playerManager != null) //Gotta do this here because the vars are null in the constructor.
                if (_playerManager.ControlledEntity != null)
                    if (_playerManager.ControlledEntity.HasComponent(ComponentFamily.Inventory))
                    {
                        var invComp = (InventoryComponent)_playerManager.ControlledEntity.GetComponent(ComponentFamily.Inventory);
                        _inventory = new InventoryViewer(invComp, _userInterfaceManager, _resourceManager);
                    }

            _comboBg.Position = Position;

            var equipBgPos = Position;
            _equipBg.Position = Position;
            equipBgPos.Offset((int)(_comboBg.AABB.Width / 2f - _equipBg.AABB.Width / 2f), 40);
            _equipBg.Position = equipBgPos;

            var comboOpenPos = Position;
            comboOpenPos.Offset((int)(_comboBg.Width - _comboOpen.ClientArea.Width), (int)_comboBg.Height - 1);
            _comboOpen.Position = comboOpenPos;
            _comboOpen.Update();

            var comboClosePos = Position;
            comboClosePos.Offset(264, 11); //Magic photoshop ruler numbers.
            _comboClose.Position = comboClosePos;
            _comboClose.Update();

            var tabEquipPos = Position;
            tabEquipPos.Offset(-26 , 76); //Magic photoshop ruler numbers.
            _tabEquip.Position = tabEquipPos;
            _tabEquip.Color = _currentTab == 1 ? Color.White : _inactiveColor;
            _tabEquip.Update();

            var tabHealthPos = tabEquipPos;
            tabHealthPos.Offset(0, 3 + _tabEquip.ClientArea.Height);
            _tabHealth.Position = tabHealthPos;
            _tabHealth.Color = _currentTab == 2 ? Color.White : _inactiveColor;
            _tabHealth.Update();
            
            var tabCraftPos = tabHealthPos;
            tabCraftPos.Offset(0, 3 + _tabHealth.ClientArea.Height);
            _tabCraft.Position = tabCraftPos;
            _tabCraft.Color = _currentTab == 3 ? Color.White : _inactiveColor;
            _tabCraft.Update();

            var handsPos = Position;
            handsPos.Offset(1, (int)_comboBg.Height);
            _handLBg.Position = handsPos;
            _handRBg.Position = handsPos;

            ClientArea = new Rectangle(Position.X, Position.Y, (int)_comboBg.AABB.Width, (int)_comboBg.AABB.Height + _comboOpen.ClientArea.Height);

            switch (_currentTab)
            {
                case (1): //Equip tab
                    {
                        #region Equip
                        //Only set position for topmost 2 slots directly. Rest uses these to position themselves.
                        var slotLeftStart = Position;
                        slotLeftStart.Offset(28, 40);
                        _slotHead.Position = slotLeftStart;
                        _slotHead.Update();

                        var slotRightStart = Position;
                        slotRightStart.Offset((int)(_comboBg.AABB.Width - _slotMask.ClientArea.Width - 28), 40);
                        _slotMask.Position = slotRightStart;
                        _slotMask.Update();

                        var vertSpacing = 6 + _slotHead.ClientArea.Height;

                        //Left Side - head, eyes, outer, hands, feet
                        slotLeftStart.Offset(0, vertSpacing);
                        _slotEyes.Position = slotLeftStart;
                        _slotEyes.Update();

                        slotLeftStart.Offset(0, vertSpacing);
                        _slotOuter.Position = slotLeftStart;
                        _slotOuter.Update();

                        slotLeftStart.Offset(0, vertSpacing);
                        _slotHands.Position = slotLeftStart;
                        _slotHands.Update();

                        slotLeftStart.Offset(0, vertSpacing);
                        _slotFeet.Position = slotLeftStart;
                        _slotFeet.Update();

                        //Right Side - mask, ears, inner, belt, back
                        slotRightStart.Offset(0, vertSpacing);
                        _slotEars.Position = slotRightStart;
                        _slotEars.Update();

                        slotRightStart.Offset(0, vertSpacing);
                        _slotInner.Position = slotRightStart;
                        _slotInner.Update();

                        slotRightStart.Offset(0, vertSpacing);
                        _slotBelt.Position = slotRightStart;
                        _slotBelt.Update();

                        slotRightStart.Offset(0, vertSpacing);
                        _slotBack.Position = slotRightStart;
                        _slotBack.Update();

                        if (_inventory != null)
                        {
                            _inventory.Position = new Point(Position.X + 12, Position.Y + 315);
                            _inventory.Update();
                        }
                        break; 
                        #endregion
                    }
                case (2): //Health tab
                    {
                        #region Status
                        var healthStr = "";

                        if (_playerManager == null || _playerManager.ControlledEntity == null)
                            return;

                        var playerEnt = _playerManager.ControlledEntity;
                        var healthcomp = (HumanHealthComponent)playerEnt.GetComponent(ComponentFamily.Damageable); //Unsafe cast. Might not be human health comp.

                        foreach (var loc in healthcomp.DamageZones)
                        {
                            healthStr += loc.location + "   --   ";

                            var healthPct = loc.currentHealth / (float)loc.maxHealth;

                            if (healthPct > 0.75) healthStr += "HEALTHY (" + loc.currentHealth + " / " + loc.maxHealth + ")" + Environment.NewLine;
                            else if (healthPct > 0.50) healthStr += "INJURED (" + loc.currentHealth + " / " + loc.maxHealth + ")" + Environment.NewLine;
                            else if (healthPct > 0.25) healthStr += "WOUNDED (" + loc.currentHealth + " / " + loc.maxHealth + ")" + Environment.NewLine;
                            else if (healthPct > 0) healthStr += "CRITICAL (" + loc.currentHealth + " / " + loc.maxHealth + ")" + Environment.NewLine;
                            else healthStr += "CRIPPLED (" + loc.currentHealth+ " / " + loc.maxHealth + ")" + Environment.NewLine;
                        }

                        healthStr += Environment.NewLine + "Total Health : " + healthcomp.GetHealth() + " / " + healthcomp.GetMaxHealth();

                        _healthText.Text = healthStr;
                        _healthText.Color = Color.FloralWhite;
                        _healthText.Position = new Vector2D(Position.X + 40, Position.Y + 40);

                        break; 
                        #endregion
                    }
                case (3): //Craft tab
                    {
                        #region Crafting
                        _craftSlot1.Position = new Point(Position.X + 40, Position.Y + 80);
                        _craftSlot1.Update();

                        _craftSlot2.Position = new Point(Position.X + ClientArea.Width - _craftSlot2.ClientArea.Width - 40, Position.Y + 80);
                        _craftSlot2.Update();

                        _craftButton.Position = new Point(Position.X + (int)(ClientArea.Width / 2f) - (int)(_craftButton.ClientArea.Width / 2f), Position.Y + 70);
                        _craftButton.Update();

                        if (_craftTimer != null) _craftTimer.Position = new Point(Position.X + (int)(ClientArea.Width / 2f) - (int)(_craftTimer.ClientArea.Width / 2f), Position.Y + 155);

                        _craftStatus.Position = new Vector2D(Position.X + (int)(ClientArea.Width / 2f) - (int)(_craftStatus.Width / 2f), Position.Y + 40);

                        _blueprints.Position = new Point(Position.X + 40, Position.Y + 180);
                        _blueprints.Update();

                        if (_inventory != null)
                        {
                            _inventory.Position = new Point(Position.X + 12, Position.Y + 315);
                            _inventory.Update();
                        }
                        break; 
                        #endregion
                    }   
            }

            if (_craftTimer != null) _craftTimer.Update(); //Needs to update even when its not on the crafting tab so it continues to count.

            #region Hands UI
            if (_playerManager == null || _playerManager.ControlledEntity == null)
                return;

            var entity = _playerManager.ControlledEntity;
            var hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);

            if (hands.currentHand == Hand.Left)
            {
                _handLBg.Color = Color.White;
                _handRBg.Color = _inactiveColor;
            }
            else
            {
                _handRBg.Color = Color.White;
                _handLBg.Color = _inactiveColor;
            }

            #endregion
        }

        public override void Dispose()
        {
        }

        public void UpdateHandIcons()
        {
            if (_playerManager.ControlledEntity == null)
                return;

            var entity = _playerManager.ControlledEntity;
            var hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);

            if (hands == null) return;

            if (hands.HandSlots.Keys.Contains(Hand.Left))
            {
                var entityL = hands.HandSlots[Hand.Left];
                LeftHand.Entity = entityL;
                LeftHand.HeldSprite = Utilities.GetSpriteComponentSprite(entityL);
            }
            else
            {
                LeftHand.Entity = null;
                LeftHand.HeldSprite = null;
            }

            if (hands.HandSlots.Keys.Contains(Hand.Right))
            {
                var entityR = hands.HandSlots[Hand.Right];
                RightHand.Entity = entityR;
                RightHand.HeldSprite = Utilities.GetSpriteComponentSprite(entityR);
            }
            else
            {
                RightHand.Entity = null;
                RightHand.HeldSprite = null;
            }
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (_comboOpen.MouseDown(e)) return true;

            if (_showTabbedWindow)
            {
                if (_comboClose.MouseDown(e)) return true;
                if (_tabEquip.MouseDown(e)) return true;
                if (_tabHealth.MouseDown(e)) return true;
                if (_tabCraft.MouseDown(e)) return true;
            }

            #region Hands UI, Switching
            var entity = _playerManager.ControlledEntity;
            var hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);
            if (e.Buttons == MouseButtons.Right)
            {
                if (Utilities.SpritePixelHit(_handLBg, e.Position))
                {
                    SendSwitchHandTo(Hand.Left);
                    return true;
                }

                if (Utilities.SpritePixelHit(_handRBg, e.Position))
                {
                    SendSwitchHandTo(Hand.Right);
                    return true;
                }
            }
            else if (e.Buttons == MouseButtons.Left)
            {
                if (Utilities.SpritePixelHit(_handLBg, e.Position))
                {
                    if (hands.HandSlots.Keys.Contains(Hand.Left))
                    {
                        var entityL = hands.HandSlots[Hand.Left];
                        _userInterfaceManager.DragInfo.StartDrag(entityL);
                    }
                    return true;
                }

                if (Utilities.SpritePixelHit(_handRBg, e.Position))
                {
                    if (hands.HandSlots.Keys.Contains(Hand.Right))
                    {
                        var entityR = hands.HandSlots[Hand.Right];
                        _userInterfaceManager.DragInfo.StartDrag(entityR);
                    }
                    return true;
                }
            }
            #endregion

            switch (_currentTab)
            {
                case (1): //Equip tab
                    {
                        #region Equip
                        //Left Side - head, eyes, outer, hands, feet
                        if (_slotHead.MouseDown(e)) return true;
                        if (_slotEyes.MouseDown(e)) return true;
                        if (_slotOuter.MouseDown(e)) return true;
                        if (_slotHands.MouseDown(e)) return true;
                        if (_slotFeet.MouseDown(e)) return true;

                        //Right Side - mask, ears, inner, belt, back
                        if (_slotMask.MouseDown(e)) return true;
                        if (_slotEars.MouseDown(e)) return true;
                        if (_slotInner.MouseDown(e)) return true;
                        if (_slotBelt.MouseDown(e)) return true;
                        if (_slotBack.MouseDown(e)) return true;

                        if (_inventory != null) if (_inventory.MouseDown(e)) return true;
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
                        if (_craftTimer != null) if (_craftTimer.MouseDown(e)) return true;
                        if (_craftSlot1.MouseDown(e)) return true;
                        if (_craftSlot2.MouseDown(e)) return true;
                        if (_craftButton.MouseDown(e)) return true;
                        if (_blueprints.MouseDown(e)) return true;
                        if (_inventory != null) if (_inventory.MouseDown(e)) return true;

                        break; 
                        #endregion
                    }
            }

            return false;
        }

        private void SendSwitchHandTo(Hand hand)
        {
            var playerEntity = _playerManager.ControlledEntity;
            var equipComponent = (HumanHandsComponent)playerEntity.GetComponent(ComponentFamily.Hands);
            equipComponent.SendSwitchHands(hand);
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            var mouseAABB = new PointF(e.Position.X, e.Position.Y);

            if (_userInterfaceManager.DragInfo.IsEntity && _userInterfaceManager.DragInfo.DragEntity != null)
            {
                #region Hands
                if (_playerManager.ControlledEntity == null)
                    return false;

                var entity = _playerManager.ControlledEntity;

                var equipment = (EquipmentComponent)entity.GetComponent(ComponentFamily.Equipment);
                var hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);

                if (hands == null || entity == null) return false;

                if (Utilities.SpritePixelHit(_handLBg, e.Position))
                {
                    if (!hands.HandSlots.ContainsKey(Hand.Left))
                    {
                        if (hands.HandSlots.ContainsValue(_userInterfaceManager.DragInfo.DragEntity))
                        {
                            if (hands.HandSlots.First(x => x.Value == _userInterfaceManager.DragInfo.DragEntity).Key == Hand.Left) //From me to me, ignore.
                                return false;

                                hands.SendDropEntity(_userInterfaceManager.DragInfo.DragEntity); //Other hand to me.

                        }
                        equipment.DispatchUnEquipItemToSpecifiedHand(_userInterfaceManager.DragInfo.DragEntity.Uid, Hand.Left);
                        _userInterfaceManager.DragInfo.Reset();
                        return true;
                    }
                }

                if (Utilities.SpritePixelHit(_handRBg, e.Position))
                {
                    if (!hands.HandSlots.ContainsKey(Hand.Right))
                    {
                        if (hands.HandSlots.ContainsValue(_userInterfaceManager.DragInfo.DragEntity))
                        {
                            if (hands.HandSlots.First(x => x.Value == _userInterfaceManager.DragInfo.DragEntity).Key == Hand.Right) //From me to me, ignore.
                                return false;

                            hands.SendDropEntity(_userInterfaceManager.DragInfo.DragEntity); //Other hand to me.
                        }
                        equipment.DispatchUnEquipItemToSpecifiedHand(_userInterfaceManager.DragInfo.DragEntity.Uid, Hand.Right);
                        _userInterfaceManager.DragInfo.Reset();
                        return true;
                    }
                } 
                #endregion
            }

            switch (_currentTab)
            {
                case (1): //Equip tab
                    {
                        #region Equip
                        //Left Side - head, eyes, outer, hands, feet
                        if (_slotHead.MouseUp(e)) return true;
                        if (_slotEyes.MouseUp(e)) return true;
                        if (_slotOuter.MouseUp(e)) return true;
                        if (_slotHands.MouseUp(e)) return true;
                        if (_slotFeet.MouseUp(e)) return true;

                        //Right Side - mask, ears, inner, belt, back
                        if (_slotMask.MouseUp(e)) return true;
                        if (_slotEars.MouseUp(e)) return true;
                        if (_slotInner.MouseUp(e)) return true;
                        if (_slotBelt.MouseUp(e)) return true;
                        if (_slotBack.MouseUp(e)) return true;

                        if (_inventory != null) if (_inventory.MouseUp(e)) return true;

                        if (_comboBg.AABB.Contains(mouseAABB) && _userInterfaceManager.DragInfo.IsEntity && _userInterfaceManager.DragInfo.DragEntity != null)
                        { //Should be refined to only trigger in the equip area. Equip it if they drop it anywhere on the thing. This might make the slots obsolete if we keep it.
                            if (_playerManager.ControlledEntity == null)
                                return false;

                            var entity = (Entity)_playerManager.ControlledEntity;

                            var equipment = (EquipmentComponent)entity.GetComponent(ComponentFamily.Equipment);
                            equipment.DispatchEquip(_userInterfaceManager.DragInfo.DragEntity.Uid);
                            _userInterfaceManager.DragInfo.Reset();
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
                        if (_craftTimer != null) if (_craftTimer.MouseUp(e)) return true;
                        if (_craftSlot1.MouseUp(e))
                        {
                            if (_craftSlot2.ContainingEntity == _craftSlot1.ContainingEntity) _craftSlot2.ResetEntity();
                            return true;
                        }
                        if (_craftSlot2.MouseUp(e))
                        {
                            if (_craftSlot1.ContainingEntity == _craftSlot2.ContainingEntity) _craftSlot1.ResetEntity();
                            return true;
                        }
                        if (_craftButton.MouseUp(e)) return true;
                        if (_blueprints.MouseUp(e)) return true;
                        if (_inventory != null) if (_inventory.MouseUp(e)) return true;
                        break; 
                        #endregion
                    }
            }

            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {      
            switch (_currentTab)
            {
                case (1): //Equip tab
                    {
                        #region Equip
                        //Left Side - head, eyes, outer, hands, feet
                        _slotHead.MouseMove(e);
                        _slotEyes.MouseMove(e);
                        _slotOuter.MouseMove(e);
                        _slotHands.MouseMove(e);
                        _slotFeet.MouseMove(e);

                        //Right Side - mask, ears, inner, belt, back
                        _slotMask.MouseMove(e);
                        _slotEars.MouseMove(e);
                        _slotInner.MouseMove(e);
                        _slotBelt.MouseMove(e);
                        _slotBack.MouseMove(e);

                        if (_inventory != null) _inventory.MouseMove(e);
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
                        if (_craftTimer != null) _craftTimer.MouseMove(e);
                        _craftSlot1.MouseMove(e);
                        _craftSlot2.MouseMove(e);
                        _craftButton.MouseMove(e);
                        _blueprints.MouseMove(e);
                        if (_inventory != null) _inventory.MouseMove(e);
                        break; 
                        #endregion
                    }
            }
        }
    }
}
