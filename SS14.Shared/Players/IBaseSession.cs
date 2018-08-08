namespace SS14.Shared.Players
{
    /// <summary>
    ///     Basic info about a player session.
    /// </summary>
    public interface IBaseSession
    {
        /// <summary>
        ///     Index of the session.
        /// </summary>
        PlayerIndex Index { get; }

        /// <summary>
        ///     Current name of this player.
        /// </summary>
        string Name { get; set; }
    }
}
