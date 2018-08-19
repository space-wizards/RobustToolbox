using SS14.Shared.Enums;
using SS14.Shared.Network;
using SS14.Shared.Players;

namespace SS14.Client.Player
{
    /// <summary>
    ///     Client side session of a player.
    /// </summary>
    public class PlayerSession : ICommonSession
    {
        /// <summary>
        ///     Status of the session.
        /// </summary>
        public SessionStatus Status { get; set; } = SessionStatus.Connecting;

        public NetSessionId SessionId { get; }

        /// <summary>
        ///     Current name of this player.
        /// </summary>
        public string Name { get; set; } = "<Unknown>";

        /// <summary>
        ///     Current connection latency of this session from the server to their client.
        /// </summary>
        public short Ping { get; set; }

        /// <summary>
        ///     Creates an instance of a PlayerSession.
        /// </summary
        public PlayerSession(NetSessionId session)
        {
            SessionId = session;
        }
    }
}
