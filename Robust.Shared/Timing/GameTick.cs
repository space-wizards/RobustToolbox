using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Timing
{
    /// <summary>
    ///     Wraps a game tick value.
    /// </summary>
    [Serializable, NetSerializable]
    public readonly struct GameTick : IEquatable<GameTick>, IComparable<GameTick>
    {
        public static readonly GameTick Zero = new(0);
        public static readonly GameTick First = new(1);
        public static readonly GameTick MaxValue = new(uint.MaxValue);

        public readonly uint Value;

        /// <summary>
        ///     Constructs a new instance of <c>GameTick</c>.
        /// </summary>
        /// <param name="value"></param>
        public GameTick(uint value)
        {
            Value = value;
        }

        /// <inheritdoc />
        public bool Equals(GameTick other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is GameTick other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (int) Value;
        }

        /// <summary>
        ///     Check for equality by value between two objects.
        /// </summary>
        public static bool operator ==(GameTick a, GameTick b)
        {
            return a.Value == b.Value;
        }

        /// <summary>
        ///     Check for inequality by value between two objects.
        /// </summary>
        public static bool operator !=(GameTick a, GameTick b)
        {
            return a.Value != b.Value;
        }

        /// <inheritdoc />
        public int CompareTo(GameTick other)
        {
            return Value.CompareTo(other.Value);
        }

        public static bool operator >(GameTick a, GameTick b) => a.Value > b.Value;
        public static bool operator >=(GameTick a, GameTick b) => a.Value >= b.Value;
        public static bool operator <(GameTick a, GameTick b) => a.Value < b.Value;
        public static bool operator <=(GameTick a, GameTick b) => a.Value <= b.Value;

        public static GameTick operator +(GameTick a, uint b)
        {
            return new(a.Value + b);
        }

        public static GameTick operator -(GameTick a, uint b)
        {
            return new(a.Value - b);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
