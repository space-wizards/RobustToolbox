using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Players;

namespace Robust.Client.Player
{
    internal sealed class PlayerSession : IPlayerSession
    {
        /// <inheritdoc />
        internal SessionStatus Status { get; set; } = SessionStatus.Connecting;

        /// <inheritdoc />
        SessionStatus ICommonSession.Status
        {
            get => this.Status;
            set => this.Status = value;
        }

        /// <inheritdoc />
        public IEntity? AttachedEntity { get; set; }

        /// <inheritdoc />
        public EntityUid? AttachedEntityUid => AttachedEntity?.Uid;

        /// <inheritdoc />
        public NetUserId UserId { get; }

        /// <inheritdoc cref="IPlayerSession" />
        internal string Name { get; set; } = "<Unknown>";

        /// <inheritdoc cref="IPlayerSession" />
        string ICommonSession.Name
        {
            get => this.Name;
            set => this.Name = value;
        }

        /// <inheritdoc />
        internal short Ping { get; set; }

        /// <inheritdoc />
        public INetChannel ConnectedClient { get; internal set; } = null!;

        /// <inheritdoc />
        short ICommonSession.Ping
        {
            get => this.Ping;
            set => this.Ping = value;
        }

        /// <summary>
        ///     Creates an instance of a PlayerSession.
        /// </summary
        public PlayerSession(NetUserId user)
        {
            UserId = user;
        }
    }
}
