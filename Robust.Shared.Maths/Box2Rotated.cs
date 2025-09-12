using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     This type contains a <see cref="Box2"/> and a rotation <see cref="Angle"/> in world space.
    /// </summary>
    [Serializable]
    public struct Box2Rotated : IEquatable<Box2Rotated>, ISpanFormattable
    {
        public Box2 Box;
        public Angle Rotation;

        /// <summary>
        /// The point about which the rotation occurs.
        /// </summary>
        public Vector2 Origin;

        /// <summary>
        ///     A 1x1 unit box with the origin centered and identity rotation.
        /// </summary>
        public static readonly Box2Rotated UnitCentered = new(Box2.UnitCentered, Angle.Zero, Vector2.Zero);

        public readonly Vector2 BottomRight => Origin + Rotation.RotateVec(Box.BottomRight - Origin);
        public readonly Vector2 TopLeft => Origin + Rotation.RotateVec(Box.TopLeft - Origin);
        public readonly Vector2 TopRight => Origin + Rotation.RotateVec(Box.TopRight - Origin);
        public readonly Vector2 BottomLeft => Origin + Rotation.RotateVec(Box.BottomLeft - Origin);
        public readonly Vector2 Center => Origin + Rotation.RotateVec((Box.BottomLeft + Box.TopRight)/2 - Origin);

        public readonly Matrix3x2 Transform
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // Equivalent to
                // Matrix3Helpers.CreateTransform(Origin - Rotation.RotateVec(Origin), Rotation)

                var angle = (float) Rotation;
                var sin = MathF.Sin(angle);
                var cos = MathF.Cos(angle);
                var cos1 = 1 - cos;
                var dx = cos1 * Origin.X + sin * Origin.Y;
                var dy = - sin * Origin.X + cos1 * Origin.Y;

                return new Matrix3x2
                {
                    M11 = cos,
                    M12 = sin,
                    M21 = -sin,
                    M22 = cos,
                    M31 = dx,
                    M32 = dy,
                };
            }
        }

        public Box2Rotated(Vector2 bottomLeft, Vector2 topRight)
            : this(new Box2(bottomLeft, topRight))
        {
        }

        public Box2Rotated(Box2 box)
            : this(box, 0)
        {
        }

        public Box2Rotated(Box2 box, Angle rotation)
            : this(box, rotation, Vector2.Zero)
        {
        }

        public Box2Rotated(Box2 box, Angle rotation, Vector2 origin)
        {
            Box = box;
            Rotation = rotation;
            Origin = origin;
        }

        /// <summary>
        /// Enlarges the box by the specified value.
        /// </summary>
        [Pure]
        public readonly Box2Rotated Enlarged(float value)
        {
            var box = Box.Enlarged(value);
            return new Box2Rotated(box, Rotation, Origin);
        }

        /// <summary>
        /// Calculates the smallest AABB that will encompass the rotated box. The AABB is in local space.
        /// </summary>
        public readonly Box2 CalcBoundingBox()
        {
            GetVertices(out var x, out var y);
            var aabb = SimdHelpers.GetAABB(x, y);
            return Unsafe.As<Vector128<float>, Box2>(ref aabb);
        }

        /// <summary>
        /// Applies the transformation to the box's corners and returns the coordinates in two simd vectors.
        /// </summary>
        /// <remarks>The corners are ordered clockwise, starting from what was the bottom left corner prior to the transformation.</remarks>
        /// <remarks>This is effectively a specialized variant of a <see cref="Matrix3Helpers"/> transform method that avoids having to use construct the matrix via <see cref="Transform"/></remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void GetVertices(out Vector128<float> x, out Vector128<float> y)
        {
            var boxVec = Unsafe.As<Box2, Vector128<float>>(ref Unsafe.AsRef(in Box));

            var originX = Vector128.Create(Origin.X);
            var originY = Vector128.Create(Origin.Y);

            var cos = Vector128.Create((float) Math.Cos(Rotation));
            var sin = Vector128.Create((float) Math.Sin(Rotation));

            var boxX = Vector128.Shuffle(boxVec, Vector128.Create(0, 2, 2, 0)) - originX;
            var boxY = Vector128.Shuffle(boxVec, Vector128.Create(1, 1, 3, 3)) - originY;

            x = boxX * cos - boxY * sin + originX;
            y = boxX * sin + boxY * cos + originY;
        }

        public readonly bool Contains(Vector2 worldPoint)
        {
            // Get the worldpoint in our frame of reference so we can do a faster AABB check.
            var localPoint = GetLocalPoint(worldPoint);
            return Box.Contains(localPoint);
        }

        /// <summary>
        /// Convert a point in world-space coordinates to our local coordinates.
        /// </summary>
        private readonly Vector2 GetLocalPoint(Vector2 point)
        {
            return Origin + (-Rotation).RotateVec(point - Origin);
        }

        #region Equality

        /// <inheritdoc />
        public readonly bool Equals(Box2Rotated other)
        {
            return Box.Equals(other.Box) && Rotation.Equals(other.Rotation) && Origin.Equals(other.Origin);
        }

        public readonly bool EqualsApprox(Box2Rotated other)
        {
            return Box.EqualsApprox(other.Box)
                   && Rotation.EqualsApprox(other.Rotation)
                   && Origin.EqualsApprox(other.Origin);
        }

        public readonly bool EqualsApprox(Box2Rotated other, double tolerance)
        {
            return Box.EqualsApprox(other.Box, tolerance)
                   && Rotation.EqualsApprox(other.Rotation, tolerance)
                   && Origin.EqualsApprox(other.Origin, tolerance);
        }

        /// <inheritdoc />
        public readonly override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Box2Rotated other && Equals(other);
        }

        /// <inheritdoc />
        public readonly override int GetHashCode()
        {
            unchecked
            {
                return (Box.GetHashCode() * 397) ^ Rotation.GetHashCode();
            }
        }

        /// <summary>
        ///     Check for equality by value between two <see cref="Box2Rotated"/>.
        /// </summary>
        public static bool operator ==(Box2Rotated a, Box2Rotated b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Check for inequality by value between two <see cref="Box2Rotated"/>.
        /// </summary>
        public static bool operator !=(Box2Rotated a, Box2Rotated b)
        {
            return !a.Equals(b);
        }

        #endregion

        /// <summary>
        ///     Returns a string representation of this type.
        /// </summary>
        public readonly override string ToString()
        {
            return $"{Box}, {Rotation}";
        }

        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString();
        }

        public readonly bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
        {
            return FormatHelpers.TryFormatInto(
                destination,
                out charsWritten,
                $"{Box}, {Rotation}");
        }
    }
}
