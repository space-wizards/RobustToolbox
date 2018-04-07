using SS14.Shared.GameStates;
using SS14.Shared.Network.Messages;
using System.Collections.Generic;

namespace SS14.Client.Interfaces.GameStates
{
    public interface IGameStateManager
    {
        void HandleFullStateMessage(MsgFullState message);
        void HandleStateUpdateMessage(MsgStateUpdate message);
    }
}
