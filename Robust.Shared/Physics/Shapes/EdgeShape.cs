using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// A line segment (edge) shape. These can be connected in chains or loops
    /// to other edge shapes.
    /// The connectivity information is used to ensure correct contact normals.
    /// </summary>
    public class EdgeShape : Shape
    {
        /// <summary>
        /// Edge start vertex
        /// </summary>
        internal Vector2 _vertex1;

        /// <summary>
        /// Edge end vertex
        /// </summary>
        internal Vector2 _vertex2;

        internal EdgeShape()
            : base(0)
        {
            ShapeType = ShapeType.Edge;
            _radius = PhysicsSettings.PolygonRadius;
        }

        /// <summary>
        /// Create a new EdgeShape with the specified start and end.
        /// </summary>
        /// <param name="start">The start of the edge.</param>
        /// <param name="end">The end of the edge.</param>
        public EdgeShape(Vector2 start, Vector2 end)
            : base(0)
        {
            ShapeType = ShapeType.Edge;
            _radius = PhysicsSettings.PolygonRadius;
            Set(start, end);
        }

        public override int ChildCount
        {
            get { return 1; }
        }

        /// <summary>
        /// Is true if the edge is connected to an adjacent vertex before vertex 1.
        /// </summary>
        public bool HasVertex0 { get; set; }

        /// <summary>
        /// Is true if the edge is connected to an adjacent vertex after vertex2.
        /// </summary>
        public bool HasVertex3 { get; set; }

        /// <summary>
        /// Optional adjacent vertices. These are used for smooth collision.
        /// </summary>
        public Vector2 Vertex0 { get; set; }

        /// <summary>
        /// Optional adjacent vertices. These are used for smooth collision.
        /// </summary>
        public Vector2 Vertex3 { get; set; }

        /// <summary>
        /// These are the edge vertices
        /// </summary>
        public Vector2 Vertex1
        {
            get { return _vertex1; }
            set
            {
                _vertex1 = value;
                ComputeProperties();
            }
        }

        /// <summary>
        /// These are the edge vertices
        /// </summary>
        public Vector2 Vertex2
        {
            get { return _vertex2; }
            set
            {
                _vertex2 = value;
                ComputeProperties();
            }
        }

        /// <summary>
        /// Set this as an isolated edge.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        public void Set(Vector2 start, Vector2 end)
        {
            _vertex1 = start;
            _vertex2 = end;
            HasVertex0 = false;
            HasVertex3 = false;

            ComputeProperties();
        }

        public override bool TestPoint(ref PhysicsTransform physicsTransform, ref Vector2 point)
        {
            return false;
        }

        public override bool RayCast(out RayCastOutput output, ref RayCastInput input, PhysicsTransform physicsTransform, int childIndex)
        {
            // p = p1 + t * d
            // v = v1 + s * e
            // p1 + t * d = v1 + s * e
            // s * e - t * d = p1 - v1

            output = new RayCastOutput();

            // Put the ray into the edge's frame of reference.
            Vector2 p1 = Complex.Divide(input.Point1 - physicsTransform.Position, ref physicsTransform.Quaternion);
            Vector2 p2 = Complex.Divide(input.Point2 - physicsTransform.Position, ref physicsTransform.Quaternion);
            Vector2 d = p2 - p1;

            Vector2 v1 = _vertex1;
            Vector2 v2 = _vertex2;
            Vector2 e = v2 - v1;
            Vector2 normal = new Vector2(e.Y, -e.X); //TODO: Could possibly cache the normal.
            normal = normal.Normalized;

            // q = p1 + t * d
            // dot(normal, q - v1) = 0
            // dot(normal, p1 - v1) + t * dot(normal, d) = 0
            float numerator = Vector2.Dot(normal, v1 - p1);
            float denominator = Vector2.Dot(normal, d);

            if (denominator == 0.0f)
            {
                return false;
            }

            float t = numerator / denominator;
            if (t < 0.0f || input.MaxFraction < t)
            {
                return false;
            }

            Vector2 q = p1 + d * t;

            // q = v1 + s * r
            // s = dot(q - v1, r) / dot(r, r)
            Vector2 r = v2 - v1;
            float rr = Vector2.Dot(r, r);
            if (rr == 0.0f)
            {
                return false;
            }

            float s = Vector2.Dot(q - v1, r) / rr;
            if (s < 0.0f || 1.0f < s)
            {
                return false;
            }

            output.Fraction = t;
            if (numerator > 0.0f)
            {
                output.Normal = -normal;
            }
            else
            {
                output.Normal = normal;
            }
            return true;
        }

        public override Box2 ComputeAABB(PhysicsTransform physicsTransform, int childIndex)
        {
            var aabb = new Box2();

            // OPT: Vector2 v1 = PhysicsTransform.Multiply(ref _vertex1, ref PhysicsTransform);
            float v1X = (_vertex1.X * physicsTransform.Quaternion.Real - _vertex1.Y * physicsTransform.Quaternion.Imaginary) + physicsTransform.Position.X;
            float v1Y = (_vertex1.Y * physicsTransform.Quaternion.Real + _vertex1.X * physicsTransform.Quaternion.Imaginary) + physicsTransform.Position.Y;
            // OPT: Vector2 v2 = PhysicsTransform.Multiply(ref _vertex2, ref PhysicsTransform);
            float v2X = (_vertex2.X * physicsTransform.Quaternion.Real - _vertex2.Y * physicsTransform.Quaternion.Imaginary) + physicsTransform.Position.X;
            float v2Y = (_vertex2.Y * physicsTransform.Quaternion.Real + _vertex2.X * physicsTransform.Quaternion.Imaginary) + physicsTransform.Position.Y;

            // OPT: aabb.LowerBound = Vector2.Min(v1, v2);
            // OPT: aabb.UpperBound = Vector2.Max(v1, v2);
            if (v1X < v2X)
            {
                aabb.Left = v1X;
                aabb.Right = v2X;
            }
            else
            {
                aabb.Left = v2X;
                aabb.Right = v1X;
            }
            if (v1Y < v2Y)
            {
                aabb.Bottom = v1Y;
                aabb.Top = v2Y;
            }
            else
            {
                aabb.Bottom = v2Y;
                aabb.Top = v1Y;
            }

            // OPT: Vector2 r = new Vector2(Radius, Radius);
            // OPT: aabb.LowerBound = aabb.LowerBound - r;
            // OPT: aabb.UpperBound = aabb.LowerBound + r;
            aabb.Left -= Radius;
            aabb.Bottom -= Radius;
            aabb.Right += Radius;
            aabb.Top += Radius;

            return aabb;
        }

        protected override void ComputeProperties()
        {
            MassData.Centroid = (_vertex1 + _vertex2) * 0.5f;
        }

        public bool CompareTo(EdgeShape shape)
        {
            return (HasVertex0 == shape.HasVertex0 &&
                    HasVertex3 == shape.HasVertex3 &&
                    Vertex0 == shape.Vertex0 &&
                    Vertex1 == shape.Vertex1 &&
                    Vertex2 == shape.Vertex2 &&
                    Vertex3 == shape.Vertex3);
        }

        public override Shape Clone()
        {
            EdgeShape clone = new EdgeShape();
            clone.ShapeType = ShapeType;
            clone._radius = _radius;
            clone._density = _density;
            clone.HasVertex0 = HasVertex0;
            clone.HasVertex3 = HasVertex3;
            clone.Vertex0 = Vertex0;
            clone._vertex1 = _vertex1;
            clone._vertex2 = _vertex2;
            clone.Vertex3 = Vertex3;
            clone.MassData = MassData;
            return clone;
        }
    }
}
