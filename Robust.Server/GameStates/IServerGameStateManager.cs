using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Robust.Server.GameStates
{
    /// <summary>
    /// Engine service that provides creating and dispatching of game states.
    /// </summary>
    public interface IServerGameStateManager
    {
        /// <summary>
        /// One time initialization of the service.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Create and dispatch game states to all connected sessions.
        /// </summary>
        void SendGameStateUpdate();

        ushort TransformNetId { get; set; }

        Action<ICommonSession, GameTick, GameTick>? ClientAck { get; set; }

        Action<ICommonSession, GameTick, GameTick, EntityUid?>? ClientRequestFull { get; set; }
    }
}
