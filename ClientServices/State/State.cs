using System;
using System.Collections.Generic;
using ClientInterfaces;
using ClientInterfaces.Input;
using ClientInterfaces.Network;
using ClientInterfaces.Placement;
using ClientInterfaces.Player;
using ClientInterfaces.State;
using ClientInterfaces.UserInterface;

namespace ClientServices.State
{
    public abstract class State
    {
        protected readonly IStateManager StateManager;
        protected readonly INetworkManager NetworkManager;
        protected readonly IResourceManager ResourceManager;
        protected readonly IUserInterfaceManager UserInterfaceManager;
        protected readonly IMapManager MapManager;
        protected readonly IPlayerManager PlayerManager;
        protected readonly IConfigurationManager ConfigurationManager;
        protected readonly IPlacementManager PlacementManager;
        protected readonly IKeyBindingManager KeyBindingManager;

        protected State(IDictionary<Type, object> managers)
        {
            StateManager = (IStateManager)managers[typeof(IStateManager)];
            NetworkManager = (INetworkManager)managers[typeof(INetworkManager)];
            ResourceManager = (IResourceManager)managers[typeof(IResourceManager)];
            UserInterfaceManager = (IUserInterfaceManager)managers[typeof(IUserInterfaceManager)];
            MapManager = (IMapManager)managers[typeof(IMapManager)];
            PlayerManager = (IPlayerManager)managers[typeof(IPlayerManager)];
            ConfigurationManager = (IConfigurationManager)managers[typeof(IConfigurationManager)];
            PlacementManager = (IPlacementManager)managers[typeof(IPlacementManager)];
            KeyBindingManager = (IKeyBindingManager)managers[typeof(IKeyBindingManager)];
        }
    }
}
