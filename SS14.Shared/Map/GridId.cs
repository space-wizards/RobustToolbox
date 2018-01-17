using System;

namespace SS14.Shared.Map
{
    [Serializable]
    public struct GridId : IEquatable<GridId>
    {
        public static readonly GridId DefaultGrid = new GridId(0);

        private readonly int _value;

        public GridId(int value)
        {
            _value = value;
        }

        /// <inheritdoc />
        public bool Equals(GridId other)
        {
            return _value == other._value;
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
            return _value;
        }

        public static bool operator ==(GridId a, GridId b)
        {
            return a._value == b._value;
        }

        public static bool operator !=(GridId a, GridId b)
        {
            return !(a == b);
        }

        public static explicit operator int(GridId self)
        {
            return self._value;
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }
}
