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
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics
{
    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed partial class Fixture : IEquatable<Fixture>, ISerializationHooks
    {
        [ViewVariables]
        [field: NonSerialized]
        public FixtureProxy[] Proxies { get; set; } = Array.Empty<FixtureProxy>();

        [ViewVariables]
        [NonSerialized]
        public int ProxyCount = 0;

        [DataField("shape")]
        public IPhysShape Shape { get; private set; } = new PhysShapeAabb();

        [NonSerialized]
        public EntityUid Owner;

        /// <summary>
        /// All of the other fixtures this fixture has a contact with.
        /// </summary>
        [ViewVariables]
        [NonSerialized]
        public Dictionary<Fixture, Contact> Contacts = new();

        /// <summary>
        /// Contact friction between 2 bodies. Not tile-friction for top-down.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("friction"), Access(typeof(SharedPhysicsSystem), typeof(FixtureSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
        public float Friction = PhysicsConstants.DefaultContactFriction;

        /// <summary>
        /// AKA how much bounce there is on a collision.
        /// 0.0 for inelastic collision and 1.0 for elastic.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("restitution"), Access(typeof(SharedPhysicsSystem), typeof(FixtureSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
        public float Restitution = PhysicsConstants.DefaultRestitution;

        /// <summary>
        ///     Non-hard <see cref="PhysicsComponent"/>s will not cause action collision (e.g. blocking of movement)
        ///     while still raising collision events.
        /// </summary>
        /// <remarks>
        ///     This is useful for triggers or such to detect collision without actually causing a blockage.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite), DataField("hard"), Access(typeof(SharedPhysicsSystem), typeof(FixtureSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
        public bool Hard = true;

        /// <summary>
        /// In kg / m ^ 2
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("density"),
         Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute,
             Other = AccessPermissions.Read)]
        public float Density = PhysicsConstants.DefaultDensity;

        /// <summary>
        /// Bitmask of the collision layers the component is a part of.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("layer", customTypeSerializer: typeof(FlagSerializer<CollisionLayer>)),
         Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute,
             Other = AccessPermissions.Read)]
        public int CollisionLayer;

        /// <summary>
        ///  Bitmask of the layers this component collides with.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("mask", customTypeSerializer: typeof(FlagSerializer<CollisionMask>)),
         Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute,
             Other = AccessPermissions.Read)]
        public int CollisionMask;

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
                poly.Set(verts, 4);
                Shape = poly;
            }
        }

        internal Fixture(
            IPhysShape shape,
            int collisionLayer,
            int collisionMask,
            bool hard,
            float density = PhysicsConstants.DefaultDensity,
            float friction = PhysicsConstants.DefaultContactFriction,
            float restitution = PhysicsConstants.DefaultRestitution)
        {
            Shape = shape;
            CollisionLayer = collisionLayer;
            CollisionMask = collisionMask;
            Hard = hard;
            Density = density;
            Friction = friction;
            Restitution = restitution;
        }

        public Fixture()
        {
        }

        /// <summary>
        ///     As a bunch of things aren't serialized we need to instantiate Fixture from an empty ctor and then copy values across.
        /// </summary>
        /// <param name="fixture"></param>
        internal void CopyTo(Fixture fixture)
        {
            fixture.Shape = Shape;
            fixture.Friction = Friction;
            fixture.Restitution = Restitution;
            fixture.Hard = Hard;
            fixture.CollisionLayer = CollisionLayer;
            fixture.CollisionMask = CollisionMask;
            fixture.Density = Density;
        }

        /// <summary>
        /// Returns true if equal apart from body reference.
        /// </summary>
        public bool Equivalent(Fixture other)
        {
            return Hard == other.Hard &&
                   CollisionLayer == other.CollisionLayer &&
                   CollisionMask == other.CollisionMask &&
                   Shape.Equals(other.Shape) &&
                   MathHelper.CloseTo(Density, other.Density);
        }

        // This is a crude equals mainly to avoid having to re-create the fixtures every time a state comes in.
        public bool Equals(Fixture? other)
        {
            if (other == null) return false;

            return Equivalent(other) && Owner == other.Owner;
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
