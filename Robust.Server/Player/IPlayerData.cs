using Robust.Shared.Network;

namespace Robust.Server.Player
{
    /// <summary>
    ///     Stores player-specific data that is not lost upon reconnect.
    /// </summary>
    public interface IPlayerData
    {
        /// <summary>
        ///     The session ID of the player owning this data.
        /// </summary>
        NetUserId UserId { get; }

        /// <summary>
        ///     Custom field that content can assign anything to.
        ///     Go wild.
        /// </summary>
        object? ContentDataUncast { get; set; }
    }
}
