using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.Serialization;

namespace SS14.Shared.Network
{
    [Serializable, NetSerializable]
    public struct NetSessionId : IEquatable<NetSessionId>
    {
        public readonly string Username;

        public NetSessionId(string name)
        {
            Username = name;
        }

        public override bool Equals(object obj)
        {
            return obj is NetSessionId && Equals((NetSessionId)obj);
        }

        public bool Equals(NetSessionId other)
        {
            return Username == other.Username;
        }

        public override int GetHashCode()
        {
            return -182246463 + EqualityComparer<string>.Default.GetHashCode(Username);
        }

        public override string ToString()
        {
            return Username;
        }

        public static bool operator ==(NetSessionId id1, NetSessionId id2)
        {
            return id1.Equals(id2);
        }

        public static bool operator !=(NetSessionId id1, NetSessionId id2)
        {
            return !(id1 == id2);
        }
    }
}
