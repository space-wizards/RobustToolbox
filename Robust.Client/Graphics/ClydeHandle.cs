using System;
using Robust.Shared.Graphics;

namespace Robust.Client.Graphics;

internal readonly struct ClydeHandle : IEquatable<ClydeHandle>, IClydeHandle
{
    public ClydeHandle(long value)
    {
        Value = value;
    }

    public long Value { get; }

    public static explicit operator ClydeHandle(long x)
    {
        return new(x);
    }

    public static explicit operator long(ClydeHandle h)
    {
        return h.Value;
    }

    public bool Equals(ClydeHandle other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ClydeHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ClydeHandle left, ClydeHandle right)
    {
        return left.Value == right.Value;
    }

    public static bool operator !=(ClydeHandle left, ClydeHandle right)
    {
        return left.Value != right.Value;
    }

    public override string ToString()
    {
        return $"ClydeHandle {Value}";
    }
}
