using Lidgren.Network;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface.Components;
using SS14.Shared;
using SS14.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.UserInterface.Inventory
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

        private readonly Color _inactiveColor = new Color(90, 90, 90);

        private readonly INetworkManager _networkManager;
        private readonly IPlayerManager _playerManager;
        private readonly IResourceManager _resourceManager;
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
        }

        private void ComboOpenClicked(ImageButton sender)
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        {
            _showTabbedWindow = !_showTabbedWindow;
        }

        private void ComboCloseClicked(ImageButton sender)
        {
            _showTabbedWindow = false;
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (e.Code == Keyboard.Key.I)
            {
                _showTabbedWindow = !_showTabbedWindow;
                return true;
            }

            return false;
        }

        public override void Render()
        {
            if (_showTabbedWindow)
            {
                _comboBg.Draw();
                _comboClose.Render();
                _tabHealth.Render();
                _tabEquip.Render();

                _txtDbg.Position = new Vector2i(Position.X + 20, Position.Y + 15);
                _txtDbg.Color = new SFML.Graphics.Color(255, 222, 173);
                if (_currentTab == 1) _txtDbg.Text = "Equipment";
                if (_currentTab == 2) _txtDbg.Text = "Status";
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

            _comboBg.Position = new Vector2f (Position.X, Position.Y);

            var bounds = _comboBg.GetLocalBounds();
            var equipBgPos = Position;
            _equipBg.Position = new Vector2f (Position.X,Position.Y);
            equipBgPos += new Vector2i((int)(bounds.Width / 2f - _equipBg.GetLocalBounds().Width / 2f), 40);
            _equipBg.Position = new Vector2f(equipBgPos.X,equipBgPos.Y);

            var comboClosePos = Position;
            comboClosePos += new Vector2i(264, 11); //Magic photoshop ruler numbers.
            _comboClose.Position = comboClosePos;
            _comboClose.Update(frameTime);

            var tabEquipPos = Position;
            tabEquipPos += new Vector2i(-26, 76); //Magic photoshop ruler numbers.
            _tabEquip.Position = tabEquipPos;
            _tabEquip.Color = _currentTab == 1 ? Color.White :_inactiveColor;
            _tabEquip.Update(frameTime);

            var tabHealthPos = tabEquipPos;
            tabHealthPos += new Vector2i(0, 3 + _tabEquip.ClientArea.Height);
            _tabHealth.Position = tabHealthPos;
            _tabHealth.Color = _currentTab == 2 ? Color.White : _inactiveColor;
            _tabHealth.Update(frameTime);

            ClientArea = new IntRect(Position.X, Position.Y, (int)bounds.Width, (int)bounds.Height);

            switch (_currentTab)
            {
                case (1): //Equip tab
                    {
                        #region Equip

                        //Only set position for topmost 2 slots directly. Rest uses these to position themselves.
                        var slotLeftStart = Position;
                        slotLeftStart += new Vector2i(28, 40);
                        _slotHead.Position = slotLeftStart;
                        _slotHead.Update(frameTime);

                        var slotRightStart = Position;
                        slotRightStart += new Vector2i((int) (bounds.Width - _slotMask.ClientArea.Width - 28), 40);
                        _slotMask.Position = slotRightStart;
                        _slotMask.Update(frameTime);

                        int vertSpacing = 6 + _slotHead.ClientArea.Height;

                        //Left Side - head, eyes, outer, hands, feet
                        slotLeftStart.Y += vertSpacing;
                        _slotEyes.Position = slotLeftStart;
                        _slotEyes.Update(frameTime);

                        slotLeftStart.Y += vertSpacing;
                        _slotOuter.Position = slotLeftStart;
                        _slotOuter.Update(frameTime);

                        slotLeftStart.Y += vertSpacing;
                        _slotHands.Position = slotLeftStart;
                        _slotHands.Update(frameTime);

                        slotLeftStart.Y += vertSpacing;
                        _slotFeet.Position = slotLeftStart;
                        _slotFeet.Update(frameTime);

                        //Right Side - mask, ears, inner, belt, back
                        slotRightStart.Y += vertSpacing;
                        _slotEars.Position = slotRightStart;
                        _slotEars.Update(frameTime);

                        slotRightStart.Y += vertSpacing;
                        _slotInner.Position = slotRightStart;
                        _slotInner.Update(frameTime);

                        slotRightStart.Y += vertSpacing;
                        _slotBelt.Position = slotRightStart;
                        _slotBelt.Update(frameTime);

                        slotRightStart.Y += vertSpacing;
                        _slotBack.Position = slotRightStart;
                        _slotBack.Update(frameTime);

                        if (_inventory != null)
                        {
                            _inventory.Position = new Vector2i(Position.X + 12, Position.Y + 315);
                            _inventory.Update(frameTime);
                        }
                        break;

                        #endregion
                    }
                case (2): //Health tab
                    {
                        #region Status

                        var resLinePos = new Vector2i(Position.X + 35, Position.Y + 70);

                        const int spacing = 8;

                        _ResBlunt.Position = resLinePos;
                        resLinePos.Y += _ResBlunt.ClientArea.Height + spacing;
                        _ResBlunt.Update(frameTime);

                        _ResPierce.Position = resLinePos;
                        resLinePos.Y += _ResPierce.ClientArea.Height + spacing;
                        _ResPierce.Update(frameTime);

                        _ResSlash.Position = resLinePos;
                        resLinePos.Y += _ResSlash.ClientArea.Height + spacing;
                        _ResSlash.Update(frameTime);

                        _ResBurn.Position = resLinePos;
                        resLinePos.Y += _ResBurn.ClientArea.Height + spacing;
                        _ResBurn.Update(frameTime);

                        _ResFreeze.Position = resLinePos;
                        resLinePos.Y += _ResFreeze.ClientArea.Height + spacing;
                        _ResFreeze.Update(frameTime);

                        _ResShock.Position = resLinePos;
                        resLinePos.Y += _ResShock.ClientArea.Height + spacing;
                        _ResShock.Update(frameTime);

                        _ResTox.Position = resLinePos;
                        _ResTox.Update(frameTime);

                        break;

                        #endregion
                    }
            }
            //Needs to update even when its not on the crafting tab so it continues to count.
        }

        public override void Dispose()
        {
            //TODO dispose me
            base.Dispose();
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (_showTabbedWindow)
            {
                if (_comboClose.MouseDown(e)) return true;
                if (_tabEquip.MouseDown(e)) return true;
                if (_tabHealth.MouseDown(e)) return true;

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
                }
            }
            return false;
        }

        private void SendSwitchHandTo(InventoryLocation hand)
        {
            Entity playerEntity = _playerManager.ControlledEntity;
            var equipComponent = (HumanHandsComponent) playerEntity.GetComponent(ComponentFamily.Hands);
            equipComponent.SendSwitchHands(hand);
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            var mouseAABB = new Vector2i(e.X, e.Y);

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

                        if (_comboBg.GetLocalBounds().Contains(mouseAABB.X, mouseAABB.Y) && _userInterfaceManager.DragInfo.IsEntity &&
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
            }

            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
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
            }
        }

        public override bool MouseWheelMove(MouseWheelEventArgs e)
        {
            if (_currentTab == 1 || _currentTab == 3)
            {
                if (_inventory.MouseWheelMove(e)) return true;
            }
            return false;
        }
    }
}
