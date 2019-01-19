using SS14.Shared.Network;

namespace SS14.Shared.Players
{
    /// <summary>
    ///     Basic info about a player session.
    /// </summary>
    public interface IBaseSession
    {
        /// <summary>
        ///     The UID of this session.
        /// </summary>
        NetSessionId SessionId { get; }

        /// <summary>
        ///     Current name of this player.
        /// </summary>
        string Name { get; }
    }
}
