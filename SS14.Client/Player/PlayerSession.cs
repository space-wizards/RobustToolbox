using SS14.Shared.Enums;
using SS14.Shared.Network;

namespace SS14.Client.Player
{

    public class PlayerSession : IPlayerSession
    {
        /// <inheritdoc />
        public SessionStatus Status { get; set; } = SessionStatus.Connecting;

        /// <inheritdoc />
        public NetSessionId SessionId { get; }

        /// <inheritdoc cref="IPlayerSession" />
        public string Name { get; set; } = "<Unknown>";

        /// <inheritdoc />
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
