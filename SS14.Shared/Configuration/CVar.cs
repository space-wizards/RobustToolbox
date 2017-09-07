using System;

namespace SS14.Shared.Configuration
{
    /// <summary>
    /// Extra flags for changing the behavior of a config var.
    /// </summary>
    [Flags]
    public enum CVar
    {
        /// <summary>
        /// No special flags.
        /// </summary>
        NONE = 0,

        /// <summary>
        /// Debug vars that are considered 'cheating' to change.
        /// </summary>
        CHEAT = 1,

        /// <summary>
        /// Only the server can change this variable.
        /// </summary>
        SERVER = 2,

        /// <summary>
        /// This can only be changed when not connected to a server.
        /// </summary>
        NOT_CONNECTED = 4,

        /// <summary>
        /// Changing this var syncs between clients and server.
        /// </summary>
        REPLICATED = 8,

        /// <summary>
        /// Non-default values are saved to the configuration file.
        /// </summary>
        ARCHIVE = 16,

        /// <summary>
        /// Changing this var on the server notifies all clients, does nothing client-side.
        /// </summary>
        NOTIFY = 32
    }
}
