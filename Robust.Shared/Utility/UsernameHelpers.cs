using System;
using AHelpers = Robust.Shared.AuthLib.UsernameHelpers;

namespace Robust.Shared.Utility
{
    [Obsolete("Use Robust.Shared.AuthLib.UsernameHelpers instead.")]
    public static class UsernameHelpers
    {
        /// <summary>
        ///     Checks whether a user name is valid.
        ///     If this is false, feel free to kick the person requesting it. Loudly.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>True if the name is acceptable, false otherwise.</returns>
        public static (bool, string? reason) IsNameValid(string name)
        {
            var valid = AHelpers.IsNameValid(name, out var reason);
            if (valid)
            {
                return (true, null);
            }

            return (false, reason.ToText());
        }
    }
}
