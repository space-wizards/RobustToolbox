using System;
using System.Numerics;
using System.Reflection.Metadata;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Robust.Shared.Physics.Shapes
{
    public sealed class CircleShape : Shape
    {
        // TODO: Relative to the body's transform or...?
        /// <summary>
        /// Get or set the position of the circle
        /// </summary>
        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;
                ComputeProperties(); //TODO: Optimize here
            }
        }

        public Vector2 _position;

        /// <summary>
        /// Create a new circle with the desired radius and density.
        /// </summary>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="density">The density of the circle.</param>
        public CircleShape(float radius, float density)
            : base(density)
        {
            DebugTools.Assert(radius >= 0);
            DebugTools.Assert(density >= 0);

            ShapeType = ShapeType.Circle;
            _position = Vector2.Zero;
            Radius = radius; // The Radius property cache 2radius and calls ComputeProperties(). So no need to call ComputeProperties() here.
        }

        public CircleShape()
            : base(0)
        {
            ShapeType = ShapeType.Circle;
            _radius = 0.0f;
            _position = Vector2.Zero;
        }

        public override int ChildCount => 1;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _radius, "radius", 1.0f);
            ComputeProperties();
        }

        public override bool TestPoint(ref PhysicsTransform transform, ref Vector2 point)
        {
            var centre = transform.Position + Complex.Multiply(_position, transform.Quaternion);
            var distance = point - centre;
            return Vector2.Dot(distance, distance) <= _2radius;
        }

        public override bool RayCast(out RayCastOutput output, ref CollisionRay input, PhysicsTransform transform, int childIndex)
        {
            // Collision Detection in Interactive 3D Environments by Gino van den Bergen
            // From Section 3.1.2
            // x = s + a * r
            // norm(x) = radius

            output = new RayCastOutput();

            var centre = transform.Position + Complex.Multiply(_position, transform.Quaternion);
            Vector2 s = input.Start - centre;
            float b = Vector2.Dot(s, s) - _2radius;

            // Solve quadratic equation.
            Vector2 r = input.End - input.Start;
            float c = Vector2.Dot(s, r);
            float rr = Vector2.Dot(r, r);
            float sigma = c * c - rr * b;

            // Check for negative discriminant and short segment.
            if (sigma < 0.0f || rr < float.Epsilon)
            {
                return false;
            }

            // Find the point of intersection of the line with the circle.
            float a = -(c + (float)Math.Sqrt(sigma));

            // Is the intersection point on the segment?
            if (0.0f <= a && a <= input.MaxFraction * rr)
            {
                a /= rr;
                output.Fraction = a;

                //TODO: Check results here (not sloth's TODO)
                output.Normal = (s + r * a).Normalized;
                return true;
            }

            return false;
        }

        // TODO: Anything World is giga sketchy when we need relative transforms
        public override Box2 ComputeAABB(PhysicsTransform physicsTransform, int childIndex)
        {
            Box2 aabb = new Box2();

            // TODO: Optimise
            var pX = (_position.X * physicsTransform.Quaternion.Real - _position.Y * physicsTransform.Quaternion.Imaginary) + physicsTransform.Position.X;
            var pY = (_position.Y * physicsTransform.Quaternion.Real + _position.X * physicsTransform.Quaternion.Imaginary) + physicsTransform.Position.Y;

            aabb.Left = pX - Radius;
            aabb.Bottom = pY - Radius;
            aabb.Right = pX + Radius;
            aabb.Top = pY + Radius;
            return aabb;
        }

        protected override void ComputeProperties()
        {
            var area = (float) Math.PI * _2radius;
            MassData.Area = area;
            MassData.Mass = Density * area;
            MassData.Centroid = Position;

            // inertia about the local origin
            MassData.Inertia = MassData.Mass * (0.5f * _2radius + Vector2.Dot(Position, Position));
        }

        /// <summary>
        /// Compare the circle to another circle
        /// </summary>
        /// <param name="shape">The other circle</param>
        /// <returns>True if the two circles are the same size and have the same position</returns>
        public bool CompareTo(CircleShape shape)
        {
            return (Math.Abs(Radius - shape.Radius) < float.Epsilon && Position == shape.Position);
        }

        public override Shape Clone()
        {
            CircleShape clone = new CircleShape();
            clone.ShapeType = ShapeType;
            clone._radius = Radius;
            clone._2radius = _2radius; //FPE note: We also copy the cache
            clone._density = _density;
            clone._position = _position;
            clone.MassData = MassData;
            return clone;
        }
    }
}
