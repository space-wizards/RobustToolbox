using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     An identifier for a single OS window on the client. See <c>IClydeWindow</c> in the client project.
    /// </summary>
    [Serializable, NetSerializable]
    public readonly struct WindowId : IEquatable<WindowId>
    {
        /// <summary>
        ///     An invalid, default value. Does not represent any actual window.
        /// </summary>
        public static readonly WindowId Invalid = default;

        /// <summary>
        ///     Always the ID of the main game window.
        /// </summary>
        public static readonly WindowId Main = new(1);

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
