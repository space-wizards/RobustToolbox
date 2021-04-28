using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Map
{
    [Serializable, NetSerializable]
    public readonly struct WindowId : IEquatable<WindowId>
    {
        internal readonly int Value;

        public WindowId(int value)
        {
            Value = value;
        }

        public bool Equals(WindowId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is WindowId id && Equals(id);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(WindowId a, WindowId b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(WindowId a, WindowId b)
        {
            return !(a == b);
        }

        public static explicit operator int(WindowId self)
        {
            return self.Value;
        }

        public override string ToString()
        {
            return $"Window {Value}";
        }
    }
}
