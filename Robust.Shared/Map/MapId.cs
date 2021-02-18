using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Map
{
    [Serializable, NetSerializable]
    public struct MapId : IEquatable<MapId>
    {
        public static readonly MapId Nullspace = new(0);

        internal readonly int Value;

        public MapId(int value)
        {
            Value = value;
        }

        /// <inheritdoc />
        public bool Equals(MapId other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MapId id && Equals(id);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(MapId a, MapId b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(MapId a, MapId b)
        {
            return !(a == b);
        }

        public static explicit operator int(MapId self)
        {
            return self.Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
