using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Network;

namespace Robust.Shared.Players
{
    public interface ISharedPlayerManager
    {
        /// <summary>
        /// Player sessions with a remote endpoint.
        /// </summary>
        IEnumerable<ICommonSession> NetworkedSessions { get; }

        IEnumerable<ICommonSession> Sessions { get; }

        /// <summary>
        ///     Number of players currently connected to this server.
        /// </summary>
        int PlayerCount { get; }

        /// <summary>
        ///     Maximum number of players that can connect to this server at one time.
        /// </summary>
        int MaxPlayers { get; }
    }
}
