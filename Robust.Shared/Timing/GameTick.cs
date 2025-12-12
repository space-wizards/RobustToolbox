using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Timing
{
    /// <summary>
    ///     Represents a tick at some point in time over the game's runtime.
    ///     The actual span of time a tick <b>is</b> depends on the <see cref="F:Robust.Shared.CVars.NetTickrate"/>.
    /// </summary>
    /// <remarks>
    ///     While the game does use ticks for some timing, they are always an arbitrary time step. If you need to
    ///     measure exact passage of time, you should use <see cref="TimeSpan"/>s instead in your reference frame
    ///     (client, server, etc.) from <see cref="IGameTiming"/>.<br/>
    ///     <br/>
    ///     Ticks are appropriate for thinking purely relative to previous game ticks, for example tracking the last
    ///     time modification occurred on a component for networking purposes.<br/>
    ///     <br/>
    ///     The game can theoretically run out of ticks. At the default tickrate, this is after approximately 4.5 years.
    ///     It is recommended to reboot the game before that happens.
    /// </remarks>
    /// <seealso cref="IGameTiming"/>
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
