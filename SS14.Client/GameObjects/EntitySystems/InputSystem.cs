using System;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;
using SS14.Shared.Players;

namespace SS14.Client.GameObjects.EntitySystems
{
    class InputSystem : EntitySystem
    {
        private readonly IPlayerCommandStates _cmdStates = new PlayerCommandStates();
        private readonly CommandBindMapping _bindMap = new CommandBindMapping();

        public IPlayerCommandStates CmdStates => _cmdStates;
        public ICommandBindMapping BindMap => _bindMap;


        public void HandleInputCommand(ICommonSession session, BoundKeyFunction function, FullInputCmdMessage message)
        {
            // set state, state change is updated regardless if it is locally bound
            _cmdStates.SetState(function, message.State);

            // handle local binds before sending off
            if (_bindMap.TryGetHandler(function, out var handler))
            {
                // local handlers can block sending over the network.
                if (handler.HandleCmdMessage(session, message))
                    return;
            }

            RaiseNetworkEvent(message);
        }
    }
}
