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
