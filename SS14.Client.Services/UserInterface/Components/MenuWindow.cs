using Lidgren.Network;
using SFML.System;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Services.State.States;
using SS14.Shared;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class MenuWindow : Window
    {
        private readonly INetworkManager _netMgr = IoCManager.Resolve<INetworkManager>();
        private readonly IPlacementManager _placeMgr = IoCManager.Resolve<IPlacementManager>();
        private readonly IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        private readonly IResourceManager _resMgr = IoCManager.Resolve<IResourceManager>();
        private readonly IStateManager _stateManager = IoCManager.Resolve<IStateManager>();
        private readonly IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();

        private readonly Button button_actions;
        private readonly Button button_entity;
        private readonly Button button_quit;
        private readonly Button button_tile;

        public MenuWindow() : base("Menu", new Vector2i(140, 130), IoCManager.Resolve<IResourceManager>())
        {
            Position = new Vector2i((int) (CluwneLib.CurrentRenderTarget.Size.X/2f) - (int) (ClientArea.Width/2f),
                                 (int) (CluwneLib.CurrentRenderTarget.Size.Y/2f) - (int) (ClientArea.Height/2f));

            button_actions = new Button("Player Actions", _resMgr);
            button_actions.Clicked += button_actions_Clicked;
            button_actions.Position = new Vector2i(5, 5);
            button_actions.Update(0);
            components.Add(button_actions);

            button_entity = new Button("Spawn Entities", _resMgr);
            button_entity.Clicked += button_entity_Clicked;
            button_entity.Position = new Vector2i(5, button_actions.ClientArea.Bottom() + 5);
            button_entity.Update(0);
            components.Add(button_entity);

            button_tile = new Button("Spawn Tiles", _resMgr);
            button_tile.Clicked += button_tile_Clicked;
            button_tile.Position = new Vector2i(5, button_entity.ClientArea.Bottom() + 5);
            button_tile.Update(0);
            components.Add(button_tile);

            button_quit = new Button("Quit", _resMgr);
            button_quit.Clicked += button_quit_Clicked;
            button_quit.Position = new Vector2i(5, button_tile.ClientArea.Bottom() + 20);
            button_quit.Update(0);
            components.Add(button_quit);
        }

        private void button_quit_Clicked(Button sender)
        {
            _netMgr.Disconnect();
            _stateManager.RequestStateChange<MainScreen>();
            Dispose();
        }

        private void button_tile_Clicked(Button sender)
        {
            _userInterfaceManager.DisposeAllComponents<TileSpawnPanel>(); //Remove old ones.
            _userInterfaceManager.AddComponent(new TileSpawnPanel(new Vector2i(350, 410), _resMgr, _placeMgr));
            //Create a new one.
            Dispose();
        }

        private void button_entity_Clicked(Button sender)
        {
            _userInterfaceManager.DisposeAllComponents<EntitySpawnPanel>(); //Remove old ones.
            _userInterfaceManager.AddComponent(new EntitySpawnPanel(new Vector2i(350, 410), _resMgr, _placeMgr));
            //Create a new one.
            Dispose();
        }

        private void button_actions_Clicked(Button sender)
        {
            _userInterfaceManager.DisposeAllComponents<PlayerActionsWindow>(); //Remove old ones.
            var actComp = (PlayerActionComp) _playerManager.ControlledEntity.GetComponent(ComponentFamily.PlayerActions);
            if (actComp != null)
                _userInterfaceManager.AddComponent(new PlayerActionsWindow(new Vector2i(150, 150), _resMgr, actComp));
            //Create a new one.
            Dispose();
        }
    }
}
