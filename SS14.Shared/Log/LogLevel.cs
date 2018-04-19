namespace SS14.Shared.Log
{
    /// <remarks>
    ///     The value associated with the level determines the order in which they are filtered,
    ///     Under the default <see cref="LogManager"/>.
    /// </remarks>
    public enum LogLevel
    {
        /// <summary>
        ///     Diagnostic information usually only necessary when something broke.
        /// </summary>
        Debug = 1,

        /// <summary>
        ///     General info that can confirm that something is working.
        /// </summary>
        Info = 2,

        /// <summary>
        ///     Issues that can easily be worked around but should still be fixed.
        /// </summary>
        Warning = 3,

        /// <summary>
        ///     Errors that need fixing and are probably gonna break something.
        /// </summary>
        Error = 4,

        /// <summary>
        ///     Errors that are REALLY BAD and break EVERYTHING.
        /// </summary>
        Fatal = 5
    }
}
