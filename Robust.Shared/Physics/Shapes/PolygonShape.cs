using System;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Shapes
{
    /// <summary>
    ///     Represents a simple non-selfintersecting convex polygon.
    ///     Create a convex hull from the given array of points.
    /// </summary>
    internal sealed class PolygonShape : Shape
    {
        // TODO: These are initialized in the constructor so need to mark it somehow
        private Vertices _vertices = null!;
        private Vertices _normals = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonShape"/> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="density">The density.</param>
        internal PolygonShape(Vertices vertices, float density)
            : base(density)
        {
            ShapeType = ShapeType.Polygon;
            _radius = PhysicsSettings.PolygonRadius;

            Vertices = vertices;
        }

        /// <summary>
        /// Create a new PolygonShape with the specified density.
        /// </summary>
        /// <param name="density">The density.</param>
        public PolygonShape(float density)
            : base(density)
        {
            DebugTools.Assert(density >= 0f);

            ShapeType = ShapeType.Polygon;
            _radius = PhysicsSettings.PolygonRadius;
            _vertices = new Vertices(PhysicsSettings.MaxPolygonVertices);
            _normals = new Vertices(PhysicsSettings.MaxPolygonVertices);
        }

        internal PolygonShape()
            : base(0)
        {
            ShapeType = ShapeType.Polygon;
            _radius = PhysicsSettings.PolygonRadius;
            _vertices = new Vertices(PhysicsSettings.MaxPolygonVertices);
            _normals = new Vertices(PhysicsSettings.MaxPolygonVertices);
        }

        /// <summary>
        /// Create a convex hull from the given array of local points.
        /// The number of vertices must be in the range [3, Settings.MaxPolygonVertices].
        /// Warning: the points may be re-ordered, even if they form a convex polygon
        /// Warning: collinear points are handled but not removed. Collinear points may lead to poor stacking behavior.
        /// </summary>
        public Vertices Vertices
        {
            get => _vertices;
            set
            {
                _vertices = new Vertices(value);

                DebugTools.Assert(_vertices.Count >= 3 && _vertices.Count <= PhysicsSettings.MaxPolygonVertices);

                if (PhysicsSettings.UseConvexHullPolygons)
                {
                    //FPE note: This check is required as the GiftWrap algorithm early exits on triangles
                    //So instead of giftwrapping a triangle, we just force it to be clock wise.
                    if (_vertices.Count <= 3)
                        _vertices.ForceCounterClockWise();
                    else
                        _vertices = GiftWrap.GetConvexHull(_vertices);
                }

                _normals = new Vertices(_vertices.Count);

                // Compute normals. Ensure the edges have non-zero length.
                for (int i = 0; i < _vertices.Count; ++i)
                {
                    int next = i + 1 < _vertices.Count ? i + 1 : 0;
                    Vector2 edge = _vertices[next] - _vertices[i];
                    DebugTools.Assert(edge.LengthSquared > float.Epsilon * float.Epsilon);

                    //FPE optimization: Normals.Add(MathUtils.Cross(edge, 1.0f));
                    Vector2 temp = new Vector2(edge.Y, -edge.X).Normalized;
                    _normals.Add(temp);
                }

                // Compute the polygon mass data
                ComputeProperties();
            }
        }

        public Vertices Normals => _normals;

        internal override int ChildCount => 1;

        protected override void ComputeProperties()
        {
            // Polygon mass, centroid, and inertia.
            // Let rho be the polygon density in mass per unit area.
            // Then:
            // mass = rho * int(dA)
            // centroid.X = (1/mass) * rho * int(x * dA)
            // centroid.Y = (1/mass) * rho * int(y * dA)
            // I = rho * int((x*x + y*y) * dA)
            //
            // We can compute these integrals by summing all the integrals
            // for each triangle of the polygon. To evaluate the integral
            // for a single triangle, we make a change of variables to
            // the (u,v) coordinates of the triangle:
            // x = x0 + e1x * u + e2x * v
            // y = y0 + e1y * u + e2y * v
            // where 0 <= u && 0 <= v && u + v <= 1.
            //
            // We integrate u from [0,1-v] and then v from [0,1].
            // We also need to use the Jacobian of the transformation:
            // D = cross(e1, e2)
            //
            // Simplification: triangle centroid = (1/3) * (p1 + p2 + p3)
            //
            // The rest of the derivation is handled by computer algebra.

            DebugTools.Assert(Vertices.Count >= 3);

            //FPE optimization: Early exit as polygons with 0 density does not have any properties.
            if (_density <= 0)
                return;

            //FPE optimization: Consolidated the calculate centroid and mass code to a single method.
            Vector2 center = Vector2.Zero;
            float area = 0.0f;
            float I = 0.0f;

            // pRef is the reference point for forming triangles.
            // It's location doesn't change the result (except for rounding error).
            Vector2 s = Vector2.Zero;

            // This code would put the reference point inside the polygon.
            for (int i = 0; i < Vertices.Count; ++i)
            {
                s += Vertices[i];
            }
            s *= 1.0f / Vertices.Count;

            const float k_inv3 = 1.0f / 3.0f;

            for (int i = 0; i < Vertices.Count; ++i)
            {
                // Triangle vertices.
                Vector2 e1 = Vertices[i] - s;
                Vector2 e2 = i + 1 < Vertices.Count ? Vertices[i + 1] - s : Vertices[0] - s;

                float D = Vector2.Cross(e1, e2);

                float triangleArea = 0.5f * D;
                area += triangleArea;

                // Area weighted centroid
                center += (e1 + e2) * triangleArea * k_inv3;

                float ex1 = e1.X, ey1 = e1.Y;
                float ex2 = e2.X, ey2 = e2.Y;

                float intx2 = ex1 * ex1 + ex2 * ex1 + ex2 * ex2;
                float inty2 = ey1 * ey1 + ey2 * ey1 + ey2 * ey2;

                I += (0.25f * k_inv3 * D) * (intx2 + inty2);
            }

            //The area is too small for the engine to handle.
            DebugTools.Assert(area > float.Epsilon);

            // We save the area
            MassData.Area = area;

            // Total mass
            MassData.Mass = _density * area;

            // Center of mass
            center *= 1.0f / area;
            MassData.Centroid = center + s;

            // Inertia tensor relative to the local origin (point s).
            MassData.Inertia = _density * I;

            // Shift to center of mass then to original body origin.
            MassData.Inertia += MassData.Mass * (Vector2.Dot(MassData.Centroid, MassData.Centroid) - Vector2.Dot(center, center));
        }

        internal override bool TestPoint(ITransformComponent transform, ref Vector2 point)
        {
            Vector2 pLocal = Complex.Divide(point - transform.WorldPosition, transform.Q);

            for (int i = 0; i < Vertices.Count; ++i)
            {
                float dot = Vector2.Dot(Normals[i], pLocal - Vertices[i]);
                if (dot > 0.0f)
                {
                    return false;
                }
            }

            return true;
        }

        internal override bool RayCast(out RayCastOutput output, ref RayCastInput input, ITransformComponent transform, int childIndex)
        {
            output = new RayCastOutput();

            // Put the ray into the polygon's frame of reference.
            Vector2 p1 = Complex.Divide(input.Point1 - transform.WorldPosition, transform.Q);
            Vector2 p2 = Complex.Divide(input.Point2 - transform.WorldPosition, transform.Q);
            Vector2 d = p2 - p1;

            float lower = 0.0f, upper = input.MaxFraction;

            int index = -1;

            for (int i = 0; i < Vertices.Count; ++i)
            {
                // p = p1 + a * d
                // dot(normal, p - v) = 0
                // dot(normal, p1 - v) + a * dot(normal, d) = 0
                float numerator = Vector2.Dot(Normals[i], Vertices[i] - p1);
                float denominator = Vector2.Dot(Normals[i], d);

                if (denominator == 0.0f)
                {
                    if (numerator < 0.0f)
                    {
                        return false;
                    }
                }
                else
                {
                    // Note: we want this predicate without division:
                    // lower < numerator / denominator, where denominator < 0
                    // Since denominator < 0, we have to flip the inequality:
                    // lower < numerator / denominator <==> denominator * lower > numerator.
                    if (denominator < 0.0f && numerator < lower * denominator)
                    {
                        // Increase lower.
                        // The segment enters this half-space.
                        lower = numerator / denominator;
                        index = i;
                    }
                    else if (denominator > 0.0f && numerator < upper * denominator)
                    {
                        // Decrease upper.
                        // The segment exits this half-space.
                        upper = numerator / denominator;
                    }
                }

                // The use of epsilon here causes the assert on lower to trip
                // in some cases. Apparently the use of epsilon was to make edge
                // shapes work, but now those are handled separately.
                //if (upper < lower - b2_epsilon)
                if (upper < lower)
                {
                    return false;
                }
            }

            DebugTools.Assert(0.0f <= lower && lower <= input.MaxFraction);

            if (index >= 0)
            {
                output.Fraction = lower;
                output.Normal = Complex.Multiply(Normals[index], transform.Q);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Given a transform, compute the associated axis aligned bounding box for a child shape.
        /// </summary>
        /// <param name="aabb">The aabb results.</param>
        /// <param name="transform">The world transform of the shape.</param>
        /// <param name="childIndex">The child shape index.</param>
        internal override void ComputeAABB(out AABB aabb, ITransformComponent transform, int childIndex)
        {
            // OPT: aabb.LowerBound = Transform.Multiply(Vertices[0], ref transform);
            var vert = Vertices[0];
            aabb.LowerBound.X = (vert.X * transform.Q.Real - vert.Y * transform.Q.Imaginary) + transform.WorldPosition.X;
            aabb.LowerBound.Y = (vert.Y * transform.Q.Real + vert.X * transform.Q.Imaginary) + transform.WorldPosition.Y;
            aabb.UpperBound = aabb.LowerBound;

            for (int i = 1; i < Vertices.Count; ++i)
            {
                // OPT: Vector2 v = Transform.Multiply(Vertices[i], ref transform);
                vert = Vertices[i];
                float vX = (vert.X * transform.Q.Real - vert.Y * transform.Q.Imaginary) + transform.WorldPosition.X;
                float vY = (vert.Y * transform.Q.Real + vert.X * transform.Q.Imaginary) + transform.WorldPosition.Y;

                // OPT: Vector2.Min(ref aabb.LowerBound, ref v, out aabb.LowerBound);
                // OPT: Vector2.Max(ref aabb.UpperBound, ref v, out aabb.UpperBound);
                DebugTools.Assert(aabb.LowerBound.X <= aabb.UpperBound.X);
                if (vX < aabb.LowerBound.X) aabb.LowerBound.X = vX;
                else if (vX > aabb.UpperBound.X) aabb.UpperBound.X = vX;
                DebugTools.Assert(aabb.LowerBound.Y <= aabb.UpperBound.Y);
                if (vY < aabb.LowerBound.Y) aabb.LowerBound.Y = vY;
                else if (vY > aabb.UpperBound.Y) aabb.UpperBound.Y = vY;
            }

            // OPT: Vector2 r = new Vector2(Radius, Radius);
            // OPT: aabb.LowerBound = aabb.LowerBound - r;
            // OPT: aabb.UpperBound = aabb.UpperBound + r;
            aabb.LowerBound.X -= Radius;
            aabb.LowerBound.Y -= Radius;
            aabb.UpperBound.X += Radius;
            aabb.UpperBound.Y += Radius;
        }

        public bool CompareTo(PolygonShape shape)
        {
            if (Vertices.Count != shape.Vertices.Count)
                return false;

            for (int i = 0; i < Vertices.Count; i++)
            {
                if (Vertices[i] != shape.Vertices[i])
                    return false;
            }

            return (Math.Abs(Radius - shape.Radius) < float.Epsilon && MassData == shape.MassData);
        }

        internal override Shape Clone()
        {
            PolygonShape clone = new PolygonShape
            {
                ShapeType = ShapeType,
                _radius = _radius,
                _density = _density,
                _vertices = new Vertices(_vertices),
                _normals = new Vertices(_normals),
                MassData = MassData
            };
            return clone;
        }
    }
}
