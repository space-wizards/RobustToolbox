using System;
using System.Numerics;
using System.Reflection.Metadata;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Utility;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Robust.Shared.Physics.Shapes
{
    internal sealed class CircleShape : Shape
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

        internal Vector2 _position;

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

        internal CircleShape()
            : base(0)
        {
            ShapeType = ShapeType.Circle;
            _radius = 0.0f;
            _position = Vector2.Zero;
        }

        internal override int ChildCount => 1;

        internal override bool TestPoint(ITransformComponent transform, ref Vector2 point)
        {
            var centre = transform.WorldPosition + Complex.Multiply(_position, transform.Q);
            var distance = point - centre;
            return Vector2.Dot(distance, distance) <= _2radius;
        }

        internal override bool RayCast(out RayCastOutput output, ref RayCastInput input, ITransformComponent transform, int childIndex)
        {
            // Collision Detection in Interactive 3D Environments by Gino van den Bergen
            // From Section 3.1.2
            // x = s + a * r
            // norm(x) = radius

            output = new RayCastOutput();

            var centre = transform.WorldPosition + Complex.Multiply(_position, transform.Q);
            Vector2 s = input.Point1 - centre;
            float b = Vector2.Dot(s, s) - _2radius;

            // Solve quadratic equation.
            Vector2 r = input.Point2 - input.Point1;
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
        internal override void ComputeAABB(out AABB aabb, ITransformComponent transform, int childIndex)
        {
            // TODO: Optimise
            var pX = (_position.X * transform.Q.Real - _position.Y * transform.Q.Imaginary) + transform.WorldPosition.X;
            var pY = (_position.Y * transform.Q.Real + _position.X * transform.Q.Imaginary) + transform.WorldPosition.Y;

            aabb.LowerBound.X = pX - Radius;
            aabb.LowerBound.Y = pY - Radius;
            aabb.UpperBound.X = pX + Radius;
            aabb.UpperBound.Y = pY + Radius;
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

        internal override Shape Clone()
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
