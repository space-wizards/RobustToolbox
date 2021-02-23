using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Robust.Shared.Maths
{
    [PublicAPI]
    public struct Thickness : IEquatable<Thickness>
    {
        public float Left;
        public float Top;
        public float Right;
        public float Bottom;

        public readonly float SumHorizontal => Left + Right;
        public readonly float SumVertical => Top + Bottom;

        public Thickness(float uniform)
        {
            Left = uniform;
            Top = uniform;
            Right = uniform;
            Bottom = uniform;
        }

        public Thickness(float horizontal, float vertical)
        {
            Left = horizontal;
            Right = horizontal;
            Top = vertical;
            Bottom = vertical;
        }

        public Thickness(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public readonly bool Equals(Thickness other)
        {
            return Equals(in other);
        }

        public readonly UIBox2 Inflate(in UIBox2 box)
        {
            return new(
                box.Left - Left,
                box.Top - Top,
                box.Right + Right,
                box.Bottom + Bottom);
        }

        public readonly Vector2 Inflate(in Vector2 size)
        {
            return (size.X + SumHorizontal, size.Y + SumVertical);
        }

        public readonly UIBox2 Deflate(in UIBox2 box)
        {
            var left = box.Left + Left;
            var top = box.Top + Top;
            return new(
                left,
                top,
                // Avoid inverse boxes if the margins are larger than the box.
                Math.Max(left, box.Right - Right),
                Math.Max(top, box.Bottom - Bottom));
        }

        public readonly Vector2 Deflate(in Vector2 size)
        {
            return Vector2.ComponentMax(
                Vector2.Zero,
                (size.X - SumHorizontal, size.Y - SumVertical));
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public readonly bool Equals(in Thickness other)
        {
            return Left == other.Left &&
                   Top == other.Top &&
                   Right == other.Right &&
                   Bottom == other.Bottom;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is Thickness other && Equals(other);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Left, Top, Right, Bottom);
        }

        public static bool operator ==(in Thickness left, in Thickness right)
        {
            return left.Equals(in right);
        }

        public static bool operator !=(in Thickness left, in Thickness right)
        {
            return !left.Equals(in right);
        }

        public override string ToString()
        {
            return $"{Left},{Top},{Right},{Bottom}";
        }

        public readonly void Deconstruct(out float left, out float top, out float right, out float bottom)
        {
            left = Left;
            top = Top;
            right = Right;
            bottom = Bottom;
        }
    }
}
