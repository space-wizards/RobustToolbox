namespace SS14.Shared.Log
{
    /// <remarks>
    /// The value associated with the level determines the order in which they are filtered,
    /// Under the default <see cref="LogManager"/>.
    /// </remarks>
    public enum LogLevel
    {
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }
}
