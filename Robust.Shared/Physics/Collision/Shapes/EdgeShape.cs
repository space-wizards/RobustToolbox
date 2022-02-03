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
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Collision.Shapes
{
    [Serializable, NetSerializable]
    public sealed class EdgeShape : IPhysShape
    {
        internal Vector2 Centroid { get; set; } = Vector2.Zero;

        // Note that the normal is from Vertex 2 to Vertex 1 CCW

        /// <summary>
        ///     Edge start vertex
        /// </summary>
        internal Vector2 Vertex1;

        /// <summary>
        ///     Edge end vertex
        /// </summary>
        internal Vector2 Vertex2;

        // Optional adjacent vertices for smooth collision.

        internal Vector2 Vertex0;

        internal Vector2 Vertex3;

        public bool OneSided;

        public int ChildCount => 1;

        public ShapeType ShapeType => ShapeType.Edge;

        public float Radius
        {
            get => _radius;
            set => _radius = PhysicsConstants.PolygonRadius;
        }

        private float _radius = PhysicsConstants.PolygonRadius;

        /// <summary>
        ///     Create a 1-sided edge.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        public EdgeShape(Vector2 v1, Vector2 v2)
        {
            SetTwoSided(v1, v2);
        }

        public void SetOneSided(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            Vertex0 = v0;
            Vertex1 = v1;
            Vertex2 = v2;
            Vertex3 = v3;
            OneSided = true;
        }

        /// <summary>
        /// Set this as an isolated edge.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        public void SetTwoSided(Vector2 start, Vector2 end)
        {
            Vertex1 = start;
            Vertex2 = end;
            OneSided = true;
        }

        public bool Equals(IPhysShape? other)
        {
            if (other is not EdgeShape edge) return false;
            return OneSided == edge.OneSided &&
                   Vertex0.Equals(edge.Vertex0) &&
                   Vertex1.Equals(edge.Vertex1) &&
                   Vertex2.Equals(edge.Vertex2) &&
                   Vertex3.Equals(edge.Vertex3);
        }

        public Box2 ComputeAABB(Transform transform, int childIndex)
        {
            DebugTools.Assert(childIndex == 0);

            var v1 = Transform.Mul(transform, Vertex1);
            var v2 = Transform.Mul(transform, Vertex2);

            var lower = Vector2.ComponentMin(v1, v2);
            var upper = Vector2.ComponentMax(v1, v2);

            var radius = new Vector2(PhysicsConstants.PolygonRadius, PhysicsConstants.PolygonRadius);
            return new Box2(lower - radius, upper + radius);
        }

        public float CalculateArea()
        {
            // It's a line
            return 0f;
        }
    }
}
