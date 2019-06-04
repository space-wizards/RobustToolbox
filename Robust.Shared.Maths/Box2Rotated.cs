using System;

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     This type contains a <see cref="Box2"/> and a rotation <see cref="Angle"/> in world space.
    /// </summary>
    [Serializable]
    public readonly struct Box2Rotated : IEquatable<Box2Rotated>
    {
        public readonly Box2 Box;
        public readonly Angle Rotation;

        /// <summary>
        ///     A 1x1 unit box with the origin centered and identity rotation.
        /// </summary>
        public static readonly Box2Rotated UnitCentered = new Box2Rotated(Box2.UnitCentered, Angle.Zero);

        public Vector2 BottomRight => Rotation.RotateVec(new Vector2(Box.Right, Box.Bottom));
        public Vector2 TopLeft => Rotation.RotateVec(new Vector2(Box.Left, Box.Top));
        public Vector2 TopRight => Rotation.RotateVec(new Vector2(Box.Right, Box.Top));
        public Vector2 BottomLeft => Rotation.RotateVec(new Vector2(Box.Left, Box.Bottom));

        public Box2Rotated(Box2 box, Angle rotation)
        {
            Box = box;
            Rotation = rotation;
        }

        #region Equality

        /// <inheritdoc />
        public bool Equals(Box2Rotated other)
        {
            return Box.Equals(other.Box) && Rotation.Equals(other.Rotation);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Box2Rotated other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
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
        public override string ToString()
        {
            return $"{Box.ToString()}, {Rotation.ToString()}";
        }
    }
}
