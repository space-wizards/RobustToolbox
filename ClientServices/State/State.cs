using System;
using System.Collections.Generic;
using ClientInterfaces.Configuration;
using ClientInterfaces.Input;
using ClientInterfaces.Map;
using ClientInterfaces.Network;
using ClientInterfaces.Placement;
using ClientInterfaces.Player;
using ClientInterfaces.Resource;
using ClientInterfaces.State;
using ClientInterfaces.UserInterface;

namespace ClientServices.State
{
    public abstract class State
    {
        protected readonly IConfigurationManager ConfigurationManager;
        protected readonly IKeyBindingManager KeyBindingManager;
        protected readonly IMapManager MapManager;
        protected readonly INetworkManager NetworkManager;
        protected readonly IPlacementManager PlacementManager;
        protected readonly IPlayerManager PlayerManager;
        protected readonly IResourceManager ResourceManager;
        protected readonly IStateManager StateManager;
        protected readonly IUserInterfaceManager UserInterfaceManager;

        protected State(IDictionary<Type, object> managers)
        {
            StateManager = (IStateManager) managers[typeof (IStateManager)];
            NetworkManager = (INetworkManager) managers[typeof (INetworkManager)];
            ResourceManager = (IResourceManager) managers[typeof (IResourceManager)];
            UserInterfaceManager = (IUserInterfaceManager) managers[typeof (IUserInterfaceManager)];
            MapManager = (IMapManager) managers[typeof (IMapManager)];
            PlayerManager = (IPlayerManager) managers[typeof (IPlayerManager)];
            ConfigurationManager = (IConfigurationManager) managers[typeof (IConfigurationManager)];
            PlacementManager = (IPlacementManager) managers[typeof (IPlacementManager)];
            KeyBindingManager = (IKeyBindingManager) managers[typeof (IKeyBindingManager)];
        }
    }
}