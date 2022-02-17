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
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Robust.Shared.Physics.Collision.Shapes
{
    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed class PolygonShape : IPhysShape, ISerializationHooks, IApproxEquatable<PolygonShape>
    {
        [ViewVariables]
        public int VertexCount => Vertices.Length;

        /// <summary>
        /// This is public so engine code can manipulate it directly.
        /// NOTE! If you wish to manipulate this then you need to update the normals and centroid yourself!
        /// </summary>
        [ViewVariables]
        [DataField("vertices")]
        public Vector2[] Vertices = Array.Empty<Vector2>();

        [ViewVariables]
        public Vector2[] Normals = Array.Empty<Vector2>();

        [ViewVariables]
        internal Vector2 Centroid { get; set; } = Vector2.Zero;

        public int ChildCount => 1;

        /// <summary>
        /// The radius of this polygon.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Radius
        {
            get => _radius;
            set
            {
                if (MathHelper.CloseToPercent(_radius, value)) return;
                _radius = value;
                // TODO: Update
            }
        }

        private float _radius;

        public void SetVertices(List<Vector2> vertices)
        {
            Span<Vector2> verts = stackalloc Vector2[vertices.Count];

            for (var i = 0; i < vertices.Count; i++)
            {
                verts[i] = vertices[i];
            }

            SetVertices(verts);
        }

        public void SetVertices(Span<Vector2> vertices)
        {
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            DebugTools.Assert(vertices.Length >= 3 && vertices.Length <= configManager.GetCVar(CVars.MaxPolygonVertices));

            var vertexCount = vertices.Length;

            if (configManager.GetCVar(CVars.ConvexHullPolygons))
            {
                //FPE note: This check is required as the GiftWrap algorithm early exits on triangles
                //So instead of giftwrapping a triangle, we just force it to be clock wise.
                if (vertexCount <= 3)
                    Vertices = Physics.Vertices.ForceCounterClockwise(vertices);
                else
                    Vertices = GiftWrap.SetConvexHull(vertices);
            }
            else
            {
                Array.Resize(ref Vertices, vertexCount);

                for (var i = 0; i < vertices.Length; i++)
                {
                    Vertices[i] = vertices[i];
                }
            }

            // Convex hull may prune some vertices hence the count may change by this point.
            vertexCount = Vertices.Length;

            Array.Resize(ref Normals, vertexCount);

            // Compute normals. Ensure the edges have non-zero length.
            for (var i = 0; i < vertexCount; i++)
            {
                var next = i + 1 < vertexCount ? i + 1 : 0;
                var edge = Vertices[next] - Vertices[i];
                DebugTools.Assert(edge.LengthSquared > float.Epsilon * float.Epsilon);

                //FPE optimization: Normals.Add(MathHelper.Cross(edge, 1.0f));
                var temp = new Vector2(edge.Y, -edge.X);
                Normals[i] = temp.Normalized;
            }

            // TODO: Updates (network etc)
        }

        public ShapeType ShapeType => ShapeType.Polygon;

        public PolygonShape()
        {
            _radius = PhysicsConstants.PolygonRadius;
        }

        public PolygonShape(float radius)
        {
            _radius = radius;
        }

        void ISerializationHooks.AfterDeserialization()
        {
            SetVertices(Vertices);

            DebugTools.Assert(Physics.Vertices.IsCounterClockwise(Vertices.AsSpan()));
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
            Span<Vector2> verts = stackalloc Vector2[4];
            // Damn normies
            Span<Vector2> norms = stackalloc Vector2[4];

            verts[0] = new Vector2(-halfWidth, -halfHeight);
            verts[1] = new Vector2(halfWidth, -halfHeight);
            verts[2] = new Vector2(halfWidth, halfHeight);
            verts[3] = new Vector2(-halfWidth, halfHeight);
            norms[0] = new Vector2(0f, -1f);
            norms[1] = new Vector2(1f, 0f);
            norms[2] = new Vector2(0f, 1f);
            norms[3] = new Vector2(-1f, 0f);

            Centroid = center;

            var xf = new Transform(center, angle);
            Array.Resize(ref Vertices, 4);
            Array.Resize(ref Normals, 4);

            // Transform vertices and normals.
            for (var i = 0; i < verts.Length; ++i)
            {
                Vertices[i] = Transform.Mul(xf, verts[i]);
                Normals[i] = Transform.Mul(xf.Quaternion2D, norms[i]);
            }
        }

        // Don't need to check Centroid for these below as it's based off of the vertices below
        // (unless you wanted a potentially faster check up front?)
        public bool Equals(IPhysShape? other)
        {
            if (other is not PolygonShape poly) return false;
            if (Vertices.Length != poly.Vertices.Length) return false;
            for (var i = 0; i < Vertices.Length; i++)
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
            if (Vertices.Length != other.Vertices.Length) return false;
            for (var i = 0; i < Vertices.Length; i++)
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

            for (var i = 1; i < Vertices.Length; ++i)
            {
                var v = Transform.Mul(transform, Vertices[i]);
                lower = Vector2.ComponentMin(lower, v);
                upper = Vector2.ComponentMax(upper, v);
            }

            var r = new Vector2(_radius, _radius);
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
    }
}
