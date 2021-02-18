using Robust.Shared.Network;

namespace Robust.Shared.Players
{
    /// <summary>
    ///     Basic info about a player session.
    /// </summary>
    public interface IBaseSession
    {
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
