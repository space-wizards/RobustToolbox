namespace Robust.Shared.Network
{
    /// <summary>
    /// Possible modes for the authentication support on the server.
    /// </summary>
    public enum AuthMode : byte
    {
        /// <summary>
        /// Authentication is allowed but not required.
        /// </summary>
        /// <remarks>
        /// Unauthenticated clients get a "guest@" or "localhost@" prefix to avoid conflict with authenticated clients.
        /// </remarks>
        Optional = 0,

        /// <summary>
        /// Authenticated is required to join the server.
        /// </summary>
        /// <remarks>
        /// Unauthenticated clients are still allowed for localhost connections,
        /// but only if CVar <c>auth.allowlocal</c> is true.
        /// </remarks>
        Required = 1,

        /// <summary>
        /// Authentication is fully disabled, and even clients capable of authenticating will not authenticate.
        /// </summary>
        /// <remarks>
        /// This disables any sort of "guest@" or "localhost@" prefix for unauthenticated users.
        /// This may result in confusing mingling of database entries, if actively switched between on the same server.
        /// </remarks>
        Disabled = 2
    }
}
