using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Interfaces.Network;

namespace Robust.Server.Interfaces.GameState
{
    public interface IServerGameStateManager
    {
        void Initialize();
        void SendGameStateUpdate();
    }
}
