using System;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Robust.Client.Player
{

    internal sealed class PlayerSession : IPlayerSession
    {
        /// <inheritdoc />
        public SessionStatus Status { get; set; } = SessionStatus.Connecting;

        public IEntity? AttachedEntity { get; set; }

        /// <inheritdoc />
        public NetUserId UserId { get; }

        /// <inheritdoc cref="IPlayerSession" />
        public string Name { get; set; } = "<Unknown>";

        /// <inheritdoc />
        public short Ping { get; set; }

        /// <summary>
        ///     Creates an instance of a PlayerSession.
        /// </summary
        public PlayerSession(NetUserId user)
        {
            UserId = user;
        }
    }
}
