using System;

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
        public static readonly Box2Rotated UnitCentered = new Box2Rotated(Box2.UnitCentered, Angle.Zero, Vector2.Zero);

        public readonly Vector2 BottomRight => Origin + Rotation.RotateVec(Box.BottomRight - Origin);
        public readonly Vector2 TopLeft => Origin + Rotation.RotateVec(Box.TopLeft - Origin);
        public readonly Vector2 TopRight => Origin + Rotation.RotateVec(Box.TopRight - Origin);
        public readonly Vector2 BottomLeft => Origin + Rotation.RotateVec(Box.BottomLeft - Origin);

        public Box2Rotated(Vector2 bottomLeft, Vector2 topRight)
            : this(new Box2(bottomLeft, topRight)) { }

        public Box2Rotated(Box2 box)
            : this(box, 0) { }

        public Box2Rotated(Box2 box, Angle rotation)
            : this(box, rotation, Vector2.Zero) { }

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
            // https://stackoverflow.com/a/19830964

            var (X0, Y0) = Box.BottomLeft;
            var (X1, Y1) = Box.TopRight;

            var Fi = Rotation.Theta;

            var CX = (X0 + X1) / 2;  //Center point
            var CY = (Y0 + Y1) / 2;
            var WX = (X1 - X0) / 2;  //Half-width
            var WY = (Y1 - Y0) / 2;

            var SF = Math.Sin(Fi);
            var CF = Math.Cos(Fi);

            var NH = Math.Abs(WX * SF) + Math.Abs(WY * CF);  //boundrect half-height
            var NW = Math.Abs(WX * CF) + Math.Abs(WY * SF);  //boundrect half-width
            return new Box2((float) (CX - NW), (float) (CY - NH), (float) (CX + NW), (float) (CY + NH)); //draw bound rectangle
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
