using System;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Shapes;

namespace Robust.Shared.Physics
{
    internal static class CollisionSolver
    {
        public static void CalculateFeatures(Manifold manifold, IPhysShape a, IPhysShape b, out CollisionFeatures features)
        {
            // 2D table of all possible PhysShape combinations
            switch (a)
            {
                case PhysShapeCircle aCircle:
                    switch (b)
                    {
                        case PhysShapeCircle bCircle:
                            CircleCircle(manifold, aCircle, bCircle, 1, out features);
                            return;
                        case PhysShapeAabb bAabb:
                            CircleBox(manifold, aCircle, bAabb, 1, out features);
                            return;
                        case PhysShapeRect bRect:
                            RectCircle(manifold, bRect, aCircle, -1, out features);
                            return;
                        case PhysShapeGrid bGrid:
                            DummyBoundsFeatures(manifold, out features);
                            return;
                    }
                    break;
                case PhysShapeAabb aAabb:
                    switch (b)
                    {
                        case PhysShapeCircle bCircle:
                            CircleBox(manifold, bCircle, aAabb, -1, out features);
                            return;
                        case PhysShapeAabb bAabb:
                            BoxBox(manifold, aAabb, bAabb, 1, out features);
                            return;
                        case PhysShapeRect bRect:
                            DummyBoundsFeatures(manifold, out features);
                            return;
                        case PhysShapeGrid bGrid:
                            DummyBoundsFeatures(manifold, out features);
                            return;
                    }
                    break;
                case PhysShapeRect aRect:
                    switch (b)
                    {
                        case PhysShapeCircle bCircle:
                            RectCircle(manifold, aRect, bCircle, 1, out features);
                            return;
                        case PhysShapeAabb bAabb:
                            DummyBoundsFeatures(manifold, out features);
                            return;
                        case PhysShapeRect bRect:
                            DummyBoundsFeatures(manifold, out features);
                            return;
                        case PhysShapeGrid bGrid:
                            DummyBoundsFeatures(manifold, out features);
                            return;
                    }
                    break;
                case PhysShapeGrid aGrid:
                    switch (b)
                    {
                        case PhysShapeCircle bCircle:
                            DummyBoundsFeatures(manifold, out features);
                            return;
                        case PhysShapeAabb bAabb:
                            DummyBoundsFeatures(manifold, out features);
                            return;
                        case PhysShapeRect bRect:
                            DummyBoundsFeatures(manifold, out features);
                            return;
                        case PhysShapeGrid bGrid:
                            DummyBoundsFeatures(manifold, out features);
                            return;
                    }
                    break;
            }
            features = default;
        }

        private static void DummyBoundsFeatures(Manifold manifold, out CollisionFeatures features)
        {
            var aRect = new AlignedRectangle(manifold.A.Entity.Transform.WorldPosition, manifold.A.AABB.Size / 2);
            var bRect = new AlignedRectangle(manifold.B.Entity.Transform.WorldPosition, manifold.B.AABB.Size / 2);
            CalculateCollisionFeatures(aRect, bRect, 1, out features);
        }

        private static void CircleCircle(Manifold manifold, PhysShapeCircle a, PhysShapeCircle b, float flip,
            out CollisionFeatures features)
        {
            var aRad = a.Radius;
            var bRad = b.Radius;

            var aPos = manifold.A.Entity.Transform.WorldPosition;
            var bPos = manifold.B.Entity.Transform.WorldPosition;

            CalculateCollisionFeatures(new Circle(aPos, aRad), new Circle(bPos, bRad), (float) flip, out features);
        }

        private static void CircleBox(Manifold manifold, PhysShapeCircle a, PhysShapeAabb b, float flip,
            out CollisionFeatures features)
        {
            var aRad = a.Radius;
            var aPos = manifold.A.Entity.Transform.WorldPosition;

            var bRect = new AlignedRectangle(manifold.B.Entity.Transform.WorldPosition, b.LocalBounds.Size / 2);

            CalculateCollisionFeatures(bRect, new Circle(aPos, aRad), (float) flip * -1, out features);
        }

        private static void RectCircle(Manifold manifold, PhysShapeRect a, PhysShapeCircle b, float flip,
            out CollisionFeatures features)
        {
            var aPos = manifold.A.Entity.Transform.WorldPosition;
            var bPos = manifold.B.Entity.Transform.WorldPosition;

            var aRot = (float)manifold.A.Entity.Transform.WorldRotation.Theta;

            CalculateCollisionFeatures(new OrientedRectangle(aPos, a.Rectangle, aRot), new Circle(bPos, b.Radius), (float) flip, out features);
        }

        private static void BoxBox(Manifold manifold, PhysShapeAabb a, PhysShapeAabb b, float flip,
            out CollisionFeatures features)
        {
            var aRect = new AlignedRectangle(manifold.A.Entity.Transform.WorldPosition, a.LocalBounds.Size / 2);
            var bRect = new AlignedRectangle(manifold.B.Entity.Transform.WorldPosition, b.LocalBounds.Size / 2);

            CalculateCollisionFeatures(in aRect, in bRect, flip, out features);
        }

        public static void CalculateCollisionFeatures(in Circle A, in Circle B, float flip, out CollisionFeatures features)
        {
            var aRad = A.Radius;
            var bRad = B.Radius;

            var aPos = A.Position;
            var bPos = B.Position;

            // combined radius
            var radiiSum = aRad + bRad;

            // distance between circles
            var dist = bPos - aPos;

            // if the distance between two circles is larger than their combined radii,
            // they are not colliding, otherwise they are
            if (dist.LengthSquared > radiiSum * radiiSum)
            {
                features = default;
                return;
            }

            // if dist between circles is zero, the circles are concentric, this collision cannot be resolved
            if (dist.LengthSquared.Equals(0f))
            {
                features = default;
                return;
            }

            // generate collision normal
            var normal = dist.Normalized;

            // half of the total
            var penetraction = (radiiSum - dist.Length) * 0.5f;

            var contacts = new Vector2[1];

            // dtp - Distance to intersection point
            var dtp = aRad - penetraction;
            var contact = aPos + normal * dtp;
            contacts[0] = contact;

            features = new CollisionFeatures(true, normal, penetraction, contacts);
        }

        public static void CalculateCollisionFeatures(in AlignedRectangle A, in Circle B, float flip, out CollisionFeatures features)
        {
            // closest point inside the rectangle to the center of the sphere.
            var closestPoint = A.ClosestPoint(in B.Position);

            // If the point is outside the sphere, the sphere and OBB do not intersect.
            var distanceSq = (closestPoint - B.Position).LengthSquared;
            if (distanceSq > B.Radius * B.Radius)
            {
                features = default;
                return;
            }

            Vector2 normal;
            if (distanceSq.Equals(0.0f))
            {
                var mSq = (closestPoint - A.Center).LengthSquared;
                if (mSq.Equals(0.0f))
                {
                    features = default;
                    return;
                }

                // Closest point is at the center of the sphere
                normal = (closestPoint - A.Center).Normalized;
            }
            else
                normal = (B.Position - closestPoint).Normalized;

            var outsidePoint = B.Position - normal * B.Radius;
            var distance = (closestPoint - outsidePoint).Length;
            var contacts = new Vector2[1];
            contacts[0] = closestPoint + (outsidePoint - closestPoint) * 0.5f;
            var depth = distance * 0.5f;

            features = new CollisionFeatures(true, normal, depth, contacts);
        }

        public static void CalculateCollisionFeatures(in OrientedRectangle A, in Circle B, float flip, out CollisionFeatures features)
        {
            // closest point inside the rectangle to the center of the sphere.
            var closestPoint = A.ClosestPointWorld(B.Position);

            // If the point is outside the sphere, the sphere and OBB do not intersect.
            var distanceSq = (closestPoint - B.Position).LengthSquared;
            if (distanceSq > B.Radius * B.Radius)
            {
                features = default;
                return;
            }

            Vector2 normal;
            if (distanceSq.Equals(0.0f))
            {
                var mSq = (closestPoint - A.Center).LengthSquared;
                if (mSq.Equals(0.0f))
                {
                    features = default;
                    return;
                }

                // Closest point is at the center of the sphere
                normal = (closestPoint - A.Center).Normalized;
            }
            else
                normal = (B.Position - closestPoint).Normalized;

            var outsidePoint = B.Position - normal * B.Radius;
            var distance = (closestPoint - outsidePoint).Length;
            var contacts = new Vector2[1];
            contacts[0] = closestPoint + (outsidePoint - closestPoint) * 0.5f;
            var depth = distance * 0.5f;

            features = new CollisionFeatures(true, normal, depth, contacts);
        }

        public static void CalculateCollisionFeatures(in AlignedRectangle A, in AlignedRectangle B, float flip, out CollisionFeatures features)
        {
            // Vector from A to B
            Vector2 n = B.Center - A.Center;

            // Calculate half extents along x axis for each object
            float a_extent_x = A.HalfExtents.X;
            float b_extent_x = B.HalfExtents.X;

            // Calculate overlap on x axis
            float x_overlap = a_extent_x + b_extent_x - MathF.Abs(n.X);

            // SAT test on x axis
            if (!(x_overlap > 0))
            {
                features = default;
                return;
            }

            // Calculate half extents along y axis for each object
            float a_extent_y = A.HalfExtents.Y;
            float b_extent_y = B.HalfExtents.Y;

            // Calculate overlap on y axis
            float y_overlap = a_extent_y + b_extent_y - MathF.Abs(n.Y);

            // SAT test on y axis
            if (!(y_overlap > 0))
            {
                features = default;
                return;
            }

            Vector2 normal;
            float penetration;
            Vector2 contact;

            // Find out which axis is axis of least penetration
            if (x_overlap < y_overlap)
            {
                // Point towards B knowing that n points from A to B
                if (n.X < 0)
                    normal = new Vector2(-1, 0);
                else
                    normal = new Vector2(1, 0);
                penetration = x_overlap / 2;
                var hitx = A.Center.X + (a_extent_x * normal.X);
                var hity = B.Center.Y;
                contact = new Vector2(hitx, hity);
            }
            else
            {
                // Point toward B knowing that n points from A to B
                if (n.Y < 0)
                    normal = new Vector2(0, -1);
                else
                    normal = new Vector2(0, 1);
                penetration = y_overlap / 2;
                var hitx = B.Center.X;
                var hity = A.Center.Y + (a_extent_y * normal.Y);
                contact = new Vector2(hitx, hity);
            }

            features = new CollisionFeatures(true, normal, penetration, new[] {contact});
        }
    }

    /// <summary>
    /// Features of the collision.
    /// </summary>
    internal readonly struct CollisionFeatures
    {
        /// <summary>
        /// Are the two shapes *actually* colliding? If this is false, the rest of the
        /// values in this struct are default.
        /// </summary>
        public readonly bool Collided;

        /// <summary>
        /// Collision normal. If A moves in the negative direction of the normal and
        /// B moves in the positive direction, the objects will no longer intersect.
        /// </summary>
        public readonly Vector2 Normal;

        /// <summary>
        /// Half of the total depth of penetration. Each object needs to move
        /// by the penetration distance along the normal to resolve the collision.
        /// </summary>
        /// <remarks>
        /// The penetration depth is the length of the minimum translation vector (MTV), which
        /// is the smallest vector along which we can translate an intersecting shape to
        /// separate it from the other shape.
        /// </remarks>
        public readonly float Penetration;

        /// <summary>
        /// all the points at which the two objects collide, projected onto a plane.The plane
        /// these points are projected onto has the normal of the collision normal and is
        /// located halfway between the colliding objects.
        /// </summary>
        public readonly Vector2[] Contacts;

        /// <summary>
        /// Constructs a new instance of <see cref="CollisionFeatures"/>.
        /// </summary>
        public CollisionFeatures(bool collided, Vector2 normal, float penetration, Vector2[] contacts)
        {
            Collided = collided;
            Normal = normal;
            Penetration = penetration;
            Contacts = contacts;
        }
    }

    /// <summary>
    /// A rectangle that is always axis-aligned.
    /// </summary>
    [Serializable]
    internal readonly struct AlignedRectangle : IEquatable<AlignedRectangle>
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
        ///     A 1x1 unit rectangle with the origin centered on the world origin.
        /// </summary>
        public static readonly AlignedRectangle UnitCentered = new(new Vector2(0.5f, 0.5f));

        /// <summary>
        ///     The lower X coordinate of the left edge of the box.
        /// </summary>
        public float Left => Center.X - HalfExtents.X;

        /// <summary>
        ///     The higher X coordinate of the right edge of the box.
        /// </summary>
        public float Right => Center.X + HalfExtents.X;

        /// <summary>
        ///     The lower Y coordinate of the top edge of the box.
        /// </summary>
        public float Bottom => Center.Y + HalfExtents.Y;

        /// <summary>
        ///     The higher Y coordinate of the bottom of the box.
        /// </summary>
        public float Top => Center.Y + HalfExtents.Y;

        public AlignedRectangle(Box2 box)
        {
            var halfWidth = box.Width / 2;
            var halfHeight = box.Height / 2;

            HalfExtents = new Vector2(halfWidth, halfHeight);
            Center = new Vector2(box.Left + halfWidth, box.Height + halfHeight);
        }

        public AlignedRectangle(Vector2 halfExtents)
        {
            Center = default;
            HalfExtents = halfExtents;
        }

        public AlignedRectangle(Vector2 center, Vector2 halfExtents)
        {
            Center = center;
            HalfExtents = halfExtents;
        }

        /// <summary>
        /// Given a point, returns the closest point to it inside the box.
        /// </summary>
        public Vector2 ClosestPoint(in Vector2 position)
        {
            // clamp the point to the border of the box
            var cx = MathHelper.Clamp(position.X, Left, Right);
            var cy = MathHelper.Clamp(position.Y, Bottom, Top);

            return new Vector2(cx, cy);
        }

        #region Equality members

        public bool Equals(AlignedRectangle other)
        {
            return Center.Equals(other.Center) && HalfExtents.Equals(other.HalfExtents);
        }

        public override bool Equals(object? obj)
        {
            return obj is AlignedRectangle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Center, HalfExtents);
        }

        public static bool operator ==(AlignedRectangle left, AlignedRectangle right) {
            return left.Equals(right);
        }

        public static bool operator !=(AlignedRectangle left, AlignedRectangle right) {
            return !left.Equals(right);
        }

        #endregion

        /// <summary>
        /// Returns the string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"({Left}, {Bottom}, {Right}, {Top})";
        }
    }

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

            return new Box2((float)(CX - NW), (float)(CY - NH), (float)(CX + NW), (float)(CY + NH)); //draw bound rectangle
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
