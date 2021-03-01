using Robust.Shared.ViewVariables;

namespace Robust.Shared.Network
{
    /// <summary>
    ///     Contains data about players returned from auth server.
    /// </summary>
    public sealed record NetUserData
    {
        [ViewVariables]
        public NetUserId UserId { get; }
        [ViewVariables]
        public string UserName { get; }
        [ViewVariables]
        public string? PatronTier { get; init; }

        public NetUserData(NetUserId userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }
    }
}
