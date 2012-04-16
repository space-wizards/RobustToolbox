using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Network;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13_Shared;
using System;
using System.Drawing;
using CGO;
using ClientInterfaces;
using ClientInterfaces.Player;
using ClientInterfaces.Resource;
using ClientInterfaces.UserInterface;
using ClientInterfaces.Network;
using ClientInterfaces.State;
using ClientInterfaces.Placement;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS13_Shared.GO;
using SS13_Shared;
using SS13.IoC;
using GorgonLibrary.InputDevices;
using ClientServices.Helpers;
using ClientServices.State.States;


namespace ClientServices.UserInterface.Components
{
    class MenuWindow : Window
    {
        IResourceManager _resMgr = IoCManager.Resolve<IResourceManager>();
        IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
        IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        IPlacementManager _placeMgr = IoCManager.Resolve<IPlacementManager>();
        INetworkManager _netMgr = IoCManager.Resolve<INetworkManager>();
        IStateManager _stateManager = IoCManager.Resolve<IStateManager>();

        Button button_actions;
        Button button_entity;
        Button button_tile;
        Button button_admin;
        Button button_quit;

        public MenuWindow()
            : base("Menu", new System.Drawing.Size(140, 130), IoCManager.Resolve<IResourceManager>())
        {
            Position = new Point((int)(Gorgon.CurrentRenderTarget.Width / 2f) - (int)(ClientArea.Width / 2f), (int)(Gorgon.CurrentRenderTarget.Height / 2f) - (int)(ClientArea.Height / 2f));

            button_actions = new Button("Player Actions", _resMgr);
            button_actions.Clicked += new Button.ButtonPressHandler(button_actions_Clicked);
            button_actions.Position = new System.Drawing.Point(5, 5);
            button_actions.Update(0);
            components.Add(button_actions);

            button_entity = new Button("Spawn Entities", _resMgr);
            button_entity.Clicked += new Button.ButtonPressHandler(button_entity_Clicked);
            button_entity.Position = new System.Drawing.Point(5, button_actions.ClientArea.Bottom + 5);
            button_entity.Update(0);
            components.Add(button_entity);

            button_tile = new Button("Spawn Tiles", _resMgr);
            button_tile.Clicked += new Button.ButtonPressHandler(button_tile_Clicked);
            button_tile.Position = new System.Drawing.Point(5, button_entity.ClientArea.Bottom + 5);
            button_tile.Update(0);
            components.Add(button_tile);

            button_admin = new Button("Admin Panel", _resMgr);
            button_admin.Clicked += new Button.ButtonPressHandler(button_admin_Clicked);
            button_admin.Position = new System.Drawing.Point(5, button_tile.ClientArea.Bottom + 5);
            button_admin.Update(0);
            components.Add(button_admin);

            button_quit = new Button("Quit", _resMgr);
            button_quit.Clicked += new Button.ButtonPressHandler(button_quit_Clicked);
            button_quit.Position = new System.Drawing.Point(5, button_admin.ClientArea.Bottom + 20);
            button_quit.Update(0);
            components.Add(button_quit);
        }

        void button_quit_Clicked(Button sender)
        {
            _netMgr.Disconnect();
            _stateManager.RequestStateChange<ConnectMenu>();
            this.Dispose();
        }

        void button_admin_Clicked(Button sender)
        {
            var message = _netMgr.CreateMessage();
            message.Write((byte)NetMessage.RequestAdminPlayerlist);
            _netMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
            this.Dispose();
        }

        void button_tile_Clicked(Button sender)
        {
            _userInterfaceManager.DisposeAllComponents<TileSpawnPanel>(); //Remove old ones.
            _userInterfaceManager.AddComponent(new TileSpawnPanel(new Size(350, 410), _resMgr, _placeMgr)); //Create a new one.
            this.Dispose();
        }

        void button_entity_Clicked(Button sender)
        {
            _userInterfaceManager.DisposeAllComponents<EntitySpawnPanel>(); //Remove old ones.
            _userInterfaceManager.AddComponent(new EntitySpawnPanel(new Size(350, 410), _resMgr, _placeMgr)); //Create a new one.
            this.Dispose();
        }

        void button_actions_Clicked(Button sender)
        {
            _userInterfaceManager.DisposeAllComponents<PlayerActionsWindow>(); //Remove old ones.
            PlayerActionComp actComp = (PlayerActionComp)_playerManager.ControlledEntity.GetComponent(ComponentFamily.PlayerActions);
            if (actComp != null)
                _userInterfaceManager.AddComponent(new PlayerActionsWindow(new Size(150, 150), _resMgr, actComp)); //Create a new one.
            this.Dispose();
        }
    }
}