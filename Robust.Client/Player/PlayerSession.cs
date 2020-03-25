using Robust.Shared.Enums;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Network;

namespace Robust.Client.Player
{

    internal sealed class PlayerSession : IPlayerSession
    {
        /// <inheritdoc />
        public SessionStatus Status { get; set; } = SessionStatus.Connecting;

        public IEntity AttachedEntity { get; set; }

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
