using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Robust.Shared.Physics
{

    public partial class DynamicTree
    {

        public readonly struct Proxy : IEquatable<Proxy>, IComparable<Proxy>
        {

            private readonly int _value;

            public static Proxy Free
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(-1);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Proxy(int v) => _value = v;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(Proxy other)
                => _value == other._value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int CompareTo(Proxy other)
                => _value.CompareTo(other._value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(object? obj)
                => obj is Proxy other && Equals(other);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() => _value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator int(Proxy n) => n._value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static explicit operator Proxy(int v) => new(v);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Proxy a, Proxy b) => a._value == b._value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Proxy a, Proxy b) => a._value != b._value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator >(Proxy a, Proxy b) => a._value > b._value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator <(Proxy a, Proxy b) => a._value < b._value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator >=(Proxy a, Proxy b) => a._value >= b._value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator <=(Proxy a, Proxy b) => a._value <= b._value;

            public override string ToString()
                => _value.ToString();

        }

    }

}
