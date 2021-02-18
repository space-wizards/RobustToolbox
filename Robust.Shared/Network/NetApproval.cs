using System;

namespace Robust.Shared.Network
{
    public struct NetApproval
    {
        public bool IsApproved => _denyReason == null;

        public string DenyReason => _denyReason != null
            ? _denyReason!
            : throw new InvalidOperationException("This was not a denial.");

        private readonly string? _denyReason;

        private NetApproval(string? denyReason)
        {
            _denyReason = denyReason;
        }

        public static NetApproval Deny(string reason)
        {
            return new(reason);
        }

        public static NetApproval Allow()
        {
            return new(null);
        }
    }
}
