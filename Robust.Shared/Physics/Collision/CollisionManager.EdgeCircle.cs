using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Collision;

internal sealed partial class CollisionManager
{
    /// <summary>
        /// Compute contact points for edge versus circle.
        /// This accounts for edge connectivity.
        /// </summary>
        /// <param name="manifold">The manifold.</param>
        /// <param name="edgeA">The edge A.</param>
        /// <param name="transformA">The transform A.</param>
        /// <param name="circleB">The circle B.</param>
        /// <param name="transformB">The transform B.</param>
        public void CollideEdgeAndCircle(ref Manifold manifold, EdgeShape edgeA, in Transform transformA,
            PhysShapeCircle circleB, in Transform transformB)
        {
            manifold.PointCount = 0;

	        // Compute circle in frame of edge
	        var Q = Transform.MulT(transformA, Transform.Mul(transformB, circleB.Position));

            var A = edgeA.Vertex1;
            var B = edgeA.Vertex2;
	        var e = B - A;

	        // Normal points to the right for a CCW winding
	        var n = new Vector2(e.Y, -e.X);
	        float offset = Vector2.Dot(n, Q - A);

	        bool oneSided = edgeA.OneSided;
	        if (oneSided && offset < 0.0f)
                return;

            // Barycentric coordinates
	        float u = Vector2.Dot(e, B - Q);
	        float v = Vector2.Dot(e, Q - A);

	        float radius = edgeA.Radius + circleB.Radius;

	        ContactFeature cf = new ContactFeature();
	        cf.IndexB = 0;
	        cf.TypeB = (byte) ContactFeatureType.Vertex;

            Vector2 P;
            Vector2 d;
            float dd;

	        // Region A
	        if (v <= 0.0f)
	        {
		        P = A;
		        d = Q - P;
		        dd = Vector2.Dot(d, d);
		        if (dd > radius * radius)
                    return;

                // Is there an edge connected to A?
		        if (edgeA.OneSided)
		        {
			        var A1 = edgeA.Vertex0;
			        var B1 = A;
			        var e1 = B1 - A1;
			        float u1 = Vector2.Dot(e1, B1 - Q);

			        // Is the circle in Region AB of the previous edge?
			        if (u1 > 0.0f)
                        return;

		        }

		        cf.IndexA = 0;
		        cf.TypeA = (byte) ContactFeatureType.Vertex;
		        manifold.PointCount = 1;
		        manifold.Type = ManifoldType.Circles;
		        manifold.LocalNormal = Vector2.Zero;
		        manifold.LocalPoint = P;
		        manifold.Points[0].Id.Key = 0;
		        manifold.Points[0].Id.Features = cf;
		        manifold.Points[0].LocalPoint = circleB.Position;
		        return;
	        }

	        // Region B
	        if (u <= 0.0f)
	        {
		        P = B;
		        d = Q - P;
		        dd = Vector2.Dot(d, d);
		        if (dd > radius * radius)
                    return;

                // Is there an edge connected to B?
		        if (edgeA.OneSided)
		        {
			        var B2 = edgeA.Vertex3;
			        var A2 = B;
			        var e2 = B2 - A2;
			        float v2 = Vector2.Dot(e2, Q - A2);

			        // Is the circle in Region AB of the next edge?
			        if (v2 > 0.0f)
                        return;

		        }

		        cf.IndexA = 1;
		        cf.TypeA = (byte) ContactFeatureType.Vertex;
		        manifold.PointCount = 1;
		        manifold.Type = ManifoldType.Circles;
		        manifold.LocalNormal = Vector2.Zero;
		        manifold.LocalPoint = P;
		        manifold.Points[0].Id.Key = 0;
		        manifold.Points[0].Id.Features = cf;
		        manifold.Points[0].LocalPoint = circleB.Position;
		        return;
	        }

	        // Region AB
	        float den = Vector2.Dot(e, e);
	        DebugTools.Assert(den > 0.0f);
	        P = (A * u + B * v) * (1.0f / den);
	        d = Q - P;
	        dd = Vector2.Dot(d, d);
	        if (dd > radius * radius)
                return;

            if (offset < 0.0f)
	        {
		        n = new Vector2(-n.X, -n.Y);
	        }

	        n = n.Normalized;

	        cf.IndexA = 0;
	        cf.TypeA = (byte) ContactFeatureType.Face;
	        manifold.PointCount = 1;
	        manifold.Type = ManifoldType.FaceA;
	        manifold.LocalNormal = n;
	        manifold.LocalPoint = A;
	        manifold.Points[0].Id.Key = 0;
	        manifold.Points[0].Id.Features = cf;
	        manifold.Points[0].LocalPoint = circleB.Position;
        }
}
