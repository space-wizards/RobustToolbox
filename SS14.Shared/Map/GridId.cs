using System;
using SS14.Shared.Serialization;

namespace SS14.Shared.Map
{
    [Serializable, NetSerializable]
    public struct GridId : IEquatable<GridId>
    {
        public static readonly GridId Nullspace = new GridId(0);

        internal readonly int Value;

        public GridId(int value)
        {
            Value = value;
        }

        /// <inheritdoc />
        public bool Equals(GridId other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is GridId id && Equals(id);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(GridId a, GridId b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(GridId a, GridId b)
        {
            return !(a == b);
        }

        public static explicit operator int(GridId self)
        {
            return self.Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
