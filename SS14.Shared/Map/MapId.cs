using System;

namespace SS14.Shared.Map
{
    [Serializable]
    public struct MapId : IEquatable<MapId>
    {
        public static readonly MapId Nullspace = new MapId(0);

        private readonly int _value;
        
        public MapId(int value)
        {
            _value = value;
        }

        /// <inheritdoc />
        public bool Equals(MapId other)
        {
            return _value == other._value;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MapId id && Equals(id);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _value;
        }
        
        public static bool operator ==(MapId a, MapId b)
        {
            return a._value == b._value;
        }

        public static bool operator !=(MapId a, MapId b)
        {
            return !(a == b);
        }

        public static explicit operator int(MapId self)
        {
            return self._value;
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }
}
