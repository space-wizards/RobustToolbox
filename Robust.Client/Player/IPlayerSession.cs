using Robust.Shared.Players;

namespace Robust.Client.Player
{
    /// <summary>
    ///     Client side session of a player.
    /// </summary>
    public interface IPlayerSession : ICommonSession
    {
        /// <summary>
        ///     Current name of this player.
        /// </summary>
        new string Name { get; set; }

        /// <summary>
        ///     Current connection latency of this session from the server to their client.
        /// </summary>
        short Ping { get; set; }
    }
}
