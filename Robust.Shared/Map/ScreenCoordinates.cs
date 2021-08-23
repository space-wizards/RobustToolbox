using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using JetBrains.Annotations;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     Contains the coordinates of a position on the rendering screen.
    /// </summary>
    [PublicAPI]
    [Serializable, NetSerializable]
    public readonly struct ScreenCoordinates : IEquatable<ScreenCoordinates>
    {
        /// <summary>
        ///     Position on the rendering screen.
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        ///     Screen position on the X axis.
        /// </summary>
        public float X => Position.X;

        /// <summary>
        ///     Screen position on the Y axis.
        /// </summary>
        public float Y => Position.Y;

        /// <summary>
        ///     The window which the coordinates are on.
        /// </summary>
        public readonly WindowId Window;

        /// <summary>
        ///     Constructs a new instance of <c>ScreenCoordinates</c>.
        /// </summary>
        /// <param name="position">Position on the rendering screen.</param>
        /// <param name="window">Window for the coordinates.</param>
        public ScreenCoordinates(Vector2 position, WindowId window)
        {
            Position = position;
            Window = window;
        }

        /// <summary>
        ///     Constructs a new instance of <c>ScreenCoordinates</c>.
        /// </summary>
        /// <param name="x">X axis of a position on the screen.</param>
        /// <param name="y">Y axis of a position on the screen.</param>
        /// <param name="window">Window for the coordinates.</param>
        public ScreenCoordinates(float x, float y, WindowId window)
        {
            Position = new Vector2(x, y);
            Window = window;
        }

        public bool IsValid => Window != WindowId.Invalid;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({Position.X}, {Position.Y}, W{Window.Value})";
        }

        /// <inheritdoc />
        public bool Equals(ScreenCoordinates other)
        {
            return Position.Equals(other.Position) && Window == other.Window;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is ScreenCoordinates other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Position, Window);
        }

        /// <summary>
        ///     Check for equality by value between two objects.
        /// </summary>
        public static bool operator ==(ScreenCoordinates a, ScreenCoordinates b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Check for inequality by value between two objects.
        /// </summary>
        public static bool operator !=(ScreenCoordinates a, ScreenCoordinates b)
        {
            return !a.Equals(b);
        }

        public void Deconstruct(out Vector2 pos, out WindowId window)
        {
            pos = Position;
            window = Window;
        }
    }
}
