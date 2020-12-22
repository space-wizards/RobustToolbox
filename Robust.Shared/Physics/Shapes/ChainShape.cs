using System;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Shapes
{
    /// <summary>
    /// A chain shape is a free form sequence of line segments.
    /// The chain has two-sided collision, so you can use inside and outside collision.
    /// Therefore, you may use any winding order.
    /// Connectivity information is used to create smooth collisions.
    /// WARNING: The chain will not collide properly if there are self-intersections.
    /// </summary>
    public class ChainShape : Shape
    {
        /// <summary>
        /// The vertices. These are not owned/freed by the chain Shape.
        /// </summary>
        public Vertices Vertices;
        private Vector2 _prevVertex, _nextVertex;
        private bool _hasPrevVertex, _hasNextVertex;
        private static EdgeShape _edgeShape = new EdgeShape();

        /// <summary>
        /// Constructor for ChainShape. By default have 0 in density.
        /// </summary>
        public ChainShape()
            : base(0)
        {
            Vertices = new Vertices();
            ShapeType = ShapeType.Chain;
            _radius = PhysicsSettings.PolygonRadius;
        }

        /// <summary>
        /// Create a new chainshape from the vertices.
        /// </summary>
        /// <param name="vertices">The vertices to use. Must contain 2 or more vertices.</param>
        /// <param name="createLoop">Set to true to create a closed loop. It connects the first vertice to the last, and automatically adjusts connectivity to create smooth collisions along the chain.</param>
        public ChainShape(Vertices vertices, bool createLoop = false)
            : base(0)
        {
            ShapeType = ShapeType.Chain;
            _radius = PhysicsSettings.PolygonRadius;

            DebugTools.Assert(vertices.Count >= 3);
            DebugTools.Assert(vertices[0] != vertices[vertices.Count - 1]); // FPE. See http://www.box2d.org/forum/viewtopic.php?f=4&t=7973&p=35363

            for (int i = 1; i < vertices.Count; ++i)
            {
                Vector2 v1 = vertices[i - 1];
                Vector2 v2 = vertices[i];

                // If the code crashes here, it means your vertices are too close together.
                DebugTools.Assert(Vector2.DistanceSquared(v1, v2) > PhysicsSettings.LinearSlop * PhysicsSettings.LinearSlop);
            }

            Vertices = new Vertices(vertices);

            if (createLoop)
            {
                Vertices.Add(vertices[0]);
                PrevVertex = Vertices[Vertices.Count - 2]; //FPE: We use the properties instead of the private fields here.
                NextVertex = Vertices[1]; //FPE: We use the properties instead of the private fields here.
            }
        }

        public override int ChildCount
        {
            // edge count = vertex count - 1
            get { return Vertices.Count - 1; }
        }

        /// <summary>
        /// Establish connectivity to a vertex that precedes the first vertex.
        /// Don't call this for loops.
        /// </summary>
        public Vector2 PrevVertex
        {
            get { return _prevVertex; }
            set
            {
                DebugTools.Assert(value != null);

                _prevVertex = value;
                _hasPrevVertex = true;
            }
        }

        /// <summary>
        /// Establish connectivity to a vertex that follows the last vertex.
        /// Don't call this for loops.
        /// </summary>
        public Vector2 NextVertex
        {
            get { return _nextVertex; }
            set
            {
                DebugTools.Assert(value != null);

                _nextVertex = value;
                _hasNextVertex = true;
            }
        }

        /// <summary>
        /// This method has been optimized to reduce garbage.
        /// </summary>
        /// <param name="edge">The cached edge to set properties on.</param>
        /// <param name="index">The index.</param>
        internal void GetChildEdge(EdgeShape edge, int index)
        {
            DebugTools.Assert(0 <= index && index < Vertices.Count - 1);

            edge.ShapeType = ShapeType.Edge;
            edge.Radius = _radius;

            edge.Vertex1 = Vertices[index + 0];
            edge.Vertex2 = Vertices[index + 1];

            if (index > 0)
            {
                edge.Vertex0 = Vertices[index - 1];
                edge.HasVertex0 = true;
            }
            else
            {
                edge.Vertex0 = _prevVertex;
                edge.HasVertex0 = _hasPrevVertex;
            }

            if (index < Vertices.Count - 2)
            {
                edge.Vertex3 = Vertices[index + 2];
                edge.HasVertex3 = true;
            }
            else
            {
                edge.Vertex3 = _nextVertex;
                edge.HasVertex3 = _hasNextVertex;
            }
        }

        /// <summary>
        /// Get a child edge.
        /// </summary>
        /// <param name="index">The index.</param>
        public EdgeShape GetChildEdge(int index)
        {
            EdgeShape edgeShape = new EdgeShape();
            GetChildEdge(edgeShape, index);
            return edgeShape;
        }

        public override bool TestPoint(ref PhysicsTransform physicsTransform, ref Vector2 point)
        {
            return false;
        }

        public override bool RayCast(out RayCastOutput output, ref RayCastInput input, PhysicsTransform physicsTransform, int childIndex)
        {
            DebugTools.Assert(childIndex < Vertices.Count);

            int i1 = childIndex;
            int i2 = childIndex + 1;
            if (i2 == Vertices.Count)
            {
                i2 = 0;
            }

            _edgeShape.Vertex1 = Vertices[i1];
            _edgeShape.Vertex2 = Vertices[i2];

            return _edgeShape.RayCast(out output, ref input, physicsTransform, 0);
        }

        public override Box2 ComputeAABB(PhysicsTransform physicsTransform, int childIndex)
        {
            var aabb = new Box2();

            DebugTools.Assert(childIndex < Vertices.Count);

            int i1 = childIndex;
            int i2 = childIndex + 1;
            if (i2 == Vertices.Count)
            {
                i2 = 0;
            }

            Vector2 v1 = PhysicsTransform.Multiply(Vertices[i1], ref physicsTransform);
            Vector2 v2 = PhysicsTransform.Multiply(Vertices[i2], ref physicsTransform);

            aabb.Left = Math.Min(v1.X, v2.X);
            aabb.Right = Math.Max(v1.X, v2.X);
            aabb.Bottom = Math.Min(v1.Y, v2.Y);
            aabb.Top = Math.Max(v1.Y, v2.Y);

            return aabb;
        }

        protected override void ComputeProperties()
        {
            //Does nothing. Chain shapes don't have properties.
        }

        /// <summary>
        /// Compare the chain to another chain
        /// </summary>
        /// <param name="shape">The other chain</param>
        /// <returns>True if the two chain shapes are the same</returns>
        public bool CompareTo(ChainShape shape)
        {
            if (Vertices.Count != shape.Vertices.Count)
                return false;

            for (int i = 0; i < Vertices.Count; i++)
            {
                if (Vertices[i] != shape.Vertices[i])
                    return false;
            }

            return PrevVertex == shape.PrevVertex && NextVertex == shape.NextVertex;
        }

        public override Shape Clone()
        {
            ChainShape clone = new ChainShape();
            clone.ShapeType = ShapeType;
            clone._density = _density;
            clone._radius = _radius;
            clone.PrevVertex = _prevVertex;
            clone.NextVertex = _nextVertex;
            clone._hasNextVertex = _hasNextVertex;
            clone._hasPrevVertex = _hasPrevVertex;
            clone.Vertices = new Vertices(Vertices);
            clone.MassData = MassData;
            return clone;
        }
    }
}
