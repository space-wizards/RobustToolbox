using System;

namespace SS14.Shared.Configuration
{
    /// <summary>
    /// Extra flags for changing the behavior of a config var.
    /// </summary>
    [Flags]
    public enum CVarFlags
    {
        /// <summary>
        /// No special flags.
        /// </summary>
        NONE,

        /// <summary>
        /// Debug vars that are considered 'cheating' to change.
        /// </summary>
        CHEAT,

        /// <summary>
        /// Only the server can change this variable.
        /// </summary>
        SERVER,

        /// <summary>
        /// This can only be changed when not connected to a server.
        /// </summary>
        NOT_CONNECTED,

        /// <summary>
        /// Changing this var syncs between clients and server.
        /// </summary>
        REPLICATED,

        /// <summary>
        /// Non-default values are saved to the configuration file.
        /// </summary>
        ARCHIVE,

        /// <summary>
        /// Changing this var on the server notifies all clients, does nothing client-side.
        /// </summary>
        NOTIFY
    }
}
