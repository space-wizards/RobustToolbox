using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Network
{
    [Serializable, NetSerializable]
    public struct NetUserId : IEquatable<NetUserId>
    {
        public readonly Guid UserId;

        public NetUserId(Guid userId)
        {
            UserId = userId;
        }

        public override bool Equals(object? obj) =>
        obj switch {
            Guid id => Equals(id),
            NetUserId id => Equals(id),
            _ => false,
        };

        public bool Equals(NetUserId other) => UserId == other.UserId;

        public override int GetHashCode() => UserId.GetHashCode();

        public override string ToString() => UserId.ToString();

        public static bool operator ==(NetUserId id1, NetUserId id2) => id1.Equals(id2);

        public static bool operator !=(NetUserId id1, NetUserId id2) => !(id1 == id2);

        public static implicit operator Guid(NetUserId id) => id.UserId;
        public static explicit operator NetUserId(Guid id) => new(id);
    }
}
