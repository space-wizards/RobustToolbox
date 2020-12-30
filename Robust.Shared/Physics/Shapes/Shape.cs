using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Shapes
{
    /// <summary>
    /// A shape is used for collision detection. You can create a shape however you like.
    /// Shapes used for simulation in World are created automatically when a Fixture
    /// is created. Shapes may encapsulate a one or more child shapes.
    /// </summary>
    public abstract class Shape : IExposeData
    {
        /// <summary>
        /// Gets or sets the density.
        /// Changing the density causes a recalculation of shape properties.
        /// </summary>
        /// <value>The density.</value>
        public float Density
        {
            get => _density;
            set
            {
                if (_density == value)
                    return;

                DebugTools.Assert(value >= 0);

                _density = value;
                ComputeProperties();
            }
        }

        protected float _density;

        /// <summary>
        /// Radius of the Shape
        /// Changing the radius causes a recalculation of shape properties.
        /// </summary>
        public float Radius
        {
            get => _radius;
            set
            {
                DebugTools.Assert(value >= 0);

                _radius = value;
                _2radius = _radius * _radius;

                ComputeProperties();
            }
        }

        protected float _radius;

        public float _2radius;

        /// <summary>
        /// Contains the properties of the shape such as:
        /// - Area of the shape
        /// - Centroid
        /// - Inertia
        /// - Mass
        /// </summary>
        public MassData MassData;

        /// <summary>
        /// Get the type of this shape.
        /// </summary>
        /// <value>The type of the shape.</value>
        public ShapeType ShapeType { get; set; }

        /// <summary>
        /// Get the number of child primitives.
        /// </summary>
        /// <value></value>
        public abstract int ChildCount { get; }

        public Shape(float density)
        {
            _density = density;
            ShapeType = ShapeType.Unknown;
        }

        /// <summary>
        /// Clone the concrete shape
        /// </summary>
        /// <returns>A clone of the shape</returns>
        public abstract Shape Clone();

        /// <summary>
        /// Test a point for containment in this shape.
        /// Note: This only works for convex shapes.
        /// </summary>
        /// <param name="transform">The shape world transform.</param>
        /// <param name="point">A point in world coordinates.</param>
        /// <returns>True if the point is inside the shape</returns>
        public abstract bool TestPoint(ref PhysicsTransform transform, ref Vector2 point);

        /// <summary>
        /// Cast a ray against a child shape.
        /// </summary>
        /// <param name="output">The ray-cast results.</param>
        /// <param name="input">The ray-cast input parameters.</param>
        /// <param name="transform">The transform to be applied to the shape.</param>
        /// <param name="childIndex">The child shape index.</param>
        /// <returns>True if the ray-cast hits the shape</returns>
        public abstract bool RayCast(out RayCastOutput output, ref CollisionRay input, PhysicsTransform transform, int childIndex);

        /// <summary>
        /// Given a transform, compute the associated axis aligned bounding box for a child shape.
        /// </summary>
        /// <param name="physicsTransform">The world transform of the shape.</param>
        /// <param name="childIndex">The child shape index.</param>
        public abstract Box2 ComputeAABB(PhysicsTransform physicsTransform, int childIndex);

        /// <summary>
        /// Compute the mass properties of this shape using its dimensions and density.
        /// The inertia tensor is computed about the local origin, not the centroid.
        /// </summary>
        protected abstract void ComputeProperties();

        // TODO: Someday sweet prince
        //<summary>
        //Used for the buoyancy controller
        //</summary>
        //public abstract float ComputeSubmergedArea(ref Vector2 normal, float offset, ITransformComponent transform, out Vector2 sc);
        public virtual void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _density, "density", 1.0f);
        }
    }

    /// <summary>
    /// Ray-cast output data.
    /// </summary>
    public struct RayCastOutput
    {
        /// <summary>
        /// The ray hits at p1 + fraction * (p2 - p1), where p1 and p2 come from RayCastInput.
        /// Contains the actual fraction of the ray where it has the intersection point.
        /// </summary>
        public float Fraction;

        /// <summary>
        /// The normal of the face of the shape the ray has hit.
        /// </summary>
        public Vector2 Normal;
    }

    public enum ShapeType : sbyte
    {
        Unknown = -1,
        Circle = 0,
        Edge = 1,
        Polygon = 2,
        Chain = 3,
        TypeCount = 4,
    }
}
