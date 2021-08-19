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
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics
{
    public interface IFixture
    {
        // TODO
    }

    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed class Fixture : IFixture, IEquatable<Fixture>, ISerializationHooks
    {
        /// <summary>
        /// Allows us to reference a specific fixture when we contain multiple
        /// This is useful for stuff like slippery objects that might have a non-hard layer for mob collisions and
        /// a hard layer for wall collisions.
        /// <remarks>
        /// We can also use this for networking to make cross-referencing fixtures easier.
        /// Won't call Dirty() by default
        /// </remarks>
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("id")]
        public string ID { get; set; } = string.Empty;

        [field: NonSerialized]
        public FixtureProxy[] Proxies { get; set; } = Array.Empty<FixtureProxy>();

        [ViewVariables]
        [NonSerialized]
        public int ProxyCount = 0;

        [ViewVariables]
        [DataField("shape")]
        public IPhysShape Shape { get; private set; } = new PhysShapeAabb();

        [ViewVariables]
        [field:NonSerialized]
        public PhysicsComponent Body { get; internal set; } = default!;

        /// <summary>
        /// Contact friction between 2 bodies. Not tile-friction for top-down.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Friction
        {
            get => _friction;
            set
            {
                if (MathHelper.CloseTo(value, _friction)) return;

                _friction = value;
                Body.FixtureChanged(this);
            }
        }

        [DataField("friction")]
        private float _friction = 0.4f;

        /// <summary>
        /// AKA how much bounce there is on a collision.
        /// 0.0 for inelastic collision and 1.0 for elastic.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Restitution
        {
            get => _restitution;
            set
            {
                if (MathHelper.CloseTo(value, _restitution)) return;

                _restitution = value;
                Body.FixtureChanged(this);
            }
        }

        [DataField("restitution")]
        private float _restitution = 0f;

        /// <summary>
        ///     Non-hard <see cref="IPhysBody"/>s will not cause action collision (e.g. blocking of movement)
        ///     while still raising collision events.
        /// </summary>
        /// <remarks>
        ///     This is useful for triggers or such to detect collision without actually causing a blockage.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Hard
        {
            get => _hard;
            set
            {
                if (_hard == value)
                    return;

                _hard = value;
                Body.Awake = true;
                Body.FixtureChanged(this);
            }
        }

        [DataField("hard")]
        private bool _hard = true;

        // MassData
        // The reason these aren't a struct is because Serv3 + doing MassData in yaml everywhere would suck.
        // Plus now it's WAYYY easier to share shapes even among different prototypes.
        public float Inertia => _inertia;

        private float _inertia;

        // Should be calculated by density or whatever but eh.
        /// <summary>
        ///     Mass of the fixture. The sum of these is the mass of the body.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public float Mass
        {
            get => _mass;
            set
            {
                if (MathHelper.CloseTo(value, _mass)) return;

                _mass = MathF.Max(0f, value);
                Body.FixtureChanged(this);
                Body.ResetMassData();
                ComputeProperties();
            }
        }

        [DataField("mass")]
        private float _mass = 1.0f;

        /// <summary>
        /// Bitmask of the collision layers the component is a part of.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionLayer
        {
            get => _collisionLayer;
            set
            {
                if (_collisionLayer == value)
                    return;

                _collisionLayer = value;
                Body.FixtureChanged(this);
                EntitySystem.Get<SharedBroadphaseSystem>().Refilter(this);
            }
        }

        [DataField("layer", customTypeSerializer: typeof(FlagSerializer<CollisionLayer>))]
        private int _collisionLayer;

        /// <summary>
        ///  Bitmask of the layers this component collides with.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionMask
        {
            get => _collisionMask;
            set
            {
                if (_collisionMask == value)
                    return;

                _collisionMask = value;
                Body.FixtureChanged(this);
                EntitySystem.Get<SharedBroadphaseSystem>().Refilter(this);
            }
        }

        [DataField("mask", customTypeSerializer: typeof(FlagSerializer<CollisionMask>))]
        private int _collisionMask;

        public float Area
        {
            get
            {
                switch (Shape)
                {
                    case PhysShapeAabb aabb:
                        return aabb.LocalBounds.Width * aabb.LocalBounds.Height;
                    case PhysShapeRect rect:
                        return rect.Rectangle.Width * rect.Rectangle.Height;
                    case PhysShapeCircle circle:
                        return MathF.PI * circle.Radius * circle.Radius;
                    case PolygonShape poly:
                        ComputePoly(poly, out var area);
                        return area;
                    default:
                        return 0.0f;
                }
            }

        }

        void ISerializationHooks.AfterDeserialization()
        {
            // TODO: Temporary until PhysShapeAabb is fixed because some weird shit happens with collisions.
            // You'll also need a dedicated solver for circles (and ideally AABBs) as otherwise it'll be laggier casting to PolygonShape.
            if (Shape is PhysShapeAabb aabb)
            {
                var bounds = aabb.LocalBounds;
                var poly = new PolygonShape();
                Span<Vector2> verts = stackalloc Vector2[4];
                verts[0] = bounds.BottomLeft;
                verts[1] = bounds.BottomRight;
                verts[2] = bounds.TopRight;
                verts[3] = bounds.TopLeft;
                poly.SetVertices(verts);
                Shape = poly;
                DebugTools.Assert(Vertices.IsCounterClockwise(poly.Vertices));
            }
        }

        public Fixture(PhysicsComponent body, IPhysShape shape)
        {
            Body = body;
            Shape = shape;
        }

        public Fixture(IPhysShape shape, int collisionLayer, int collisionMask, bool hard)
        {
            Shape = shape;
            _collisionLayer = collisionLayer;
            _collisionMask = collisionMask;
            _hard = hard;
        }

        public Fixture() {}

        /// <summary>
        ///     As a bunch of things aren't serialized we need to instantiate Fixture from an empty ctor and then copy values across.
        /// </summary>
        /// <param name="fixture"></param>
        internal void CopyTo(Fixture fixture)
        {
            fixture.ID = ID;
            fixture.Shape = Shape;
            fixture._friction = _friction;
            fixture._restitution = _restitution;
            fixture._hard = _hard;
            fixture._collisionLayer = _collisionLayer;
            fixture._collisionMask = _collisionMask;
        }

        // Moved from Shape because no MassData on Shape anymore (due to serv3 and physics ease-of-use etc etc.)
        internal void ComputeProperties()
        {
            switch (Shape)
            {
                case EdgeShape edge:
                    ComputeEdge(edge);
                    break;
                case PhysShapeAabb aabb:
                    ComputeAABB(aabb);
                    break;
                case PhysShapeCircle circle:
                    ComputeCircle(circle);
                    break;
                case PhysShapeRect rect:
                    ComputeRect(rect);
                    break;
                case PolygonShape poly:
                    ComputePoly(poly, out _);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        #region ComputeProperties
        private void ComputeAABB(PhysShapeAabb aabb)
        {
            var area = aabb.LocalBounds.Width * aabb.LocalBounds.Height;
            float I = 0.0f;

            //The area is too small for the engine to handle.
            DebugTools.Assert(area > float.Epsilon);

            // Total mass
            // TODO: Do we need this?
            var density = area > 0.0f ? Mass / area : 0.0f;

            // Center of mass
            aabb.Centroid = Vector2.Zero;

            // Inertia tensor relative to the local origin (point s).
            _inertia = density * I;
        }

        private void ComputeRect(PhysShapeRect rect)
        {
            var area = rect.Rectangle.Width * rect.Rectangle.Height;
            float I = 0.0f;

            //The area is too small for the engine to handle.
            DebugTools.Assert(area > float.Epsilon);

            // Total mass
            // TODO: Do we need this?
            var density = area > 0.0f ? Mass / area : 0.0f;

            // Center of mass
            rect.Centroid = Vector2.Zero;

            // Inertia tensor relative to the local origin (point s).
            _inertia = density * I;
        }

        private void ComputePoly(PolygonShape poly, out float area)
        {
            // Polygon mass, centroid, and inertia.
            // Let rho be the polygon density in mass per unit area.
            // Then:
            // mass = rho * int(dA)
            // centroid.X = (1/mass) * rho * int(x * dA)
            // centroid.Y = (1/mass) * rho * int(y * dA)
            // I = rho * int((x*x + y*y) * dA)
            //
            // We can compute these integrals by summing all the integrals
            // for each triangle of the polygon. To evaluate the integral
            // for a single triangle, we make a change of variables to
            // the (u,v) coordinates of the triangle:
            // x = x0 + e1x * u + e2x * v
            // y = y0 + e1y * u + e2y * v
            // where 0 <= u && 0 <= v && u + v <= 1.
            //
            // We integrate u from [0,1-v] and then v from [0,1].
            // We also need to use the Jacobian of the transformation:
            // D = cross(e1, e2)
            //
            // Simplification: triangle centroid = (1/3) * (p1 + p2 + p3)
            //
            // The rest of the derivation is handled by computer algebra.

            var vertexCount = poly.Vertices.Length;

            DebugTools.Assert(vertexCount >= 3);

            //FPE optimization: Consolidated the calculate centroid and mass code to a single method.
            Vector2 center = Vector2.Zero;
            area = 0.0f;
            float I = 0.0f;

            // pRef is the reference point for forming triangles.
            // Its location doesn't change the result (except for rounding error).
            Vector2 s = Vector2.Zero;

            // This code would put the reference point inside the polygon.
            for (int i = 0; i < vertexCount; ++i)
            {
                s += poly.Vertices[i];
            }
            s *= 1.0f / vertexCount;

            const float k_inv3 = 1.0f / 3.0f;

            for (var i = 0; i < vertexCount; ++i)
            {
                // Triangle vertices.
                Vector2 e1 = poly.Vertices[i] - s;
                Vector2 e2 = i + 1 < vertexCount ? poly.Vertices[i + 1] - s : poly.Vertices[0] - s;

                float D = Vector2.Cross(e1, e2);

                float triangleArea = 0.5f * D;
                area += triangleArea;

                // Area weighted centroid
                center += (e1 + e2) * k_inv3 * triangleArea;

                float ex1 = e1.X, ey1 = e1.Y;
                float ex2 = e2.X, ey2 = e2.Y;

                float intx2 = ex1 * ex1 + ex2 * ex1 + ex2 * ex2;
                float inty2 = ey1 * ey1 + ey2 * ey1 + ey2 * ey2;

                I += (0.25f * k_inv3 * D) * (intx2 + inty2);
            }

            //The area is too small for the engine to handle.
            DebugTools.Assert(area > float.Epsilon);

            // Total mass
            // TODO: Do we need this?
            var density = area > 0.0f ? Mass / area : 0.0f;

            // Center of mass
            center *= 1.0f / area;
            poly.Centroid = center + s;

            // Inertia tensor relative to the local origin (point s).
            _inertia = density * I;

            // Shift to center of mass then to original body origin.
            _inertia += Mass * (Vector2.Dot(poly.Centroid, poly.Centroid) - Vector2.Dot(center, center));
        }

        private void ComputeCircle(PhysShapeCircle circle)
        {
            var radSquared = MathF.Pow(circle.Radius, 2);

            // inertia about the local origin
            _inertia = Mass * (0.5f * radSquared + Vector2.Dot(circle.Position, circle.Position));
        }

        private void ComputeEdge(EdgeShape edge)
        {
            edge.Centroid = (edge.Vertex1 + edge.Vertex2) * 0.5f;
        }
        #endregion

        // This is a crude equals mainly to avoid having to re-create the fixtures every time a state comes in.
        public bool Equals(Fixture? other)
        {
            if (other == null) return false;

            return _hard == other.Hard &&
                   _collisionLayer == other.CollisionLayer &&
                   _collisionMask == other.CollisionMask &&
                   Shape.Equals(other.Shape) &&
                   Body == other.Body &&
                   ID.Equals(other.ID);
        }
    }

    /// <summary>
    /// Tag type for defining the representation of the collision layer bitmask
    /// in terms of readable names in the content. To understand more about the
    /// point of this type, see the <see cref="FlagsForAttribute"/>.
    /// </summary>
    public sealed class CollisionLayer {}

    /// <summary>
    /// Tag type for defining the representation of the collision mask bitmask
    /// in terms of readable names in the content. To understand more about the
    /// point of this type, see the <see cref="FlagsForAttribute"/>.
    /// </summary>
    public sealed class CollisionMask {}
}
