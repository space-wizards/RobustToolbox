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

namespace Robust.Shared.Physics.Collision.Shapes
{
    [Serializable, NetSerializable]
    [DataDefinition]
    public class PolygonShape : IPhysShape, ISerializationHooks
    {
        /// <summary>
        ///     Counter-clockwise (CCW) order.
        /// </summary>
        [ViewVariables]
        [DataField("vertices")]
        public List<Vector2> Vertices
        {
            get => _vertices;
            set
            {
                _vertices = value;

                var configManager = IoCManager.Resolve<IConfigurationManager>();
                DebugTools.Assert(_vertices.Count >= 3 && _vertices.Count <= configManager.GetCVar(CVars.MaxPolygonVertices));

                if (configManager.GetCVar(CVars.ConvexHullPolygons))
                {
                    //FPE note: This check is required as the GiftWrap algorithm early exits on triangles
                    //So instead of giftwrapping a triangle, we just force it to be clock wise.
                    if (_vertices.Count <= 3)
                        _vertices.ForceCounterClockwise();
                    else
                        _vertices = GiftWrap.GetConvexHull(_vertices);
                }

                _normals = new List<Vector2>(_vertices.Count);

                // Compute normals. Ensure the edges have non-zero length.
                for (var i = 0; i < _vertices.Count; ++i)
                {
                    var next = i + 1 < _vertices.Count ? i + 1 : 0;
                    var edge = _vertices[next] - _vertices[i];
                    DebugTools.Assert(edge.LengthSquared > float.Epsilon * float.Epsilon);

                    //FPE optimization: Normals.Add(MathHelper.Cross(edge, 1.0f));
                    var temp = new Vector2(edge.Y, -edge.X);
                    _normals.Add(temp.Normalized);
                }

                Centroid = ComputeCentroid(_vertices);

                // Compute the polygon mass data
                // TODO: Update fixture. Maybe use events for it? Who tf knows.
                // If we get grid polys then we'll actually need runtime updating of bbs.
            }
        }

        private List<Vector2> _vertices = new();

        internal Vector2 Centroid { get; set; } = Vector2.Zero;

        [ViewVariables(VVAccess.ReadOnly)]
        public List<Vector2> Normals => _normals;

        private List<Vector2> _normals = new();

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
                if (MathHelper.CloseTo(_radius, value)) return;
                _radius = value;
                // TODO: Update
            }
        }

        private float _radius;

        public static Vector2 ComputeCentroid(List<Vector2> vertices)
        {
            DebugTools.Assert(vertices.Count >= 3);

            var c = new Vector2(0.0f, 0.0f);
            float area = 0.0f;

            // Get a reference point for forming triangles.
            // Use the first vertex to reduce round-off errors.
            var s = vertices[0];

            const float inv3 = 1.0f / 3.0f;

            for (var i = 0; i < vertices.Count; ++i)
            {
                // Triangle vertices.
                var p1 = vertices[0] - s;
                var p2 = vertices[i] - s;
                var p3 = i + 1 < vertices.Count ? vertices[i+1] - s : vertices[0] - s;

                var e1 = p2 - p1;
                var e2 = p3 - p1;

                float D = Vector2.Cross(e1, e2);

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
            _radius = IoCManager.Resolve<IConfigurationManager>().GetCVar(CVars.PolygonRadius);
        }

        public PolygonShape(float radius)
        {
            _radius = radius;
        }

        public void SetAsBox(float width, float height)
        {
            // TODO: Just have this set normals directly; look at Box2D to see how it does
            Vertices = new List<Vector2>()
            {
                new(-width, -height),
                new(width, -height),
                new(width, height),
                new(-width, height),
            };
        }

        public bool Equals(IPhysShape? other)
        {
            if (other is not PolygonShape poly) return false;
            if (_vertices.Count != poly.Vertices.Count) return false;
            for (var i = 0; i < _vertices.Count; i++)
            {
                var vert = _vertices[i];
                if (!vert.EqualsApprox(poly.Vertices[i])) return false;
            }

            return true;
        }

        public Box2 ComputeAABB(Transform transform, int childIndex)
        {
            DebugTools.Assert(childIndex == 0);
            var lower = Transform.Mul(transform, _vertices[0]);
            var upper = lower;

            for (var i = 1; i < _vertices.Count; ++i)
            {
                var v = Transform.Mul(transform, _vertices[i]);
                lower = Vector2.ComponentMin(lower, v);
                upper = Vector2.ComponentMax(upper, v);
            }

            var r = new Vector2(_radius, _radius);
            return new Box2(lower - r, upper + r);
        }

        public void ApplyState()
        {
            return;
        }

        public void DebugDraw(DebugDrawingHandle handle, in Matrix3 modelMatrix, in Box2 worldViewport, float sleepPercent)
        {
            handle.SetTransform(modelMatrix);
            handle.DrawPolygonShape(_vertices.ToArray(), handle.CalcWakeColor(handle.RectFillColor, sleepPercent));
        }

        public static explicit operator PolygonShape(PhysShapeAabb aabb)
        {
            // TODO: Need a test for this probably, if there is no AABB manifold generator done at least.
            var bounds = aabb.LocalBounds;

            // Don't use Vertices property given we can just unwind it ourselves faster.
            // Ideal world we don't need this but for now.
            return new PolygonShape(aabb.Radius)
            {
                // Giftwrap seems to use bottom-right first.
                Vertices = new List<Vector2>
                {
                    bounds.BottomRight,
                    bounds.TopRight,
                    bounds.TopLeft,
                    bounds.BottomLeft,
                },

                /*
                _normals = new List<Vector2>
                {
                    new(1, -0),
                    new (0, 1),
                    new (-1, -0),
                    new (0, -1),
                }
                */
            };
        }

        public static explicit operator PolygonShape(PhysShapeRect rect)
        {
            // Ideal world we don't even need PhysShapeRect?
            var bounds = rect.CachedBounds;

            return new PolygonShape(rect.Radius)
            {
                Vertices = new List<Vector2>
                {
                    bounds.BottomRight,
                    bounds.TopRight,
                    bounds.TopLeft,
                    bounds.BottomLeft,
                },
            };
        }
    }
}
