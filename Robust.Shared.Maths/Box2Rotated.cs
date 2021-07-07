using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     This type contains a <see cref="Box2"/> and a rotation <see cref="Angle"/> in world space.
    /// </summary>
    [Serializable]
    public struct Box2Rotated : IEquatable<Box2Rotated>
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
        /// calculates the smallest AABB that will encompass the rotated box. The AABB is in local space.
        /// </summary>
        public readonly Box2 CalcBoundingBox()
        {
            if (Sse.IsSupported && NumericsHelpers.Enabled)
            {
                return CalcBoundingBoxSse();
            }

            return CalcBoundingBoxSlow();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly unsafe Box2 CalcBoundingBoxSse()
        {
            Vector128<float> boxVec;
            fixed (float* lPtr = &Box.Left)
            {
                boxVec = Sse.LoadVector128(lPtr);
            }

            var originX = Vector128.Create(Origin.X);
            var originY = Vector128.Create(Origin.Y);

            var cos = Vector128.Create((float) Math.Cos(Rotation));
            var sin = Vector128.Create((float) Math.Sin(Rotation));

            var allX = Sse.Shuffle(boxVec, boxVec, 0b10_10_00_00);
            var allY = Sse.Shuffle(boxVec, boxVec, 0b01_11_11_01);

            allX = Sse.Subtract(allX, originX);
            allY = Sse.Subtract(allY, originY);

            var modX = Sse.Subtract(Sse.Multiply(allX, cos), Sse.Multiply(allY, sin));
            var modY = Sse.Add(Sse.Multiply(allX, sin), Sse.Multiply(allY, cos));

            allX = Sse.Add(modX, originX);
            allY = Sse.Add(modY, originY);

            var l = SimdHelpers.MinHorizontalSse(allX);
            var b = SimdHelpers.MinHorizontalSse(allY);
            var r = SimdHelpers.MaxHorizontalSse(allX);
            var t = SimdHelpers.MaxHorizontalSse(allY);

            var lb = Sse.UnpackLow(l, b);
            var rt = Sse.UnpackLow(r, t);

            var lbrt = Sse.Shuffle(lb, rt, 0b11_10_01_00);

            return Unsafe.As<Vector128<float>, Box2>(ref lbrt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly unsafe Box2 CalcBoundingBoxSlow()
        {
            Span<float> allX = stackalloc float[4];
            Span<float> allY = stackalloc float[4];
            (allX[0], allY[0]) = BottomLeft;
            (allX[1], allY[1]) = TopRight;
            (allX[2], allY[2]) = TopLeft;
            (allX[3], allY[3]) = BottomRight;

            var X0 = allX[0];
            var X1 = allX[0];
            for (int i = 1; i < allX.Length; i++)
            {
                if (allX[i] > X1)
                {
                    X1 = allX[i];
                    continue;
                }

                if (allX[i] < X0)
                {
                    X0 = allX[i];
                }
            }

            var Y0 = allY[0];
            var Y1 = allY[0];
            for (int i = 1; i < allY.Length; i++)
            {
                if (allY[i] > Y1)
                {
                    Y1 = allY[i];
                    continue;
                }

                if (allY[i] < Y0)
                {
                    Y0 = allY[i];
                }
            }

            return new Box2(X0, Y0, X1, Y1);
        }

        public bool Contains(Vector2 worldPoint)
        {
            // Get the worldpoint in our frame of reference so we can do a faster AABB check.
            var localPoint = GetLocalPoint(worldPoint);
            return Box.Contains(localPoint);
        }

        /// <summary>
        /// Convert a point in world-space coordinates to our local coordinates.
        /// </summary>
        private Vector2 GetLocalPoint(Vector2 point)
        {
            // Could make this more efficient but works for now I guess...
            var boxCenter = Box.Center;

            var result = point - boxCenter;
            result = Origin + Rotation.RotateVec(result - Origin);
            return result + boxCenter;
        }

        #region Equality

        /// <inheritdoc />
        public readonly bool Equals(Box2Rotated other)
        {
            return Box.Equals(other.Box) && Rotation.Equals(other.Rotation);
        }

        /// <inheritdoc />
        public override readonly bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Box2Rotated other && Equals(other);
        }

        /// <inheritdoc />
        public override readonly int GetHashCode()
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
        public override readonly string ToString()
        {
            return $"{Box.ToString()}, {Rotation.ToString()}";
        }
    }
}
