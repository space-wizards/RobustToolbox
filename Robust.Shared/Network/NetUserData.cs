using System.Collections.Immutable;
using System.Text;
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

        public ImmutableArray<byte> HWId { get; init; }

        /// <summary>
        /// Unique identifiers for a client's computer, account and connection.
        /// </summary>
        /// <remarks>
        /// If any of these values match between two connections,
        /// it means the auth server believes them to be the same user.
        /// </remarks>
        public ImmutableArray<ImmutableArray<byte>> ModernHWIds { get; init; }

        /// <summary>
        /// A trust value that reports the auth server's estimate of how likely this user is to be a malicious/suspicious account.
        /// </summary>
        /// <remarks>
        /// A value of 0.5 can be considered "neutral", 1 being "fully trusted".
        /// </remarks>
        public float Trust { get; init; }

        public NetUserData(NetUserId userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }

        public sealed override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("NetUserData"); // type name
            stringBuilder.Append(" { ");
            if ((this with { HWId = default }).PrintMembers(stringBuilder))
            {
                stringBuilder.Append(' ');
            }
            stringBuilder.Append('}');
            return stringBuilder.ToString();
        }
    }
}
