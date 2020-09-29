namespace Robust.Shared.Network
{
    public enum LoginType : byte
    {
        /// <summary>
        ///     This player is not logged in and as soon as they disconnect their data will be gone, probably.
        /// </summary>
        Guest = 0,

        /// <summary>
        ///     This player is properly logged in with an auth account.
        /// </summary>
        LoggedIn = 1,

        /// <summary>
        ///     This player is not logged in but their username does have a static user ID assigned.
        /// </summary>
        GuestAssigned = 2
    }

    public static class LoginTypeExt
    {
        public static bool HasStaticUserId(this LoginType type)
        {
            return type == LoginType.LoggedIn || type == LoginType.GuestAssigned;
        }
    }
}
