using System;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;

namespace SS14.Client.GameObjects.EntitySystems
{
    class InputSystem : EntitySystem
    {
        public CommandBindMapping BindMap { get; } = new CommandBindMapping();

        public void HandleInputCommand(BoundKeyFunction function, FullInputCmdMessage message)
        {
            // handle local binds before sending off
            if (BindMap.TryGetHandler(function, out var handler))
            {
                // local handlers can block sending over the network.
                if (handler.HandleCmdMessage(null, message))
                    return;
            }

            RaiseNetworkEvent(message);
        }
    }
}
