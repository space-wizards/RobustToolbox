using System;

namespace Robust.Client.Graphics
{
    internal struct ClydeHandle : IEquatable<ClydeHandle>
    {
        public ClydeHandle(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public static explicit operator ClydeHandle(int x)
        {
            return new ClydeHandle(x);
        }

        public static explicit operator int(ClydeHandle h)
        {
            return h.Value;
        }

        public bool Equals(ClydeHandle other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ClydeHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(ClydeHandle left, ClydeHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ClydeHandle left, ClydeHandle right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"ClydeHandle {Value}";
        }
    }
}
