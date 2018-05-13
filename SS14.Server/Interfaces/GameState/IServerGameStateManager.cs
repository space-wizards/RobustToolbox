using System.Collections.Generic;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;

namespace SS14.Server.Interfaces.GameState
{
    public interface IServerGameStateManager
    {
        void Initialize();
        void SendGameStateUpdate();
    }
}
