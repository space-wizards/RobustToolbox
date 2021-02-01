using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics.Shapes
{
    [Serializable, NetSerializable]
    public class PolygonShape : IPhysShape
    {
        public List<Vector2> Vertices
        {
            get => _vertices;
            set
            {
                _vertices = value;

                var configManager = IoCManager.Resolve<IConfigurationManager>();
                DebugTools.Assert(_vertices.Count >= 3 && _vertices.Count <= configManager.GetCVar(CVars.MaxPolygonVertices));

                /* TODO:
                if (configManager.GetCVar(CVars.UseConvexHullPolygons))
                {
                    //FPE note: This check is required as the GiftWrap algorithm early exits on triangles
                    //So instead of giftwrapping a triangle, we just force it to be clock wise.
                    if (_vertices.Count <= 3)
                        _vertices.ForceCounterClockWise();
                    else
                        _vertices = GiftWrap.GetConvexHull(_vertices);
                }
                */

                _normals = new List<Vector2>(_vertices.Count);

                // Compute normals. Ensure the edges have non-zero length.
                for (int i = 0; i < _vertices.Count; ++i)
                {
                    int next = i + 1 < _vertices.Count ? i + 1 : 0;
                    Vector2 edge = _vertices[next] - _vertices[i];
                    DebugTools.Assert(edge.LengthSquared > float.Epsilon * float.Epsilon);

                    //FPE optimization: Normals.Add(MathHelper.Cross(edge, 1.0f));
                    Vector2 temp = new Vector2(edge.Y, -edge.X);
                    _normals.Add(temp.Normalized);
                }

                // Compute the polygon mass data
                // ComputeProperties();
            }
        }

        private List<Vector2> _vertices = new();

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
            }
        }

        private float _radius;

        public ShapeType ShapeType => ShapeType.Polygon;

        // You did dis remmiiieeeee
        // https://discord.com/channels/310555209753690112/560845886263918612/804917295456845835
        // I might fix it later
        public PolygonShape(IPhysShape shape)
        {
            switch (shape)
            {
                case PhysShapeAabb aabb:
                    Vertices = new List<Vector2>
                    {
                        aabb.LocalBounds.BottomLeft,
                        aabb.LocalBounds.BottomRight,
                        aabb.LocalBounds.TopRight,
                        aabb.LocalBounds.TopLeft,
                    };
                    break;
                case PhysShapeGrid grid:
                    Vertices = new List<Vector2>
                    {
                        grid.LocalBounds.BottomLeft,
                        grid.LocalBounds.BottomRight,
                        grid.LocalBounds.TopRight,
                        grid.LocalBounds.TopLeft,
                    };
                    break;
                case PhysShapeRect rect:
                    Vertices = new List<Vector2>
                    {
                        rect.Rectangle.BottomLeft,
                        rect.Rectangle.BottomRight,
                        rect.Rectangle.TopRight,
                        rect.Rectangle.TopLeft,
                    };
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Vertices, "vertices", new List<Vector2>());
            _radius = IoCManager.Resolve<IConfigurationManager>().GetCVar(CVars.PolygonRadius);
            // ComputeProperties();
        }

        public bool Equals(IPhysShape? other)
        {
            // TODO: Could use casts for AABB and Rect
            if (other is not PolygonShape poly) return false;
            if (_vertices.Count != poly.Vertices.Count) return false;
            for (var i = 0; i < _vertices.Count; i++)
            {
                var vert = _vertices[i];
                if (!vert.EqualsApprox(poly.Vertices[i])) return false;
            }

            return true;
        }

        public Box2 CalculateLocalBounds(Angle rotation)
        {
            if (Vertices.Count == 0) return new Box2();

            var aabb = new Box2();
            Vector2 lower = Vertices[0];
            Vector2 upper = lower;

            for (int i = 1; i < Vertices.Count; ++i)
            {
                Vector2 v = Vertices[i];
                lower = Vector2.ComponentMin(lower, v);
                upper = Vector2.ComponentMax(upper, v);
            }

            Vector2 r = new Vector2(Radius, Radius);
            aabb.BottomLeft = lower - r;
            aabb.TopRight = upper + r;

            return aabb;
        }

        public void ApplyState()
        {
            return;
        }

        public void DebugDraw(DebugDrawingHandle handle, in Matrix3 modelMatrix, in Box2 worldViewport, float sleepPercent)
        {
            // TODO HARD
            return;
        }

        public static explicit operator PolygonShape(PhysShapeAabb aabb)
        {
            return new(aabb);
        }
    }
}
