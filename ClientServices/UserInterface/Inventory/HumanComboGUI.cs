using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CGO;
using ClientInterfaces.Network;
using ClientInterfaces.Player;
using ClientInterfaces.Resource;
using ClientInterfaces.UserInterface;
using ClientServices.UserInterface.Components;
using GameObject;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace ClientServices.UserInterface.Inventory
{
    public class HumanComboGui : GuiComponent
    {
        #region Inventory UI

        private readonly EquipmentSlotUi _slotBack;
        private readonly EquipmentSlotUi _slotBelt;
        private readonly EquipmentSlotUi _slotEars;
        private readonly EquipmentSlotUi _slotEyes;
        private readonly EquipmentSlotUi _slotFeet;
        private readonly EquipmentSlotUi _slotHands;
        private readonly EquipmentSlotUi _slotHead;

        private readonly EquipmentSlotUi _slotInner;
        private readonly EquipmentSlotUi _slotMask;
        private readonly EquipmentSlotUi _slotOuter;

        private Dictionary<EquipmentSlot, EquipmentSlotUi> _equipmentSlots =
            new Dictionary<EquipmentSlot, EquipmentSlotUi>();

        private InventoryViewer _inventory;

        #endregion

        #region Crafting UI

        private readonly ScrollableContainer _blueprints;
        private readonly ImageButton _craftButton;
        private readonly CraftSlotUi _craftSlot1;
        private readonly CraftSlotUi _craftSlot2;
        private readonly TextSprite _craftStatus;
        private int _blueprintsOffset;
        private Timer_Bar _craftTimer;

        #endregion

        #region Status UI

        private readonly ArmorInfoLabel _ResBlunt;
        private readonly ArmorInfoLabel _ResBurn;
        private readonly ArmorInfoLabel _ResFreeze;
        private readonly ArmorInfoLabel _ResPierce;
        private readonly ArmorInfoLabel _ResShock;
        private readonly ArmorInfoLabel _ResSlash;
        private readonly ArmorInfoLabel _ResTox;

        #endregion

        private readonly Sprite _comboBg;
        private readonly ImageButton _comboClose;

        private readonly Sprite _equipBg;

        private readonly Color _inactiveColor = Color.FromArgb(255, 90, 90, 90);

        private readonly INetworkManager _networkManager;
        private readonly IPlayerManager _playerManager;
        private readonly IResourceManager _resourceManager;
        private readonly ImageButton _tabCraft;
        private readonly ImageButton _tabEquip;
        private readonly ImageButton _tabHealth;
        private readonly TextSprite _txtDbg;
        private readonly IUserInterfaceManager _userInterfaceManager;
        private byte _currentTab = 1; //1 = Inventory, 2 = Health, 3 = Crafting
        private bool _showTabbedWindow;

        public HumanComboGui(IPlayerManager playerManager, INetworkManager networkManager,
                             IResourceManager resourceManager, IUserInterfaceManager userInterfaceManager)
        {
            _networkManager = networkManager;
            _playerManager = playerManager;
            _resourceManager = resourceManager;
            _userInterfaceManager = userInterfaceManager;

            ComponentClass = GuiComponentType.ComboGui;

            #region Status UI

            _ResBlunt = new ArmorInfoLabel(DamageType.Bludgeoning, resourceManager);
            _ResBurn = new ArmorInfoLabel(DamageType.Burn, resourceManager);
            _ResFreeze = new ArmorInfoLabel(DamageType.Freeze, resourceManager);
            _ResPierce = new ArmorInfoLabel(DamageType.Piercing, resourceManager);
            _ResShock = new ArmorInfoLabel(DamageType.Shock, resourceManager);
            _ResSlash = new ArmorInfoLabel(DamageType.Slashing, resourceManager);
            _ResTox = new ArmorInfoLabel(DamageType.Toxin, resourceManager);

            #endregion

            _equipBg = _resourceManager.GetSprite("outline");

            _comboBg = _resourceManager.GetSprite("combo_bg");

            _comboClose = new ImageButton
                              {
                                  ImageNormal = "button_closecombo",
                              };

            _tabEquip = new ImageButton
                            {
                                ImageNormal = "tab_equip",
                            };
            _tabEquip.Clicked += TabClicked;

            _tabHealth = new ImageButton
                             {
                                 ImageNormal = "tab_health",
                             };
            _tabHealth.Clicked += TabClicked;

            _tabCraft = new ImageButton
                            {
                                ImageNormal = "tab_craft",
                            };
            _tabCraft.Clicked += TabClicked;

            _comboClose.Clicked += ComboCloseClicked;

            //Left Side - head, eyes, outer, hands, feet
            _slotHead = new EquipmentSlotUi(EquipmentSlot.Head, _playerManager, _resourceManager, _userInterfaceManager);
            _slotHead.Dropped += SlotDropped;

            _slotEyes = new EquipmentSlotUi(EquipmentSlot.Eyes, _playerManager, _resourceManager, _userInterfaceManager);
            _slotEyes.Dropped += SlotDropped;

            _slotOuter = new EquipmentSlotUi(EquipmentSlot.Outer, _playerManager, _resourceManager,
                                             _userInterfaceManager);
            _slotOuter.Dropped += SlotDropped;

            _slotHands = new EquipmentSlotUi(EquipmentSlot.Hands, _playerManager, _resourceManager,
                                             _userInterfaceManager);
            _slotHands.Dropped += SlotDropped;

            _slotFeet = new EquipmentSlotUi(EquipmentSlot.Feet, _playerManager, _resourceManager, _userInterfaceManager);
            _slotFeet.Dropped += SlotDropped;

            //Right Side - mask, ears, inner, belt, back
            _slotMask = new EquipmentSlotUi(EquipmentSlot.Mask, _playerManager, _resourceManager, _userInterfaceManager);
            _slotMask.Dropped += SlotDropped;

            _slotEars = new EquipmentSlotUi(EquipmentSlot.Ears, _playerManager, _resourceManager, _userInterfaceManager);
            _slotEars.Dropped += SlotDropped;

            _slotInner = new EquipmentSlotUi(EquipmentSlot.Inner, _playerManager, _resourceManager,
                                             _userInterfaceManager);
            _slotInner.Dropped += SlotDropped;

            _slotBelt = new EquipmentSlotUi(EquipmentSlot.Belt, _playerManager, _resourceManager, _userInterfaceManager);
            _slotBelt.Dropped += SlotDropped;

            _slotBack = new EquipmentSlotUi(EquipmentSlot.Back, _playerManager, _resourceManager, _userInterfaceManager);
            _slotBack.Dropped += SlotDropped;

            _txtDbg = new TextSprite("comboDlgDbg", "Combo Debug", _resourceManager.GetFont("CALIBRI"));

            _craftSlot1 = new CraftSlotUi(_resourceManager, _userInterfaceManager);
            _craftSlot2 = new CraftSlotUi(_resourceManager, _userInterfaceManager);

            _craftButton = new ImageButton
                               {
                                   ImageNormal = "wrenchbutt"
                               };
            _craftButton.Clicked += CraftButtonClicked;

            _craftStatus = new TextSprite("craftText", "Status", _resourceManager.GetFont("CALIBRI"))
                               {
                                   ShadowColor = Color.DimGray,
                                   ShadowOffset = new Vector2D(1, 1),
                                   Shadowed = true
                               };

            _blueprints = new ScrollableContainer("blueprintCont", new Size(210, 100), _resourceManager);
        }

        private void CraftButtonClicked(ImageButton sender)
        {
            //craftTimer = new Timer_Bar(new Size(200,15), new TimeSpan(0,0,0,10));
            if (_craftSlot1.ContainingEntity == null || _craftSlot2.ContainingEntity == null) return;

            if (_playerManager != null)
                if (_playerManager.ControlledEntity != null)
                    if (_playerManager.ControlledEntity.HasComponent(ComponentFamily.Inventory))
                    {
                        var invComp =
                            (InventoryComponent) _playerManager.ControlledEntity.GetComponent(ComponentFamily.Inventory);
                        if (invComp.ContainedEntities.Count >= invComp.MaxSlots)
                        {
                            _craftStatus.Text = "Status: Not enough Space";
                            _craftStatus.Color = Color.DarkRed;
                            return;
                        }
                    }

            NetOutgoingMessage msg = _networkManager.CreateMessage();
            msg.Write((byte) NetMessage.CraftMessage);
            msg.Write((byte) CraftMessage.StartCraft);
            msg.Write(_craftSlot1.ContainingEntity.Uid);
            msg.Write(_craftSlot2.ContainingEntity.Uid);
            _networkManager.SendMessage(msg, NetDeliveryMethod.ReliableUnordered);
        }

        private void SlotDropped(EquipmentSlotUi sender, Entity dropped)
        {
            _userInterfaceManager.DragInfo.Reset();

            if (_playerManager.ControlledEntity == null)
                return;

            Entity entity = _playerManager.ControlledEntity;

            var equipment = (EquipmentComponent) entity.GetComponent(ComponentFamily.Equipment);

            equipment.DispatchEquip(dropped.Uid); //Serverside equip component will equip and remove from hands.
        }

        public override void ComponentUpdate(params object[] args)
        {
            switch ((ComboGuiMessage) args[0])
            {
                case ComboGuiMessage.ToggleShowPage:
                    var page = (int) args[1];
                    if (page == _currentTab) _showTabbedWindow = !_showTabbedWindow;
                    else
                    {
                        _showTabbedWindow = true;
                        ActivateTab(page);
                    }
                    break;
            }
        }

        public void ActivateTab(int tabNum)
        {
            switch (tabNum)
            {
                case 1: //Equip
                    _currentTab = 1;
                    break;
                case 2: //Status
                    if (_playerManager.ControlledEntity != null) //TEMPORARY SOLUTION.
                    {
                        Entity resEntity = _playerManager.ControlledEntity;
                        var entStats = (EntityStatsComp) resEntity.GetComponent(ComponentFamily.EntityStats);
                        entStats.PullFullUpdate();
                    }
                    _currentTab = 2;
                    break;
                case 3: //Craft
                    _currentTab = 3;
                    break;
            }
        }

        private void TabClicked(ImageButton sender)
        {
            if (sender == _tabEquip) ActivateTab(1);
            if (sender == _tabHealth) ActivateTab(2);
            if (sender == _tabCraft) ActivateTab(3);
        }

        private void ComboOpenClicked(ImageButton sender)
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        {
            _showTabbedWindow = !_showTabbedWindow;
            _craftStatus.Text = "Status";
            _craftStatus.Color = Color.White;
        }

        private void ComboCloseClicked(ImageButton sender)
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

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
            var messageType = (ComboGuiMessage) message.ReadByte();

            switch (messageType)
            {
                case ComboGuiMessage.CancelCraftBar:
                    if (_craftTimer != null)
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
                    if (_craftTimer != null)
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
            string compo1Temp = message.ReadString();
            string compo1Name = message.ReadString();
            string compo2Temp = message.ReadString();
            string compo2Name = message.ReadString();
            string resultTemp = message.ReadString();
            string resultName = message.ReadString();

            _craftStatus.Text = "Status: You successfully create '" + resultName + "'";
            _craftStatus.Color = Color.Green;

            foreach (BlueprintButton bpbutt in _blueprints.components)
            {
                var req = new List<string> {compo1Temp, compo2Temp};
                if (req.Exists(x => x.ToLowerInvariant() == bpbutt.Compo1.ToLowerInvariant()))
                    req.Remove(req.First(x => x.ToLowerInvariant() == bpbutt.Compo1.ToLowerInvariant()));
                if (req.Exists(x => x.ToLowerInvariant() == bpbutt.Compo2.ToLowerInvariant()))
                    req.Remove(req.First(x => x.ToLowerInvariant() == bpbutt.Compo2.ToLowerInvariant()));
                if (!req.Any()) return;
            }

            var newBpb = new BlueprintButton(compo1Temp, compo1Name, compo2Temp, compo2Name, resultTemp, resultName,
                                             _resourceManager);
            newBpb.Update(0);

            newBpb.Clicked += BlueprintClicked;

            newBpb.Position = new Point(0, _blueprintsOffset);
            _blueprintsOffset += newBpb.ClientArea.Height;

            _blueprints.components.Add(newBpb);
        }

        private void BlueprintClicked(BlueprintButton sender)
        {
            //craftTimer = new Timer_Bar(new Size(200,15), new TimeSpan(0,0,0,10));
            if (_playerManager != null)
                if (_playerManager.ControlledEntity != null)
                    if (_playerManager.ControlledEntity.HasComponent(ComponentFamily.Inventory))
                    {
                        var invComp =
                            (InventoryComponent) _playerManager.ControlledEntity.GetComponent(ComponentFamily.Inventory);
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

                            _ResBlunt.Render();
                            _ResPierce.Render();
                            _ResSlash.Render();
                            _ResBurn.Render();
                            _ResFreeze.Render();
                            _ResShock.Render();
                            _ResTox.Render();

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
        }

        public override void Update(float frameTime)
        {
            if (_inventory == null && _playerManager != null)
                //Gotta do this here because the vars are null in the constructor.
                if (_playerManager.ControlledEntity != null)
                    if (_playerManager.ControlledEntity.HasComponent(ComponentFamily.Inventory))
                    {
                        var invComp =
                            (InventoryComponent) _playerManager.ControlledEntity.GetComponent(ComponentFamily.Inventory);
                        _inventory = new InventoryViewer(invComp, _userInterfaceManager, _resourceManager);
                    }

            _comboBg.Position = Position;

            Point equipBgPos = Position;
            _equipBg.Position = Position;
            equipBgPos.Offset((int) (_comboBg.AABB.Width/2f - _equipBg.AABB.Width/2f), 40);
            _equipBg.Position = equipBgPos;

            Point comboClosePos = Position;
            comboClosePos.Offset(264, 11); //Magic photoshop ruler numbers.
            _comboClose.Position = comboClosePos;
            _comboClose.Update(frameTime);

            Point tabEquipPos = Position;
            tabEquipPos.Offset(-26, 76); //Magic photoshop ruler numbers.
            _tabEquip.Position = tabEquipPos;
            _tabEquip.Color = _currentTab == 1 ? Color.White : _inactiveColor;
            _tabEquip.Update(frameTime);

            Point tabHealthPos = tabEquipPos;
            tabHealthPos.Offset(0, 3 + _tabEquip.ClientArea.Height);
            _tabHealth.Position = tabHealthPos;
            _tabHealth.Color = _currentTab == 2 ? Color.White : _inactiveColor;
            _tabHealth.Update(frameTime);

            Point tabCraftPos = tabHealthPos;
            tabCraftPos.Offset(0, 3 + _tabHealth.ClientArea.Height);
            _tabCraft.Position = tabCraftPos;
            _tabCraft.Color = _currentTab == 3 ? Color.White : _inactiveColor;
            _tabCraft.Update(frameTime);

            ClientArea = new Rectangle(Position.X, Position.Y, (int) _comboBg.AABB.Width, (int) _comboBg.AABB.Height);

            switch (_currentTab)
            {
                case (1): //Equip tab
                    {
                        #region Equip

                        //Only set position for topmost 2 slots directly. Rest uses these to position themselves.
                        Point slotLeftStart = Position;
                        slotLeftStart.Offset(28, 40);
                        _slotHead.Position = slotLeftStart;
                        _slotHead.Update(frameTime);

                        Point slotRightStart = Position;
                        slotRightStart.Offset((int) (_comboBg.AABB.Width - _slotMask.ClientArea.Width - 28), 40);
                        _slotMask.Position = slotRightStart;
                        _slotMask.Update(frameTime);

                        int vertSpacing = 6 + _slotHead.ClientArea.Height;

                        //Left Side - head, eyes, outer, hands, feet
                        slotLeftStart.Offset(0, vertSpacing);
                        _slotEyes.Position = slotLeftStart;
                        _slotEyes.Update(frameTime);

                        slotLeftStart.Offset(0, vertSpacing);
                        _slotOuter.Position = slotLeftStart;
                        _slotOuter.Update(frameTime);

                        slotLeftStart.Offset(0, vertSpacing);
                        _slotHands.Position = slotLeftStart;
                        _slotHands.Update(frameTime);

                        slotLeftStart.Offset(0, vertSpacing);
                        _slotFeet.Position = slotLeftStart;
                        _slotFeet.Update(frameTime);

                        //Right Side - mask, ears, inner, belt, back
                        slotRightStart.Offset(0, vertSpacing);
                        _slotEars.Position = slotRightStart;
                        _slotEars.Update(frameTime);

                        slotRightStart.Offset(0, vertSpacing);
                        _slotInner.Position = slotRightStart;
                        _slotInner.Update(frameTime);

                        slotRightStart.Offset(0, vertSpacing);
                        _slotBelt.Position = slotRightStart;
                        _slotBelt.Update(frameTime);

                        slotRightStart.Offset(0, vertSpacing);
                        _slotBack.Position = slotRightStart;
                        _slotBack.Update(frameTime);

                        if (_inventory != null)
                        {
                            _inventory.Position = new Point(Position.X + 12, Position.Y + 315);
                            _inventory.Update(frameTime);
                        }
                        break;

                        #endregion
                    }
                case (2): //Health tab
                    {
                        #region Status

                        var resLinePos = new Point(Position.X + 35, Position.Y + 70);

                        const int spacing = 8;

                        _ResBlunt.Position = resLinePos;
                        resLinePos.Offset(0, _ResBlunt.ClientArea.Height + spacing);
                        _ResBlunt.Update(frameTime);

                        _ResPierce.Position = resLinePos;
                        resLinePos.Offset(0, _ResPierce.ClientArea.Height + spacing);
                        _ResPierce.Update(frameTime);

                        _ResSlash.Position = resLinePos;
                        resLinePos.Offset(0, _ResSlash.ClientArea.Height + spacing);
                        _ResSlash.Update(frameTime);

                        _ResBurn.Position = resLinePos;
                        resLinePos.Offset(0, _ResBurn.ClientArea.Height + spacing);
                        _ResBurn.Update(frameTime);

                        _ResFreeze.Position = resLinePos;
                        resLinePos.Offset(0, _ResFreeze.ClientArea.Height + spacing);
                        _ResFreeze.Update(frameTime);

                        _ResShock.Position = resLinePos;
                        resLinePos.Offset(0, _ResShock.ClientArea.Height + spacing);
                        _ResShock.Update(frameTime);

                        _ResTox.Position = resLinePos;
                        _ResTox.Update(frameTime);

                        break;

                        #endregion
                    }
                case (3): //Craft tab
                    {
                        #region Crafting

                        _craftSlot1.Position = new Point(Position.X + 40, Position.Y + 80);
                        _craftSlot1.Update(frameTime);

                        _craftSlot2.Position =
                            new Point(Position.X + ClientArea.Width - _craftSlot2.ClientArea.Width - 40, Position.Y + 80);
                        _craftSlot2.Update(frameTime);

                        _craftButton.Position =
                            new Point(
                                Position.X + (int) (ClientArea.Width/2f) - (int) (_craftButton.ClientArea.Width/2f),
                                Position.Y + 70);
                        _craftButton.Update(frameTime);

                        if (_craftTimer != null)
                            _craftTimer.Position =
                                new Point(
                                    Position.X + (int) (ClientArea.Width/2f) - (int) (_craftTimer.ClientArea.Width/2f),
                                    Position.Y + 155);

                        _craftStatus.Position =
                            new Vector2D(Position.X + (int) (ClientArea.Width/2f) - (int) (_craftStatus.Width/2f),
                                         Position.Y + 40);

                        _blueprints.Position = new Point(Position.X + 40, Position.Y + 180);
                        _blueprints.Update(frameTime);

                        if (_inventory != null)
                        {
                            _inventory.Position = new Point(Position.X + 12, Position.Y + 315);
                            _inventory.Update(frameTime);
                        }
                        break;

                        #endregion
                    }
            }

            if (_craftTimer != null)
                _craftTimer.Update(frameTime);
                    //Needs to update even when its not on the crafting tab so it continues to count.
        }

        public override void Dispose()
        {
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (_showTabbedWindow)
            {
                if (_comboClose.MouseDown(e)) return true;
                if (_tabEquip.MouseDown(e)) return true;
                if (_tabHealth.MouseDown(e)) return true;
                if (_tabCraft.MouseDown(e)) return true;

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
            }
            return false;
        }

        private void SendSwitchHandTo(Hand hand)
        {
            Entity playerEntity = _playerManager.ControlledEntity;
            var equipComponent = (HumanHandsComponent) playerEntity.GetComponent(ComponentFamily.Hands);
            equipComponent.SendSwitchHands(hand);
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            var mouseAABB = new PointF(e.Position.X, e.Position.Y);

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

                        if (_comboBg.AABB.Contains(mouseAABB) && _userInterfaceManager.DragInfo.IsEntity &&
                            _userInterfaceManager.DragInfo.IsActive)
                        {
                            //Should be refined to only trigger in the equip area. Equip it if they drop it anywhere on the thing. This might make the slots obsolete if we keep it.
                            if (_playerManager.ControlledEntity == null)
                                return false;

                            Entity entity = _playerManager.ControlledEntity;

                            var equipment = (EquipmentComponent) entity.GetComponent(ComponentFamily.Equipment);
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

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            if (_currentTab == 1 || _currentTab == 3)
            {
                if (_inventory.MouseWheelMove(e)) return true;
            }
            return false;
        }
    }
}