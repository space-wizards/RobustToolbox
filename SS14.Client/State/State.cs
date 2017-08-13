using SS14.Client.Interfaces.Input;
using SS14.Shared.Interfaces.Map;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.Interfaces.Configuration;
using System;
using System.Collections.Generic;
using SS14.Shared.Interfaces.Network;

namespace SS14.Client.State
{
    public abstract class State
    {
        protected readonly IConfigurationManager ConfigurationManager;
        protected readonly IKeyBindingManager KeyBindingManager;
        protected readonly IMapManager MapManager;
        protected readonly IClientNetManager NetworkManager;
        protected readonly IPlacementManager PlacementManager;
        protected readonly IPlayerManager PlayerManager;
        protected readonly IResourceCache ResourceCache;
        protected readonly IStateManager StateManager;
        protected readonly IUserInterfaceManager UserInterfaceManager;

        protected State(IDictionary<Type, object> managers)
        {
            StateManager = (IStateManager) managers[typeof (IStateManager)];
            NetworkManager = (IClientNetManager) managers[typeof (IClientNetManager)];
            ResourceCache = (IResourceCache) managers[typeof (IResourceCache)];
            UserInterfaceManager = (IUserInterfaceManager) managers[typeof (IUserInterfaceManager)];
            MapManager = (IMapManager) managers[typeof (IMapManager)];
            PlayerManager = (IPlayerManager) managers[typeof (IPlayerManager)];
            ConfigurationManager = (IConfigurationManager) managers[typeof (IConfigurationManager)];
            PlacementManager = (IPlacementManager) managers[typeof (IPlacementManager)];
            KeyBindingManager = (IKeyBindingManager) managers[typeof (IKeyBindingManager)];
        }
    }
}