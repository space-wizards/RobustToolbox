using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Player
{
    internal sealed class PlayerSession : ICommonSession
    {
        internal SessionStatus Status { get; set; } = SessionStatus.Connecting;

        /// <inheritdoc />
        SessionStatus ICommonSession.Status
        {
            get => this.Status;
            set => this.Status = value;
        }

        /// <inheritdoc />
        [ViewVariables]
        public EntityUid? AttachedEntity { get; set; }

        /// <inheritdoc />
        [ViewVariables]
        public NetUserId UserId { get; }

        [ViewVariables]
        internal string Name { get; set; } = "<Unknown>";

        /// <inheritdoc />
        string ICommonSession.Name
        {
            get => this.Name;
            set => this.Name = value;
        }

        [ViewVariables]
        internal short Ping { get; set; }

        /// <inheritdoc />
        [ViewVariables]
        public INetChannel ConnectedClient { get; internal set; } = null!;

        /// <inheritdoc />
        short ICommonSession.Ping
        {
            get => this.Ping;
            set => this.Ping = value;
        }

        /// <summary>
        ///     Creates an instance of a PlayerSession.
        /// </summary>
        public PlayerSession(NetUserId user)
        {
            UserId = user;
        }
    }
}
