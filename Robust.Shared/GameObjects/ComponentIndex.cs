using System;

namespace Robust.Shared.GameObjects;

public readonly struct ComponentIndex : IEquatable<ComponentIndex>
{
    internal readonly int Value;

    internal ComponentIndex(int value)
    {
        Value = value;
    }

    public bool Equals(ComponentIndex other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ComponentIndex other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    public static bool operator ==(ComponentIndex left, ComponentIndex right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ComponentIndex left, ComponentIndex right)
    {
        return !left.Equals(right);
    }
}
