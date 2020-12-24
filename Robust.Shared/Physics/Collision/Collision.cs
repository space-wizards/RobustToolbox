using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Shapes
{
    internal enum ContactFeatureType : byte
    {
        Vertex = 0,
        Face = 1,
    }

    /// <summary>
    /// The features that intersect to form the contact point
    /// This must be 4 bytes or less.
    /// </summary>
    public struct ContactFeature
    {
        /// <summary>
        /// Feature index on ShapeA
        /// </summary>
        public byte IndexA;

        /// <summary>
        /// Feature index on ShapeB
        /// </summary>
        public byte IndexB;

        /// <summary>
        /// The feature type on ShapeA
        /// </summary>
        public byte TypeA;

        /// <summary>
        /// The feature type on ShapeB
        /// </summary>
        public byte TypeB;
    }

    /// <summary>
    /// Contact ids to facilitate warm starting.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct ContactID
    {
        /// <summary>
        /// The features that intersect to form the contact point
        /// </summary>
        [FieldOffset(0)]
        public ContactFeature Features;

        /// <summary>
        /// Used to quickly compare contact ids.
        /// </summary>
        [FieldOffset(0)]
        public uint Key;
    }

    /// <summary>
    /// A manifold point is a contact point belonging to a contact
    /// manifold. It holds details related to the geometry and dynamics
    /// of the contact points.
    /// The local point usage depends on the manifold type:
    /// -ShapeType.Circles: the local center of circleB
    /// -SeparationFunction.FaceA: the local center of cirlceB or the clip point of polygonB
    /// -SeparationFunction.FaceB: the clip point of polygonA
    /// This structure is stored across time steps, so we keep it small.
    /// Note: the impulses are used for internal caching and may not
    /// provide reliable contact forces, especially for high speed collisions.
    /// </summary>
    public struct ManifoldPoint
    {
        /// <summary>
        /// Uniquely identifies a contact point between two Shapes
        /// </summary>
        public ContactID Id;

        /// <summary>
        /// Usage depends on manifold type
        /// </summary>
        public Vector2 LocalPoint;

        /// <summary>
        /// The non-penetration impulse
        /// </summary>
        public float NormalImpulse;

        /// <summary>
        /// The friction impulse
        /// </summary>
        public float TangentImpulse;
    }

    public enum ManifoldType
    {
        Circles,
        FaceA,
        FaceB
    }

    /// <summary>
    /// A manifold for two touching convex Shapes.
    /// Box2D supports multiple types of contact:
    /// - Clip point versus plane with radius
    /// - Point versus point with radius (circles)
    /// The local point usage depends on the manifold type:
    /// - ShapeType.Circles: the local center of circleA
    /// - SeparationFunction.FaceA: the center of faceA
    /// - SeparationFunction.FaceB: the center of faceB
    /// Similarly the local normal usage:
    /// - ShapeType.Circles: not used
    /// - SeparationFunction.FaceA: the normal on polygonA
    /// - SeparationFunction.FaceB: the normal on polygonB
    /// We store contacts in this way so that position correction can
    /// account for movement, which is critical for continuous physics.
    /// All contact scenarios must be expressed in one of these types.
    /// This structure is stored across time steps, so we keep it small.
    /// </summary>
    public struct Manifold
    {
        /// <summary>
        /// Not use for Type.SeparationFunction.Points
        /// </summary>
        public Vector2 LocalNormal;

        /// <summary>
        /// Usage depends on manifold type
        /// </summary>
        public Vector2 LocalPoint;

        /// <summary>
        /// The number of manifold points
        /// </summary>
        public int PointCount;

        /// <summary>
        /// The points of contact
        /// </summary>
        public FixedArray2<ManifoldPoint> Points;

        public ManifoldType Type;
    }

    /// <summary>
    /// This is used for determining the state of contact points.
    /// </summary>
    public enum PointState
    {
        /// <summary>
        /// Point does not exist
        /// </summary>
        Null,

        /// <summary>
        /// Point was added in the update
        /// </summary>
        Add,

        /// <summary>
        /// Point persisted across the update
        /// </summary>
        Persist,

        /// <summary>
        /// Point was removed in the update
        /// </summary>
        Remove,
    }

    /// <summary>
    /// Used for computing contact manifolds.
    /// </summary>
    public struct ClipVertex
    {
        public ContactID ID;
        public Vector2 V;
    }

    // AABB should be here but we already have Box2

    /// <summary>
    /// This structure is used to keep track of the best separating axis.
    /// </summary>
    public struct EPAxis
    {
        public int Index;
        public float Separation;
        public EPAxisType Type;
    }

    /// <summary>
    /// Reference face used for clipping
    /// </summary>
    public struct ReferenceFace
    {
        public int i1, i2;

        public Vector2 v1, v2;

        public Vector2 normal;

        public Vector2 sideNormal1;
        public float sideOffset1;

        public Vector2 sideNormal2;
        public float sideOffset2;
    }

    public enum EPAxisType
    {
        Unknown,
        EdgeA,
        EdgeB,
    }

    /// <summary>
    /// Collision methods
    /// </summary>
    public static class Collision
    {
        /// <summary>
        /// Test overlap between the two shapes.
        /// </summary>
        /// <param name="shapeA">The first shape.</param>
        /// <param name="indexA">The index for the first shape.</param>
        /// <param name="shapeB">The second shape.</param>
        /// <param name="indexB">The index for the second shape.</param>
        /// <param name="xfA">The PhysicsTransform for the first shape.</param>
        /// <param name="xfB">The PhysicsTransform for the seconds shape.</param>
        /// <returns></returns>
        public static bool TestOverlap(Shape shapeA, int indexA, Shape shapeB, int indexB, ref PhysicsTransform xfA, ref PhysicsTransform xfB)
        {
            DistanceInput _input = new DistanceInput();
            _input.ProxyA = new DistanceProxy(shapeA, indexA);
            _input.ProxyB = new DistanceProxy(shapeB, indexB);
            _input.TransformA = xfA;
            _input.TransformB = xfB;
            _input.UseRadii = true;

            SimplexCache cache;
            DistanceOutput output;
            Distance.ComputeDistance(out output, out cache, _input);

            return output.Distance < 10.0f * float.Epsilon;
        }

        public static void GetPointStates(out FixedArray2<PointState> state1, out FixedArray2<PointState> state2, ref Manifold manifold1, ref Manifold manifold2)
        {
            state1 = new FixedArray2<PointState>();
            state2 = new FixedArray2<PointState>();

            // Detect persists and removes.
            for (int i = 0; i < manifold1.PointCount; ++i)
            {
                ContactID id = manifold1.Points[i].Id;

                state1[i] = PointState.Remove;

                for (int j = 0; j < manifold2.PointCount; ++j)
                {
                    if (manifold2.Points[j].Id.Key == id.Key)
                    {
                        state1[i] = PointState.Persist;
                        break;
                    }
                }
            }

            // Detect persists and adds.
            for (int i = 0; i < manifold2.PointCount; ++i)
            {
                ContactID id = manifold2.Points[i].Id;

                state2[i] = PointState.Add;

                for (int j = 0; j < manifold1.PointCount; ++j)
                {
                    if (manifold1.Points[j].Id.Key == id.Key)
                    {
                        state2[i] = PointState.Persist;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Compute the collision manifold between two circles.
        /// </summary>
        public static void CollideCircles(ref Manifold manifold, CircleShape circleA, ref PhysicsTransform xfA, CircleShape circleB, ref PhysicsTransform xfB)
        {
            manifold.PointCount = 0;

            Vector2 pA = PhysicsTransform.Multiply(ref circleA._position, ref xfA);
            Vector2 pB = PhysicsTransform.Multiply(ref circleB._position, ref xfB);

            Vector2 d = pB - pA;
            float distSqr = Vector2.Dot(d, d);
            float radius = circleA.Radius + circleB.Radius;
            if (distSqr > radius * radius)
            {
                return;
            }

            manifold.Type = ManifoldType.Circles;
            manifold.LocalPoint = circleA.Position;
            manifold.LocalNormal = Vector2.Zero;
            manifold.PointCount = 1;

            ManifoldPoint p0 = manifold.Points[0];

            p0.LocalPoint = circleB.Position;
            p0.Id.Key = 0;

            manifold.Points[0] = p0;
        }

        /// <summary>
        /// Compute the collision manifold between a polygon and a circle.
        /// </summary>
        /// <param name="manifold">The manifold.</param>
        /// <param name="polygonA">The polygon A.</param>
        /// <param name="xfA">The PhysicsTransform of A.</param>
        /// <param name="circleB">The circle B.</param>
        /// <param name="xfB">The PhysicsTransform of B.</param>
        public static void CollidePolygonAndCircle(ref Manifold manifold, PolygonShape polygonA, ref PhysicsTransform xfA, CircleShape circleB, ref PhysicsTransform xfB)
        {
            manifold.PointCount = 0;

            // Compute circle position in the frame of the polygon.
            Vector2 c = PhysicsTransform.Multiply(ref circleB._position, ref xfB);
            Vector2 cLocal = PhysicsTransform.Divide(ref c, ref xfA);

            // Find the min separating edge.
            int normalIndex = 0;
            float separation = float.MinValue;
            float radius = polygonA.Radius + circleB.Radius;
            int vertexCount = polygonA.Vertices.Count;

            for (int i = 0; i < vertexCount; ++i)
            {
                Vector2 value1 = polygonA.Normals[i];
                Vector2 value2 = cLocal - polygonA.Vertices[i];
                float s = value1.X * value2.X + value1.Y * value2.Y;

                if (s > radius)
                {
                    // Early out.
                    return;
                }

                if (s > separation)
                {
                    separation = s;
                    normalIndex = i;
                }
            }

            // Vertices that subtend the incident face.
            int vertIndex1 = normalIndex;
            int vertIndex2 = vertIndex1 + 1 < vertexCount ? vertIndex1 + 1 : 0;
            Vector2 v1 = polygonA.Vertices[vertIndex1];
            Vector2 v2 = polygonA.Vertices[vertIndex2];

            // If the center is inside the polygon ...
            if (separation < float.Epsilon)
            {
                manifold.PointCount = 1;
                manifold.Type = ManifoldType.FaceA;
                manifold.LocalNormal = polygonA.Normals[normalIndex];
                manifold.LocalPoint = (v1 + v2) * 0.5f;

                ManifoldPoint p0 = manifold.Points[0];

                p0.LocalPoint = circleB.Position;
                p0.Id.Key = 0;

                manifold.Points[0] = p0;

                return;
            }

            // Compute barycentric coordinates
            float u1 = (cLocal.X - v1.X) * (v2.X - v1.X) + (cLocal.Y - v1.Y) * (v2.Y - v1.Y);
            float u2 = (cLocal.X - v2.X) * (v1.X - v2.X) + (cLocal.Y - v2.Y) * (v1.Y - v2.Y);

            if (u1 <= 0.0f)
            {
                float r = (cLocal.X - v1.X) * (cLocal.X - v1.X) + (cLocal.Y - v1.Y) * (cLocal.Y - v1.Y);
                if (r > radius * radius)
                {
                    return;
                }

                manifold.PointCount = 1;
                manifold.Type = ManifoldType.FaceA;
                manifold.LocalNormal = cLocal - v1;
                float factor = 1f /
                               (float)
                               Math.Sqrt(manifold.LocalNormal.X * manifold.LocalNormal.X +
                                         manifold.LocalNormal.Y * manifold.LocalNormal.Y);
                manifold.LocalNormal.X = manifold.LocalNormal.X * factor;
                manifold.LocalNormal.Y = manifold.LocalNormal.Y * factor;
                manifold.LocalPoint = v1;

                ManifoldPoint p0b = manifold.Points[0];

                p0b.LocalPoint = circleB.Position;
                p0b.Id.Key = 0;

                manifold.Points[0] = p0b;
            }
            else if (u2 <= 0.0f)
            {
                float r = (cLocal.X - v2.X) * (cLocal.X - v2.X) + (cLocal.Y - v2.Y) * (cLocal.Y - v2.Y);
                if (r > radius * radius)
                {
                    return;
                }

                manifold.PointCount = 1;
                manifold.Type = ManifoldType.FaceA;
                manifold.LocalNormal = cLocal - v2;
                float factor = 1f /
                               (float)
                               Math.Sqrt(manifold.LocalNormal.X * manifold.LocalNormal.X +
                                         manifold.LocalNormal.Y * manifold.LocalNormal.Y);
                manifold.LocalNormal.X = manifold.LocalNormal.X * factor;
                manifold.LocalNormal.Y = manifold.LocalNormal.Y * factor;
                manifold.LocalPoint = v2;

                ManifoldPoint p0c = manifold.Points[0];

                p0c.LocalPoint = circleB.Position;
                p0c.Id.Key = 0;

                manifold.Points[0] = p0c;
            }
            else
            {
                Vector2 faceCenter = (v1 + v2) * 0.5f;
                Vector2 value1 = cLocal - faceCenter;
                Vector2 value2 = polygonA.Normals[vertIndex1];
                float separation2 = value1.X * value2.X + value1.Y * value2.Y;
                if (separation2 > radius)
                {
                    return;
                }

                manifold.PointCount = 1;
                manifold.Type = ManifoldType.FaceA;
                manifold.LocalNormal = polygonA.Normals[vertIndex1];
                manifold.LocalPoint = faceCenter;

                ManifoldPoint p0d = manifold.Points[0];

                p0d.LocalPoint = circleB.Position;
                p0d.Id.Key = 0;

                manifold.Points[0] = p0d;
            }
        }

        /// <summary>
        /// Compute the collision manifold between two polygons.
        /// </summary>
        /// <param name="manifold">The manifold.</param>
        /// <param name="polyA">The poly A.</param>
        /// <param name="PhysicsTransformA">The PhysicsTransform A.</param>
        /// <param name="polyB">The poly B.</param>
        /// <param name="PhysicsTransformB">The PhysicsTransform B.</param>
        public static void CollidePolygons(ref Manifold manifold, PolygonShape polyA, ref PhysicsTransform PhysicsTransformA, PolygonShape polyB, ref PhysicsTransform PhysicsTransformB)
        {
            manifold.PointCount = 0;
            float totalRadius = polyA.Radius + polyB.Radius;

            int edgeA = 0;
            float separationA = FindMaxSeparation(out edgeA, polyA, ref PhysicsTransformA, polyB, ref PhysicsTransformB);
            if (separationA > totalRadius)
                return;

            int edgeB = 0;
            float separationB = FindMaxSeparation(out edgeB, polyB, ref PhysicsTransformB, polyA, ref PhysicsTransformA);
            if (separationB > totalRadius)
                return;

            PolygonShape poly1; // reference polygon
            PolygonShape poly2; // incident polygon
            PhysicsTransform xf1, xf2;
            int edge1; // reference edge
            bool flip;
            const float k_relativeTol = 0.98f;
            const float k_absoluteTol = 0.001f;

            if (separationB > k_relativeTol * separationA + k_absoluteTol)
            {
                poly1 = polyB;
                poly2 = polyA;
                xf1 = PhysicsTransformB;
                xf2 = PhysicsTransformA;
                edge1 = edgeB;
                manifold.Type = ManifoldType.FaceB;
                flip = true;
            }
            else
            {
                poly1 = polyA;
                poly2 = polyB;
                xf1 = PhysicsTransformA;
                xf2 = PhysicsTransformB;
                edge1 = edgeA;
                manifold.Type = ManifoldType.FaceA;
                flip = false;
            }

            FixedArray2<ClipVertex> incidentEdge;
            FindIncidentEdge(out incidentEdge, poly1, ref xf1, edge1, poly2, ref xf2);

            int count1 = poly1.Vertices.Count;

            int iv1 = edge1;
            int iv2 = edge1 + 1 < count1 ? edge1 + 1 : 0;

            Vector2 v11 = poly1.Vertices[iv1];
            Vector2 v12 = poly1.Vertices[iv2];

            Vector2 localTangent = v12 - v11;
            localTangent = localTangent.Normalized;

            Vector2 localNormal = new Vector2(localTangent.Y, -localTangent.X);
            Vector2 planePoint = (v11 + v12) * 0.5f;

            Vector2 tangent = Complex.Multiply(localTangent, ref xf1.Quaternion);

            float normalx = tangent.Y;
            float normaly = -tangent.X;

            v11 = PhysicsTransform.Multiply(ref v11, ref xf1);
            v12 = PhysicsTransform.Multiply(ref v12, ref xf1);

            // Face offset.
            float frontOffset = normalx * v11.X + normaly * v11.Y;

            // Side offsets, extended by polytope skin thickness.
            float sideOffset1 = -(tangent.X * v11.X + tangent.Y * v11.Y) + totalRadius;
            float sideOffset2 = tangent.X * v12.X + tangent.Y * v12.Y + totalRadius;

            // Clip incident edge against extruded edge1 side edges.
            FixedArray2<ClipVertex> clipPoints1;
            FixedArray2<ClipVertex> clipPoints2;

            // Clip to box side 1
            int np = ClipSegmentToLine(out clipPoints1, ref incidentEdge, -tangent, sideOffset1, iv1);

            if (np < 2)
                return;

            // Clip to negative box side 1
            np = ClipSegmentToLine(out clipPoints2, ref clipPoints1, tangent, sideOffset2, iv2);

            if (np < 2)
            {
                return;
            }

            // Now clipPoints2 contains the clipped points.
            manifold.LocalNormal = localNormal;
            manifold.LocalPoint = planePoint;

            int pointCount = 0;
            for (int i = 0; i < PhysicsSettings.MaxManifoldPoints; ++i)
            {
                Vector2 value = clipPoints2[i].V;
                float separation = normalx * value.X + normaly * value.Y - frontOffset;

                if (separation <= totalRadius)
                {
                    ManifoldPoint cp = manifold.Points[pointCount];
                    PhysicsTransform.Divide(clipPoints2[i].V, ref xf2, out cp.LocalPoint);
                    cp.Id = clipPoints2[i].ID;

                    if (flip)
                    {
                        // Swap features
                        ContactFeature cf = cp.Id.Features;
                        cp.Id.Features.IndexA = cf.IndexB;
                        cp.Id.Features.IndexB = cf.IndexA;
                        cp.Id.Features.TypeA = cf.TypeB;
                        cp.Id.Features.TypeB = cf.TypeA;
                    }

                    manifold.Points[pointCount] = cp;

                    ++pointCount;
                }
            }

            manifold.PointCount = pointCount;
        }

        /// <summary>
        /// Compute contact points for edge versus circle.
        /// This accounts for edge connectivity.
        /// </summary>
        /// <param name="manifold">The manifold.</param>
        /// <param name="edgeA">The edge A.</param>
        /// <param name="PhysicsTransformA">The PhysicsTransform A.</param>
        /// <param name="circleB">The circle B.</param>
        /// <param name="PhysicsTransformB">The PhysicsTransform B.</param>
        public static void CollideEdgeAndCircle(ref Manifold manifold, EdgeShape edgeA, ref PhysicsTransform PhysicsTransformA, CircleShape circleB, ref PhysicsTransform PhysicsTransformB)
        {
            manifold.PointCount = 0;

            // Compute circle in frame of edge
            Vector2 Q = PhysicsTransform.Divide(PhysicsTransform.Multiply(ref circleB._position, ref PhysicsTransformB), ref PhysicsTransformA);

            Vector2 A = edgeA.Vertex1, B = edgeA.Vertex2;
            Vector2 e = B - A;

            // Barycentric coordinates
            float u = Vector2.Dot(e, B - Q);
            float v = Vector2.Dot(e, Q - A);

            float radius = edgeA.Radius + circleB.Radius;

            ContactFeature cf;
            cf.IndexB = 0;
            cf.TypeB = (byte)ContactFeatureType.Vertex;

            Vector2 P, d;

            // Region A
            if (v <= 0.0f)
            {
                P = A;
                d = Q - P;
                var dd = Vector2.Dot(d, d);

                if (dd > radius * radius)
                {
                    return;
                }

                // Is there an edge connected to A?
                if (edgeA.HasVertex0)
                {
                    Vector2 A1 = edgeA.Vertex0;
                    Vector2 B1 = A;
                    Vector2 e1 = B1 - A1;
                    float u1 = Vector2.Dot(e1, B1 - Q);

                    // Is the circle in Region AB of the previous edge?
                    if (u1 > 0.0f)
                    {
                        return;
                    }
                }

                cf.IndexA = 0;
                cf.TypeA = (byte)ContactFeatureType.Vertex;
                manifold.PointCount = 1;
                manifold.Type = ManifoldType.Circles;
                manifold.LocalNormal = Vector2.Zero;
                manifold.LocalPoint = P;
                ManifoldPoint mp = new ManifoldPoint();
                mp.Id.Key = 0;
                mp.Id.Features = cf;
                mp.LocalPoint = circleB.Position;
                manifold.Points[0] = mp;
                return;
            }

            // Region B
            if (u <= 0.0f)
            {
                P = B;
                d = Q - P;
                var dd = Vector2.Dot(d, d);
                if (dd > radius * radius)
                {
                    return;
                }

                // Is there an edge connected to B?
                if (edgeA.HasVertex3)
                {
                    Vector2 B2 = edgeA.Vertex3;
                    Vector2 A2 = B;
                    Vector2 e2 = B2 - A2;
                    float v2 = Vector2.Dot(e2, Q - A2);

                    // Is the circle in Region AB of the next edge?
                    if (v2 > 0.0f)
                    {
                        return;
                    }
                }

                cf.IndexA = 1;
                cf.TypeA = (byte)ContactFeatureType.Vertex;
                manifold.PointCount = 1;
                manifold.Type = ManifoldType.Circles;
                manifold.LocalNormal = Vector2.Zero;
                manifold.LocalPoint = P;
                ManifoldPoint mp = new ManifoldPoint();
                mp.Id.Key = 0;
                mp.Id.Features = cf;
                mp.LocalPoint = circleB.Position;
                manifold.Points[0] = mp;
                return;
            }

            // Region AB
            var den = Vector2.Dot(e, e);
            Debug.Assert(den > 0.0f);
            P = (A * u + B * v) * (1.0f / den);
            d = Q - P;
            var dd2 = Vector2.Dot(d, d);
            if (dd2 > radius * radius)
            {
                return;
            }

            Vector2 n = new Vector2(-e.Y, e.X);
            if (Vector2.Dot(n, Q - A) < 0.0f)
            {
                n = new Vector2(-n.X, -n.Y);
            }

            n = n.Normalized;

            cf.IndexA = 0;
            cf.TypeA = (byte)ContactFeatureType.Face;
            manifold.PointCount = 1;
            manifold.Type = ManifoldType.FaceA;
            manifold.LocalNormal = n;
            manifold.LocalPoint = A;
            ManifoldPoint mp2 = new ManifoldPoint();
            mp2.Id.Key = 0;
            mp2.Id.Features = cf;
            mp2.LocalPoint = circleB.Position;
            manifold.Points[0] = mp2;
        }

        /// <summary>
        /// Collides and edge and a polygon, taking into account edge adjacency.
        /// </summary>
        /// <param name="manifold">The manifold.</param>
        /// <param name="edgeA">The edge A.</param>
        /// <param name="xfA">The xf A.</param>
        /// <param name="polygonB">The polygon B.</param>
        /// <param name="xfB">The xf B.</param>
        public static void CollideEdgeAndPolygon(ref Manifold manifold, EdgeShape edgeA, ref PhysicsTransform xfA, PolygonShape polygonB, ref PhysicsTransform xfB)
        {
            EPCollider.Collide(ref manifold, edgeA, ref xfA, polygonB, ref xfB);
        }

        private static class EPCollider
        {
            /// <summary>
            /// This holds polygon B expressed in frame A.
            /// </summary>
            internal struct TempPolygon
            {
                public Vector2[] Vertices;
                public Vector2[] Normals;
                public int Count;

                internal TempPolygon(int maxPolygonVertices)
                {
                    Vertices = new Vector2[maxPolygonVertices];
                    Normals = new Vector2[maxPolygonVertices];
                    Count = 0;
                }
            }

            public static void Collide(ref Manifold manifold, EdgeShape edgeA, ref PhysicsTransform xfA, PolygonShape polygonB, ref PhysicsTransform xfB)
            {
                // Algorithm:
                // 1. Classify v1 and v2
                // 2. Classify polygon centroid as front or back
                // 3. Flip normal if necessary
                // 4. Initialize normal range to [-pi, pi] about face normal
                // 5. Adjust normal range according to adjacent edges
                // 6. Visit each separating axes, only accept axes within the range
                // 7. Return if _any_ axis indicates separation
                // 8. Clip

                TempPolygon tempPolygonB = new TempPolygon(PhysicsSettings.MaxPolygonVertices);
                PhysicsTransform xf;
                Vector2 centroidB;
                Vector2 normal0 = new Vector2();
                Vector2 normal1;
                Vector2 normal2 = new Vector2();
                Vector2 normal;
                Vector2 lowerLimit, upperLimit;
                float radius;
                bool front;

                PhysicsTransform.Divide(ref xfB, ref xfA, out xf);

                centroidB = PhysicsTransform.Multiply(polygonB.MassData.Centroid, ref xf);

                Vector2 v0 = edgeA.Vertex0;
                Vector2 v1 = edgeA._vertex1;
                Vector2 v2 = edgeA._vertex2;
                Vector2 v3 = edgeA.Vertex3;

                bool hasVertex0 = edgeA.HasVertex0;
                bool hasVertex3 = edgeA.HasVertex3;

                Vector2 edge1 = v2 - v1;
                edge1 = edge1.Normalized;
                normal1 = new Vector2(edge1.Y, -edge1.X);
                float offset1 = Vector2.Dot(normal1, centroidB - v1);
                float offset0 = 0.0f, offset2 = 0.0f;
                bool convex1 = false, convex2 = false;

                // Is there a preceding edge?
                if (hasVertex0)
                {
                    Vector2 edge0 = v1 - v0;
                    edge0 = edge0.Normalized;
                    normal0 = new Vector2(edge0.Y, -edge0.X);
                    convex1 = Vector2.Cross(edge0, edge1) >= 0.0f;
                    offset0 = Vector2.Dot(normal0, centroidB - v0);
                }

                // Is there a following edge?
                if (hasVertex3)
                {
                    Vector2 edge2 = v3 - v2;
                    edge2 = edge2.Normalized;
                    normal2 = new Vector2(edge2.Y, -edge2.X);
                    convex2 = Vector2.Cross(edge1, edge2) > 0.0f;
                    offset2 = Vector2.Dot(normal2, centroidB - v2);
                }

                // Determine front or back collision. Determine collision normal limits.
                if (hasVertex0 && hasVertex3)
                {
                    if (convex1 && convex2)
                    {
                        front = offset0 >= 0.0f || offset1 >= 0.0f || offset2 >= 0.0f;
                        if (front)
                        {
                            normal = normal1;
                            lowerLimit = normal0;
                            upperLimit = normal2;
                        }
                        else
                        {
                            normal = -normal1;
                            lowerLimit = -normal1;
                            upperLimit = -normal1;
                        }
                    }
                    else if (convex1)
                    {
                        front = offset0 >= 0.0f || (offset1 >= 0.0f && offset2 >= 0.0f);
                        if (front)
                        {
                            normal = normal1;
                            lowerLimit = normal0;
                            upperLimit = normal1;
                        }
                        else
                        {
                            normal = -normal1;
                            lowerLimit = -normal2;
                            upperLimit = -normal1;
                        }
                    }
                    else if (convex2)
                    {
                        front = offset2 >= 0.0f || (offset0 >= 0.0f && offset1 >= 0.0f);
                        if (front)
                        {
                            normal = normal1;
                            lowerLimit = normal1;
                            upperLimit = normal2;
                        }
                        else
                        {
                            normal = -normal1;
                            lowerLimit = -normal1;
                            upperLimit = -normal0;
                        }
                    }
                    else
                    {
                        front = offset0 >= 0.0f && offset1 >= 0.0f && offset2 >= 0.0f;
                        if (front)
                        {
                            normal = normal1;
                            lowerLimit = normal1;
                            upperLimit = normal1;
                        }
                        else
                        {
                            normal = -normal1;
                            lowerLimit = -normal2;
                            upperLimit = -normal0;
                        }
                    }
                }
                else if (hasVertex0)
                {
                    if (convex1)
                    {
                        front = offset0 >= 0.0f || offset1 >= 0.0f;
                        if (front)
                        {
                            normal = normal1;
                            lowerLimit = normal0;
                            upperLimit = -normal1;
                        }
                        else
                        {
                            normal = -normal1;
                            lowerLimit = normal1;
                            upperLimit = -normal1;
                        }
                    }
                    else
                    {
                        front = offset0 >= 0.0f && offset1 >= 0.0f;
                        if (front)
                        {
                            normal = normal1;
                            lowerLimit = normal1;
                            upperLimit = -normal1;
                        }
                        else
                        {
                            normal = -normal1;
                            lowerLimit = normal1;
                            upperLimit = -normal0;
                        }
                    }
                }
                else if (hasVertex3)
                {
                    if (convex2)
                    {
                        front = offset1 >= 0.0f || offset2 >= 0.0f;
                        if (front)
                        {
                            normal = normal1;
                            lowerLimit = -normal1;
                            upperLimit = normal2;
                        }
                        else
                        {
                            normal = -normal1;
                            lowerLimit = -normal1;
                            upperLimit = normal1;
                        }
                    }
                    else
                    {
                        front = offset1 >= 0.0f && offset2 >= 0.0f;
                        if (front)
                        {
                            normal = normal1;
                            lowerLimit = -normal1;
                            upperLimit = normal1;
                        }
                        else
                        {
                            normal = -normal1;
                            lowerLimit = -normal2;
                            upperLimit = normal1;
                        }
                    }
                }
                else
                {
                    front = offset1 >= 0.0f;
                    if (front)
                    {
                        normal = normal1;
                        lowerLimit = -normal1;
                        upperLimit = -normal1;
                    }
                    else
                    {
                        normal = -normal1;
                        lowerLimit = normal1;
                        upperLimit = normal1;
                    }
                }

                // Get polygonB in frameA
                tempPolygonB.Count = polygonB.Vertices.Count;
                for (int i = 0; i < polygonB.Vertices.Count; ++i)
                {
                    tempPolygonB.Vertices[i] = PhysicsTransform.Multiply(polygonB.Vertices[i], ref xf);
                    tempPolygonB.Normals[i] = Complex.Multiply(polygonB.Normals[i], ref xf.Quaternion);
                }

                radius = 2.0f * PhysicsSettings.PolygonRadius;

                manifold.PointCount = 0;

                EPAxis edgeAxis = ComputeEdgeSeparation(ref tempPolygonB, ref normal, ref v1, front);

                // If no valid normal can be found than this edge should not collide.
                if (edgeAxis.Type == EPAxisType.Unknown)
                {
                    return;
                }

                if (edgeAxis.Separation > radius)
                {
                    return;
                }

                EPAxis polygonAxis = ComputePolygonSeparation(ref tempPolygonB, ref normal, ref v1, ref v2, ref lowerLimit, ref upperLimit, radius);
                if (polygonAxis.Type != EPAxisType.Unknown && polygonAxis.Separation > radius)
                {
                    return;
                }

                // Use hysteresis for jitter reduction.
                const float k_relativeTol = 0.98f;
                const float k_absoluteTol = 0.001f;

                EPAxis primaryAxis;
                if (polygonAxis.Type == EPAxisType.Unknown)
                {
                    primaryAxis = edgeAxis;
                }
                else if (polygonAxis.Separation > k_relativeTol * edgeAxis.Separation + k_absoluteTol)
                {
                    primaryAxis = polygonAxis;
                }
                else
                {
                    primaryAxis = edgeAxis;
                }

                FixedArray2<ClipVertex> ie = new FixedArray2<ClipVertex>();
                ReferenceFace rf;
                if (primaryAxis.Type == EPAxisType.EdgeA)
                {
                    manifold.Type = ManifoldType.FaceA;

                    // Search for the polygon normal that is most anti-parallel to the edge normal.
                    int bestIndex = 0;
                    float bestValue = Vector2.Dot(normal, tempPolygonB.Normals[0]);
                    for (int i = 1; i < tempPolygonB.Count; ++i)
                    {
                        float value = Vector2.Dot(normal, tempPolygonB.Normals[i]);
                        if (value < bestValue)
                        {
                            bestValue = value;
                            bestIndex = i;
                        }
                    }

                    int i1 = bestIndex;
                    int i2 = i1 + 1 < tempPolygonB.Count ? i1 + 1 : 0;

                    ClipVertex c0 = ie[0];
                    c0.V = tempPolygonB.Vertices[i1];
                    c0.ID.Features.IndexA = 0;
                    c0.ID.Features.IndexB = (byte)i1;
                    c0.ID.Features.TypeA = (byte)ContactFeatureType.Face;
                    c0.ID.Features.TypeB = (byte)ContactFeatureType.Vertex;
                    ie[0] = c0;

                    ClipVertex c1 = ie[1];
                    c1.V = tempPolygonB.Vertices[i2];
                    c1.ID.Features.IndexA = 0;
                    c1.ID.Features.IndexB = (byte)i2;
                    c1.ID.Features.TypeA = (byte)ContactFeatureType.Face;
                    c1.ID.Features.TypeB = (byte)ContactFeatureType.Vertex;
                    ie[1] = c1;

                    if (front)
                    {
                        rf.i1 = 0;
                        rf.i2 = 1;
                        rf.v1 = v1;
                        rf.v2 = v2;
                        rf.normal = normal1;
                    }
                    else
                    {
                        rf.i1 = 1;
                        rf.i2 = 0;
                        rf.v1 = v2;
                        rf.v2 = v1;
                        rf.normal = -normal1;
                    }
                }
                else
                {
                    manifold.Type = ManifoldType.FaceB;
                    ClipVertex c0 = ie[0];
                    c0.V = v1;
                    c0.ID.Features.IndexA = 0;
                    c0.ID.Features.IndexB = (byte)primaryAxis.Index;
                    c0.ID.Features.TypeA = (byte)ContactFeatureType.Vertex;
                    c0.ID.Features.TypeB = (byte)ContactFeatureType.Face;
                    ie[0] = c0;

                    ClipVertex c1 = ie[1];
                    c1.V = v2;
                    c1.ID.Features.IndexA = 0;
                    c1.ID.Features.IndexB = (byte)primaryAxis.Index;
                    c1.ID.Features.TypeA = (byte)ContactFeatureType.Vertex;
                    c1.ID.Features.TypeB = (byte)ContactFeatureType.Face;
                    ie[1] = c1;

                    rf.i1 = primaryAxis.Index;
                    rf.i2 = rf.i1 + 1 < tempPolygonB.Count ? rf.i1 + 1 : 0;
                    rf.v1 = tempPolygonB.Vertices[rf.i1];
                    rf.v2 = tempPolygonB.Vertices[rf.i2];
                    rf.normal = tempPolygonB.Normals[rf.i1];
                }

                rf.sideNormal1 = new Vector2(rf.normal.Y, -rf.normal.X);
                rf.sideNormal2 = -rf.sideNormal1;
                rf.sideOffset1 = Vector2.Dot(rf.sideNormal1, rf.v1);
                rf.sideOffset2 = Vector2.Dot(rf.sideNormal2, rf.v2);

                // Clip incident edge against extruded edge1 side edges.
                FixedArray2<ClipVertex> clipPoints1;
                FixedArray2<ClipVertex> clipPoints2;
                int np;

                // Clip to box side 1
                np = ClipSegmentToLine(out clipPoints1, ref ie, rf.sideNormal1, rf.sideOffset1, rf.i1);

                if (np < PhysicsSettings.MaxManifoldPoints)
                {
                    return;
                }

                // Clip to negative box side 1
                np = ClipSegmentToLine(out clipPoints2, ref clipPoints1, rf.sideNormal2, rf.sideOffset2, rf.i2);

                if (np < PhysicsSettings.MaxManifoldPoints)
                {
                    return;
                }

                // Now clipPoints2 contains the clipped points.
                if (primaryAxis.Type == EPAxisType.EdgeA)
                {
                    manifold.LocalNormal = rf.normal;
                    manifold.LocalPoint = rf.v1;
                }
                else
                {
                    manifold.LocalNormal = polygonB.Normals[rf.i1];
                    manifold.LocalPoint = polygonB.Vertices[rf.i1];
                }

                int pointCount = 0;
                for (int i = 0; i < PhysicsSettings.MaxManifoldPoints; ++i)
                {
                    float separation = Vector2.Dot(rf.normal, clipPoints2[i].V - rf.v1);

                    if (separation <= radius)
                    {
                        ManifoldPoint cp = manifold.Points[pointCount];

                        if (primaryAxis.Type == EPAxisType.EdgeA)
                        {
                            PhysicsTransform.Divide(clipPoints2[i].V, ref xf, out cp.LocalPoint);
                            cp.Id = clipPoints2[i].ID;
                        }
                        else
                        {
                            cp.LocalPoint = clipPoints2[i].V;
                            cp.Id.Features.TypeA = clipPoints2[i].ID.Features.TypeB;
                            cp.Id.Features.TypeB = clipPoints2[i].ID.Features.TypeA;
                            cp.Id.Features.IndexA = clipPoints2[i].ID.Features.IndexB;
                            cp.Id.Features.IndexB = clipPoints2[i].ID.Features.IndexA;
                        }

                        manifold.Points[pointCount] = cp;
                        ++pointCount;
                    }
                }

                manifold.PointCount = pointCount;
            }

            private static EPAxis ComputeEdgeSeparation(ref TempPolygon polygonB, ref Vector2 normal, ref Vector2 v1, bool front)
            {
                EPAxis axis;
                axis.Type = EPAxisType.EdgeA;
                axis.Index = front ? 0 : 1;
                axis.Separation = float.MaxValue;

                for (int i = 0; i < polygonB.Count; ++i)
                {
                    float s = Vector2.Dot(normal, polygonB.Vertices[i] - v1);
                    if (s < axis.Separation)
                    {
                        axis.Separation = s;
                    }
                }

                return axis;
            }

            private static EPAxis ComputePolygonSeparation(ref TempPolygon polygonB, ref Vector2 normal, ref Vector2 v1, ref Vector2 v2, ref Vector2 lowerLimit, ref Vector2 upperLimit, float radius)
            {
                EPAxis axis;
                axis.Type = EPAxisType.Unknown;
                axis.Index = -1;
                axis.Separation = float.MinValue;

                Vector2 perp = new Vector2(-normal.Y, normal.X);

                for (int i = 0; i < polygonB.Count; ++i)
                {
                    Vector2 n = -polygonB.Normals[i];

                    float s1 = Vector2.Dot(n, polygonB.Vertices[i] - v1);
                    float s2 = Vector2.Dot(n, polygonB.Vertices[i] - v2);
                    float s = Math.Min(s1, s2);

                    if (s > radius)
                    {
                        // No collision
                        axis.Type = EPAxisType.EdgeB;
                        axis.Index = i;
                        axis.Separation = s;
                        return axis;
                    }

                    // Adjacency
                    if (Vector2.Dot(n, perp) >= 0.0f)
                    {
                        if (Vector2.Dot(n - upperLimit, normal) < -PhysicsSettings.AngularSlop)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (Vector2.Dot(n - lowerLimit, normal) < -PhysicsSettings.AngularSlop)
                        {
                            continue;
                        }
                    }

                    if (s > axis.Separation)
                    {
                        axis.Type = EPAxisType.EdgeB;
                        axis.Index = i;
                        axis.Separation = s;
                    }
                }

                return axis;
            }
        }

        /// <summary>
        /// Clipping for contact manifolds.
        /// </summary>
        /// <param name="vOut">The v out.</param>
        /// <param name="vIn">The v in.</param>
        /// <param name="normal">The normal.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="vertexIndexA">The vertex index A.</param>
        /// <returns></returns>
        private static int ClipSegmentToLine(out FixedArray2<ClipVertex> vOut, ref FixedArray2<ClipVertex> vIn, Vector2 normal, float offset, int vertexIndexA)
        {
            vOut = new FixedArray2<ClipVertex>();

            ClipVertex v0 = vIn[0];
            ClipVertex v1 = vIn[1];

            // Start with no output points
            int numOut = 0;

            // Calculate the distance of end points to the line
            float distance0 = normal.X * v0.V.X + normal.Y * v0.V.Y - offset;
            float distance1 = normal.X * v1.V.X + normal.Y * v1.V.Y - offset;

            // If the points are behind the plane
            if (distance0 <= 0.0f) vOut[numOut++] = v0;
            if (distance1 <= 0.0f) vOut[numOut++] = v1;

            // If the points are on different sides of the plane
            if (distance0 * distance1 < 0.0f)
            {
                // Find intersection point of edge and plane
                float interp = distance0 / (distance0 - distance1);

                ClipVertex cv = vOut[numOut];

                cv.V.X = v0.V.X + interp * (v1.V.X - v0.V.X);
                cv.V.Y = v0.V.Y + interp * (v1.V.Y - v0.V.Y);

                // VertexA is hitting edgeB.
                cv.ID.Features.IndexA = (byte)vertexIndexA;
                cv.ID.Features.IndexB = v0.ID.Features.IndexB;
                cv.ID.Features.TypeA = (byte)ContactFeatureType.Vertex;
                cv.ID.Features.TypeB = (byte)ContactFeatureType.Face;

                vOut[numOut] = cv;

                ++numOut;
            }

            return numOut;
        }

        /// <summary>
        /// Find the separation between poly1 and poly2 for a give edge normal on poly1.
        /// </summary>
        /// <param name="poly1">The poly1.</param>
        /// <param name="xf1">The XF1.</param>
        /// <param name="edge1">The edge1.</param>
        /// <param name="poly2">The poly2.</param>
        /// <param name="xf2">The XF2.</param>
        /// <returns></returns>
        private static float EdgeSeparation(PolygonShape poly1, ref PhysicsTransform xf1To2, int edge1, PolygonShape poly2)
        {
            List<Vector2> vertices1 = poly1.Vertices;
            List<Vector2> normals1 = poly1.Normals;

            int count2 = poly2.Vertices.Count;
            List<Vector2> vertices2 = poly2.Vertices;

            Debug.Assert(0 <= edge1 && edge1 < poly1.Vertices.Count);

            // Convert normal from poly1's frame into poly2's frame.
            Vector2 normal1 = Complex.Multiply(normals1[edge1], ref xf1To2.Quaternion);

            // Find support vertex on poly2 for -normal.
            int index = 0;
            float minDot = float.MaxValue;

            for (int i = 0; i < count2; ++i)
            {
                float dot = Vector2.Dot(vertices2[i], normal1);
                if (dot < minDot)
                {
                    minDot = dot;
                    index = i;
                }
            }

            Vector2 v1 = PhysicsTransform.Multiply(vertices1[edge1], ref xf1To2);
            Vector2 v2 = vertices2[index];
            float separation = Vector2.Dot(v2 - v1, normal1);

            return separation;
        }

        /// <summary>
        /// Find the max separation between poly1 and poly2 using edge normals from poly1.
        /// </summary>
        /// <param name="edgeIndex">Index of the edge.</param>
        /// <param name="poly1">The poly1.</param>
        /// <param name="xf1">The XF1.</param>
        /// <param name="poly2">The poly2.</param>
        /// <param name="xf2">The XF2.</param>
        /// <returns></returns>
        private static float FindMaxSeparation(out int edgeIndex, PolygonShape poly1, ref PhysicsTransform xf1, PolygonShape poly2, ref PhysicsTransform xf2)
        {
            int count1 = poly1.Vertices.Count;
            List<Vector2> normals1 = poly1.Normals;

            var xf1To2 = PhysicsTransform.Divide(ref xf1, ref xf2);

            // Vector pointing from the centroid of poly1 to the centroid of poly2.
            Vector2 c2local = PhysicsTransform.Divide(poly2.MassData.Centroid, ref xf1To2);
            Vector2 dLocal1 = c2local - poly1.MassData.Centroid;

            // Find edge normal on poly1 that has the largest projection onto d.
            int edge = 0;
            float maxDot = float.MinValue;
            for (int i = 0; i < count1; ++i)
            {
                float dot = Vector2.Dot(normals1[i], dLocal1);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    edge = i;
                }
            }

            // Get the separation for the edge normal.
            float s = EdgeSeparation(poly1, ref xf1To2, edge, poly2);

            // Check the separation for the previous edge normal.
            int prevEdge = edge - 1 >= 0 ? edge - 1 : count1 - 1;
            float sPrev = EdgeSeparation(poly1, ref xf1To2, prevEdge, poly2);

            // Check the separation for the next edge normal.
            int nextEdge = edge + 1 < count1 ? edge + 1 : 0;
            float sNext = EdgeSeparation(poly1, ref xf1To2, nextEdge, poly2);

            // Find the best edge and the search direction.
            int bestEdge;
            float bestSeparation;
            int increment;
            if (sPrev > s && sPrev > sNext)
            {
                increment = -1;
                bestEdge = prevEdge;
                bestSeparation = sPrev;
            }
            else if (sNext > s)
            {
                increment = 1;
                bestEdge = nextEdge;
                bestSeparation = sNext;
            }
            else
            {
                edgeIndex = edge;
                return s;
            }

            // Perform a local search for the best edge normal.
            for (; ; )
            {
                if (increment == -1)
                    edge = bestEdge - 1 >= 0 ? bestEdge - 1 : count1 - 1;
                else
                    edge = bestEdge + 1 < count1 ? bestEdge + 1 : 0;

                s = EdgeSeparation(poly1, ref xf1To2, edge, poly2);

                if (s > bestSeparation)
                {
                    bestEdge = edge;
                    bestSeparation = s;
                }
                else
                {
                    break;
                }
            }

            edgeIndex = bestEdge;
            return bestSeparation;
        }

        private static void FindIncidentEdge(out FixedArray2<ClipVertex> c, PolygonShape poly1, ref PhysicsTransform xf1, int edge1, PolygonShape poly2, ref PhysicsTransform xf2)
        {
            c = new FixedArray2<ClipVertex>();
            Vertices normals1 = poly1.Normals;

            int count2 = poly2.Vertices.Count;
            Vertices vertices2 = poly2.Vertices;
            Vertices normals2 = poly2.Normals;

            DebugTools.Assert(0 <= edge1 && edge1 < poly1.Vertices.Count);

            // Get the normal of the reference edge in poly2's frame.
            Vector2 normal1 = Complex.Divide(Complex.Multiply(normals1[edge1], ref xf1.Quaternion), ref xf2.Quaternion);


            // Find the incident edge on poly2.
            int index = 0;
            float minDot = float.MaxValue;
            for (int i = 0; i < count2; ++i)
            {
                float dot = Vector2.Dot(normal1, normals2[i]);
                if (dot < minDot)
                {
                    minDot = dot;
                    index = i;
                }
            }

            // Build the clip vertices for the incident edge.
            int i1 = index;
            int i2 = i1 + 1 < count2 ? i1 + 1 : 0;

            ClipVertex cv0 = c[0];

            cv0.V = PhysicsTransform.Multiply(vertices2[i1], ref xf2);
            cv0.ID.Features.IndexA = (byte)edge1;
            cv0.ID.Features.IndexB = (byte)i1;
            cv0.ID.Features.TypeA = (byte)ContactFeatureType.Face;
            cv0.ID.Features.TypeB = (byte)ContactFeatureType.Vertex;

            c[0] = cv0;

            ClipVertex cv1 = c[1];
            cv1.V = PhysicsTransform.Multiply(vertices2[i2], ref xf2);
            cv1.ID.Features.IndexA = (byte)edge1;
            cv1.ID.Features.IndexB = (byte)i2;
            cv1.ID.Features.TypeA = (byte)ContactFeatureType.Face;
            cv1.ID.Features.TypeB = (byte)ContactFeatureType.Vertex;

            c[1] = cv1;
        }
    }
}
