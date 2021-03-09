// MIT License

// Copyright (c) 2019 Erin Catto

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Physics.Dynamics.Shapes
{
    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed class EdgeShape : IPhysShape
    {
        /// <summary>
        ///     Edge start vertex
        /// </summary>
        internal Vector2 Vertex1;

        /// <summary>
        ///     Edge end vertex
        /// </summary>
        internal Vector2 Vertex2;

        /// <summary>
        ///     Is true if the edge is connected to an adjacent vertex before vertex 1.
        /// </summary>
        public bool HasVertex0 { get; set; }

        /// <summary>
        ///     Is true if the edge is connected to an adjacent vertex after vertex2.
        /// </summary>
        public bool HasVertex3 { get; set; }

        /// <summary>
        ///     Optional adjacent vertices. These are used for smooth collision.
        /// </summary>
        public Vector2 Vertex0 { get; set; }

        /// <summary>
        ///     Optional adjacent vertices. These are used for smooth collision.
        /// </summary>
        public Vector2 Vertex3 { get; set; }

        public int ChildCount => 1;

        public bool OneSided => !(HasVertex0 && HasVertex3);

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

        public ShapeType ShapeType => ShapeType.Edge;

        /// <summary>
        ///     Create a 1-sided edge.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public EdgeShape(Vector2 start, Vector2 end)
        {
            Set(start, end);
            _radius = IoCManager.Resolve<IConfigurationManager>().GetCVar(CVars.PolygonRadius);
        }

        /// <summary>
        /// Set this as an isolated edge.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        public void Set(Vector2 start, Vector2 end)
        {
            Vertex1 = start;
            Vertex2 = end;
            HasVertex0 = false;
            HasVertex3 = false;

            // TODO ComputeProperties();
        }

        public bool Equals(IPhysShape? other)
        {
            if (other is not EdgeShape edge) return false;
            return (HasVertex0 == edge.HasVertex0 &&
                    HasVertex3 == edge.HasVertex3 &&
                    Vertex0 == edge.Vertex0 &&
                    Vertex1 == edge.Vertex1 &&
                    Vertex2 == edge.Vertex2 &&
                    Vertex3 == edge.Vertex3);
        }

        public Box2 CalculateLocalBounds(Angle rotation)
        {
            Vector2 lower = Vector2.ComponentMin(Vertex1, Vertex2);
            Vector2 upper = Vector2.ComponentMax(Vertex1, Vertex2);

            Vector2 r = new Vector2(Radius, Radius);
            var aabb = new Box2
            {
                BottomLeft = lower - r,
                TopRight = upper + r
            };
            return aabb;
        }

        public void ApplyState()
        {
            return;
        }

        public void DebugDraw(DebugDrawingHandle handle, in Matrix3 modelMatrix, in Box2 worldViewport, float sleepPercent)
        {
            var m = Matrix3.Identity;
            m.R0C2 = modelMatrix.R0C2;
            m.R1C2 = modelMatrix.R1C2;
            handle.SetTransform(m);
            handle.DrawLine(Vertex1, Vertex2, handle.CalcWakeColor(handle.RectFillColor, sleepPercent));
            handle.SetTransform(Matrix3.Identity);
        }
    }
}
