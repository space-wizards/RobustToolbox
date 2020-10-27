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

        public override bool Equals(object? obj)
        {
            return obj is NetUserId id && Equals(id);
        }

        public bool Equals(NetUserId other)
        {
            return UserId == other.UserId;
        }

        public override int GetHashCode()
        {
            return UserId.GetHashCode();
        }

        public override string ToString()
        {
            return UserId.ToString();
        }

        public static bool operator ==(NetUserId id1, NetUserId id2)
        {
            return id1.Equals(id2);
        }

        public static bool operator !=(NetUserId id1, NetUserId id2)
        {
            return !(id1 == id2);
        }
    }
}
