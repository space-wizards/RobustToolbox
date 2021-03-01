using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Robust.Shared.Players
{
    /// <summary>
    /// Common info between client and server sessions.
    /// </summary>
    public interface ICommonSession
    {
        /// <summary>
        /// Status of the session.
        /// </summary>
        SessionStatus Status { get; internal set; }

        /// <summary>
        /// Entity that this session is represented by in the world, if any.
        /// </summary>
        IEntity? AttachedEntity { get; }

        /// <summary>
        /// Entity UID that this session is represented by in the world, if any.
        /// </summary>
        EntityUid? AttachedEntityUid { get; }

        /// <summary>
        /// The UID of this session.
        /// </summary>
        NetUserId UserId { get; }

        /// <summary>
        /// Current name of this player.
        /// </summary>
         string Name { get; internal set; }

        /// <summary>
        /// Current connection latency of this session from the server to their client.
        /// </summary>
        short Ping { get; internal set; }
    }
}
