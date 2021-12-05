using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
        /// Entity UID that this session is represented by in the world, if any.
        /// </summary>
        EntityUid? AttachedEntity { get; }

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

        /// <summary>
        /// The current network channel for this player.
        /// </summary>
        /// <remarks>
        /// On the Server every player has a network channel,
        /// on the Client only the LocalPlayer has a network channel.
        /// </remarks>
        INetChannel ConnectedClient { get; }

        /// <summary>
        ///     Porting convenience for admin commands which use such logic as "at the player's feet", etc: the transform component of the attached entity.
        /// </summary>
        TransformComponent? AttachedEntityTransform => IoCManager.Resolve<IEntityManager>().GetComponentOrNull<TransformComponent>(AttachedEntity ?? default);
    }
}
