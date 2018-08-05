using System;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;

namespace SS14.Client.GameObjects.EntitySystems
{
    class InputSystem : EntitySystem
    {
        public CommandBindMapping BindMap { get; } = new CommandBindMapping();

        public void HandleInputCommand(InputCmdMessage message)
        {
            //TODO: Make the BindMap work!

            RaiseNetworkEvent(message);
        }
    }
}
