using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Robust.Shared.Players
{
    /// <summary>
    ///     Common info between client and server sessions.
    /// </summary>
    public interface ICommonSession
    {
        /// <summary>
        ///     Status of the session.
        /// </summary>
        SessionStatus Status { get; set; }

        IEntity? AttachedEntity { get; }

        /// <summary>
        ///     The UID of this session.
        /// </summary>
        NetUserId UserId { get; }

        /// <summary>
        ///     Current name of this player.
        /// </summary>
        string Name { get; }
    }
}
