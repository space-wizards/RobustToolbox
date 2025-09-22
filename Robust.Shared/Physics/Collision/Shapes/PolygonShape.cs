/*
* Farseer Physics Engine:
* Copyright (c) 2012 Ian Qvist
*
* Original source Box2D:
* Copyright (c) 2006-2011 Erin Catto http://www.box2d.org
*
* This software is provided 'as-is', without any express or implied
* warranty.  In no event will the authors be held liable for any damages
* arising from the use of this software.
* Permission is granted to anyone to use this software for any purpose,
* including commercial applications, and to alter it and redistribute it
* freely, subject to the following restrictions:
* 1. The origin of this software must not be misrepresented; you must not
* claim that you wrote the original software. If you use this software
* in a product, an acknowledgment in the product documentation would be
* appreciated but is not required.
* 2. Altered source versions must be plainly marked as such, and must not be
* misrepresented as being the original software.
* 3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Collision.Shapes
{
    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed partial class PolygonShape : IPhysShape, ISerializationHooks, IEquatable<PolygonShape>, IApproxEquatable<PolygonShape>
    {
        // TODO: Serialize this someday. This probably needs a dedicated shapeserializer that derives vertexcount
        // from the yml nodes just for convenience.
        [ViewVariables]
        public int VertexCount => Vertices.Length;

        [DataField("vertices"),
         Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute,
             Other = AccessPermissions.Read)]
        public Vector2[] Vertices = Array.Empty<Vector2>();

        [ViewVariables,
         Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute,
             Other = AccessPermissions.Read)]
        public Vector2[] Normals = Array.Empty<Vector2>();

        [ViewVariables, Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
        public Vector2 Centroid { get; internal set; } = Vector2.Zero;

        public int ChildCount => 1;

        /// <summary>
        /// The radius of this polygon.
        /// </summary>
        [DataField, Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
        public float Radius { get; set; } = PhysicsConstants.PolygonRadius;

        public bool Set(List<Vector2> vertices)
        {
            var verts = CollectionsMarshal.AsSpan(vertices);
            return Set(verts, vertices.Count);
        }

        public bool Set(ReadOnlySpan<Vector2> vertices, int count)
        {
            DebugTools.Assert(count is >= 3 and <= PhysicsConstants.MaxPolygonVertices);

            var hull = InternalPhysicsHull.ComputeHull(vertices, count);

            if (hull.Count < 3)
            {
                return false;
            }

            Set(hull);
            return true;
        }

        internal void Set(InternalPhysicsHull hull)
        {
            DebugTools.Assert(hull.Count >= 3);
            var vertexCount = hull.Count;
            Array.Resize(ref Vertices, vertexCount);
            Array.Resize(ref Normals, vertexCount);

            for (var i = 0; i < vertexCount; i++)
            {
                Vertices[i] = hull.Points[i];
            }

            // Compute normals. Ensure the edges have non-zero length.
            for (var i = 0; i < vertexCount; i++)
            {
                var next = i + 1 < vertexCount ? i + 1 : 0;
                var edge = Vertices[next] - Vertices[i];
                DebugTools.Assert(edge.LengthSquared() > float.Epsilon * float.Epsilon);

                var temp = Vector2Helpers.Cross(edge, 1f);
                Normals[i] = temp.Normalized();
            }

            Centroid = ComputeCentroid(Vertices, VertexCount);
        }

        public bool Validate()
        {
            var count = VertexCount;
            if (count is < 3 or > PhysicsConstants.MaxPolygonVertices)
                return false;

            var hull = new InternalPhysicsHull();
            for (var i = 0; i < count; i++)
            {
                hull.Points[i] = Vertices[i];
            }

            hull.Count = count;
            return InternalPhysicsHull.ValidateHull(hull);
        }

        private static Vector2 ComputeCentroid(Vector2[] vs, int count)
        {
            DebugTools.Assert(count >= 3);

            var c = new Vector2(0.0f, 0.0f);
            float area = 0.0f;

            // Get a reference point for forming triangles.
            // Use the first vertex to reduce round-off errors.
            var s = vs[0];

            const float inv3 = 1.0f / 3.0f;

            for (var i = 0; i < count; ++i)
            {
                // Triangle vertices.
                var p1 = vs[0] - s;
                var p2 = vs[i] - s;
                var p3 = i + 1 < count ? vs[i+1] - s : vs[0] - s;

                var e1 = p2 - p1;
                var e2 = p3 - p1;

                float D = Vector2Helpers.Cross(e1, e2);

                float triangleArea = 0.5f * D;
                area += triangleArea;

                // Area weighted centroid
                c += (p1 + p2 + p3) * triangleArea * inv3;
            }

            // Centroid
            DebugTools.Assert(area > float.Epsilon);
            c = c * (1.0f / area) + s;
            return c;
        }

        public ShapeType ShapeType => ShapeType.Polygon;

        public PolygonShape()
        {
        }

        internal PolygonShape(SlimPolygon poly)
        {
            Vertices = new Vector2[poly.VertexCount];
            Normals = new Vector2[poly.VertexCount];

            poly._vertices.AsSpan[..VertexCount].CopyTo(Vertices);
            poly._normals.AsSpan[..VertexCount].CopyTo(Normals);

            Centroid = poly.Centroid;
        }

        internal PolygonShape(Polygon poly)
        {
            Vertices = new Vector2[poly.VertexCount];
            Normals = new Vector2[poly.VertexCount];

            poly._vertices.AsSpan[..VertexCount].CopyTo(Vertices);
            poly._normals.AsSpan[..VertexCount].CopyTo(Normals);

            Centroid = poly.Centroid;
        }

        public PolygonShape(float radius)
        {
            Radius = radius;
        }

        void ISerializationHooks.AfterDeserialization()
        {
            // TODO: Someday don't need this.
            Set(Vertices.AsSpan(), VertexCount);
        }

        public void Set(Box2Rotated bounds)
        {
            Span<Vector2> verts = stackalloc Vector2[4];
            verts[0] = bounds.BottomLeft;
            verts[1] = bounds.BottomRight;
            verts[2] = bounds.TopRight;
            verts[3] = bounds.TopLeft;

            var hull = new InternalPhysicsHull(verts, 4);
            Set(hull);
        }

        public void SetAsBox(Box2 box)
        {
            Array.Resize(ref Vertices, 4);
            Array.Resize(ref Normals, 4);

            Vertices[0] = box.BottomLeft;
            Vertices[1] = box.BottomRight;
            Vertices[2] = box.TopRight;
            Vertices[3] = box.TopLeft;

            Normals[0] = new Vector2(0.0f, -1.0f);
            Normals[1] = new Vector2(1.0f, 0.0f);
            Normals[2] = new Vector2(0.0f, 1.0f);
            Normals[3] = new Vector2(-1.0f, 0.0f);

            Centroid = box.Center;
        }

        public void SetAsBox(float halfWidth, float halfHeight)
        {
            Array.Resize(ref Vertices, 4);
            Array.Resize(ref Normals, 4);

            Vertices[0] = new Vector2(-halfWidth, -halfHeight);
            Vertices[1] = new Vector2(halfWidth, -halfHeight);
            Vertices[2] = new Vector2(halfWidth,  halfHeight);
            Vertices[3] = new Vector2(-halfWidth,  halfHeight);

            Normals[0] = new Vector2(0.0f, -1.0f);
            Normals[1] = new Vector2(1.0f, 0.0f);
            Normals[2] = new Vector2(0.0f, 1.0f);
            Normals[3] = new Vector2(-1.0f, 0.0f);

            Centroid = Vector2.Zero;
        }

        public void SetAsBox(float halfWidth, float halfHeight, Vector2 center, float angle)
        {
            Array.Resize(ref Vertices, 4);
            Array.Resize(ref Normals, 4);

            Vertices[0] = new Vector2(-halfWidth, -halfHeight);
            Vertices[1] = new Vector2(halfWidth, -halfHeight);
            Vertices[2] = new Vector2(halfWidth, halfHeight);
            Vertices[3] = new Vector2(-halfWidth, halfHeight);

            Normals[0] = new Vector2(0f, -1f);
            Normals[1] = new Vector2(1f, 0f);
            Normals[2] = new Vector2(0f, 1f);
            Normals[3] = new Vector2(-1f, 0f);

            Centroid = center;

            var xf = new Transform(center, angle);

            // Transform vertices and normals.
            for (var i = 0; i < VertexCount; ++i)
            {
                Vertices[i] = Transform.Mul(xf, Vertices[i]);
                Normals[i] = Transform.Mul(xf.Quaternion2D, Normals[i]);
            }
        }

        // Don't need to check Centroid for these below as it's based off of the vertices below
        // (unless you wanted a potentially faster check up front?)
        public bool Equals(IPhysShape? other)
        {
            if (other is not PolygonShape poly) return false;
            if (VertexCount != poly.VertexCount) return false;
            for (var i = 0; i < VertexCount; i++)
            {
                var vert = Vertices[i];
                if (!vert.Equals(poly.Vertices[i])) return false;
            }

            return true;
        }

        public bool EqualsApprox(PolygonShape other)
        {
            return EqualsApprox(other, 0.001);
        }

        public bool EqualsApprox(PolygonShape other, double tolerance)
        {
            if (VertexCount != other.VertexCount || !MathHelper.CloseTo(Radius, other.Radius, tolerance)) return false;

            for (var i = 0; i < VertexCount; i++)
            {
                if (!Vertices[i].EqualsApprox(other.Vertices[i], tolerance)) return false;
            }

            return true;
        }

        public Box2 ComputeAABB(Transform transform, int childIndex)
        {
            DebugTools.Assert(childIndex == 0);
            var lower = Transform.Mul(transform, Vertices[0]);
            var upper = lower;

            for (var i = 1; i < VertexCount; ++i)
            {
                var v = Transform.Mul(transform, Vertices[i]);
                lower = Vector2.Min(lower, v);
                upper = Vector2.Max(upper, v);
            }

            var r = new Vector2(Radius, Radius);
            return new Box2(lower - r, upper + r);
        }

        public static explicit operator PolygonShape(PhysShapeAabb aabb)
        {
            // TODO: Need a test for this probably, if there is no AABB manifold generator done at least.
            var bounds = aabb.LocalBounds;

            // Don't use setter as we already know the winding.
            return new PolygonShape(aabb.Radius)
            {
                Vertices = new []
                {
                    bounds.BottomLeft,
                    bounds.BottomRight,
                    bounds.TopRight,
                    bounds.TopLeft,
                },
                Normals = new []
                {
                    new Vector2(0f, -1f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(-1f, 0f),
                },
            };
        }

        public bool Equals(PolygonShape? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            if (!Radius.Equals(other.Radius) || VertexCount != other.VertexCount)
                return false;

            for (var i = 0; i < VertexCount; i++)
            {
                var vert = Vertices[i];
                var otherVert = other.Vertices[i];

                if (!vert.Equals(otherVert))
                    return false;
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is PolygonShape other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VertexCount, Vertices.AsSpan(0, VertexCount).ToArray(), Radius);
        }
    }
}
