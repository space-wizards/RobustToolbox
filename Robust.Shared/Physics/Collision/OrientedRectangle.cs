using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Collision
{
    /// <summary>
    /// A rectangle that can be rotated.
    /// </summary>
    [Serializable]
    internal readonly struct OrientedRectangle : IEquatable<OrientedRectangle>
    {
        /// <summary>
        /// Center point of the rectangle in world space.
        /// </summary>
        public readonly Vector2 Center;

        /// <summary>
        /// Half of the total width and height of the rectangle.
        /// </summary>
        public readonly Vector2 HalfExtents;

        /// <summary>
        /// World rotation of the rectangle in radians.
        /// </summary>
        public readonly float Rotation;

        /// <summary>
        ///     A 1x1 unit box with the origin centered and identity rotation.
        /// </summary>
        public static readonly OrientedRectangle UnitCentered = new(Vector2.Zero, Vector2.One, 0);

        public OrientedRectangle(Box2 worldBox)
        {
            Center = worldBox.Center;

            var hWidth = MathF.Abs(worldBox.Right - worldBox.Left) * 0.5f;
            var hHeight = MathF.Abs(worldBox.Bottom - worldBox.Top) * 0.5f;

            HalfExtents = new Vector2(hWidth, hHeight);
            Rotation = 0;
        }

        public OrientedRectangle(Vector2 halfExtents)
        {
            Center = default;
            HalfExtents = halfExtents;
            Rotation = default;
        }

        public OrientedRectangle(Vector2 center, Vector2 halfExtents)
        {
            Center = center;
            HalfExtents = halfExtents;
            Rotation = default;
        }

        public OrientedRectangle(Vector2 center, Vector2 halfExtents, float rotation)
        {
            Center = center;
            HalfExtents = halfExtents;
            Rotation = rotation;
        }

        public OrientedRectangle(in Vector2 center, in Box2 localBox, float rotation)
        {
            Center = center;
            HalfExtents = new Vector2(localBox.Width / 2, localBox.Height / 2);
            Rotation = rotation;
        }

        /// <summary>
        /// calculates the smallest AABB that will encompass this rectangle. The AABB is in local space.
        /// </summary>
        public Box2 CalcBoundingBox()
        {
            var Fi = Rotation;

            var CX = Center.X;
            var CY = Center.Y;

            var WX = HalfExtents.X;
            var WY = HalfExtents.Y;

            var SF = MathF.Sin(Fi);
            var CF = MathF.Cos(Fi);

            var NH = MathF.Abs(WX * SF) + MathF.Abs(WY * CF);  //boundrect half-height
            var NW = MathF.Abs(WX * CF) + MathF.Abs(WY * SF);  //boundrect half-width

            return new Box2(CX - NW, CY - NH, CX + NW, CY + NH); //draw bound rectangle
        }

        /// <summary>
        /// Tests if a point is contained inside this rectangle.
        /// </summary>
        /// <param name="point">Point to test.</param>
        /// <returns>True if the point is contained inside this rectangle.</returns>
        public bool Contains(Vector2 point)
        {
            // rotate around rectangle center by -rectAngle
            var s = MathF.Sin(-Rotation);
            var c = MathF.Cos(-Rotation);

            // set origin to rect center
            var newPoint = point - Center;

            // rotate
            newPoint = new Vector2(newPoint.X * c - newPoint.Y * s, newPoint.X * s + newPoint.Y * c);

            // put origin back
            newPoint += Center;

            // check if our transformed point is in the rectangle, which is no longer
            // rotated relative to the point

            var xMin = -HalfExtents.X;
            var xMax =  HalfExtents.X;
            var yMin = -HalfExtents.Y;
            var yMax =  HalfExtents.Y;

            return newPoint.X >= xMin && newPoint.X <= xMax && newPoint.Y >= yMin && newPoint.Y <= yMax;
        }

        /// <summary>
        /// Returns the closest point inside the rectangle to the given point in world space.
        /// </summary>
        public Vector2 ClosestPointWorld(Vector2 worldPoint)
        {
            // inverse-transform the sphere's center into the box's local space.
            var localPoint = InverseTransformPoint(worldPoint);

            var xMin = -HalfExtents.X;
            var xMax = HalfExtents.X;
            var yMin = -HalfExtents.Y;
            var yMax = HalfExtents.Y;

            // clamp the point to the border of the box
            var cx = MathHelper.Clamp(localPoint.X, xMin, xMax);
            var cy = MathHelper.Clamp(localPoint.Y, yMin, yMax);

            return TransformPoint(new Vector2(cx, cy));
        }

        /// <summary>
        /// Transforms a point from the rectangle's local space to world space.
        /// </summary>
        public Vector2 TransformPoint(Vector2 localPoint)
        {
            var theta = Rotation;
            var (x, y) = localPoint;
            var dx = MathF.Cos(theta) * x - MathF.Sin(theta) * y;
            var dy = MathF.Sin(theta) * x + MathF.Cos(theta) * y;
            return new Vector2(dx, dy) + Center;
        }

        /// <summary>
        /// Transforms a point from world space to the rectangle's local space.
        /// </summary>
        public Vector2 InverseTransformPoint(Vector2 worldPoint)
        {
            var theta = -Rotation;
            var (x, y) = worldPoint + -Center;
            var dx = MathF.Cos(theta) * x - MathF.Sin(theta) * y;
            var dy = MathF.Sin(theta) * x + MathF.Cos(theta) * y;
            return new Vector2(dx, dy);
        }

        #region Equality Members

        /// <inheritdoc />
        public bool Equals(OrientedRectangle other)
        {
            return Center.Equals(other.Center) && HalfExtents.Equals(other.HalfExtents) && Rotation.Equals(other.Rotation);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is OrientedRectangle other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Center, HalfExtents, Rotation);
        }

        /// <summary>
        ///     Check for equality by value between two <see cref="OrientedRectangle"/>.
        /// </summary>
        public static bool operator ==(OrientedRectangle left, OrientedRectangle right) {
            return left.Equals(right);
        }

        /// <summary>
        ///     Check for inequality by value between two <see cref="OrientedRectangle"/>.
        /// </summary>
        public static bool operator !=(OrientedRectangle left, OrientedRectangle right) {
            return !left.Equals(right);
        }

        #endregion

        /// <summary>
        /// Returns the string representation of this object.
        /// </summary>
        public override string ToString()
        {
            var box = new Box2(-HalfExtents.X, -HalfExtents.Y, HalfExtents.X, HalfExtents.Y).Translated(Center);
            return $"{box}, {Rotation}";
        }
    }
}
