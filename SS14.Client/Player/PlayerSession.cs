using SS14.Shared;
using SS14.Shared.Players;

namespace SS14.Client.Player
{
    /// <summary>
    ///     Client side session of a player.
    /// </summary>
    public class PlayerSession
    {
        /// <summary>
        ///     Status of the session.
        /// </summary>
        public SessionStatus Status { get; set; } = SessionStatus.Zombie;

        /// <summary>
        ///     Index of the session.
        /// </summary>
        public PlayerIndex Index { get; }

        /// <summary>
        ///     Universally unique identifier for this session.
        /// </summary>
        public long Uuid { get; }

        /// <summary>
        ///     Current name of this player.
        /// </summary>
        public string Name { get; set; } = "<Unknown>";

        /// <summary>
        ///     Current connection latency of this session from the server to their client.
        /// </summary>
        public short Ping { get; set; }

        /// <summary>
        ///     Creates an instance of a PlayerSession, using the index and uuid passed to it.
        /// </summary>
        /// <param name="index">Index of the session.</param>
        /// <param name="uuid">Universally unique identifier for this session.</param>
        public PlayerSession(PlayerIndex index, long uuid)
        {
            Index = index;
            Uuid = uuid;
        }
    }
}
