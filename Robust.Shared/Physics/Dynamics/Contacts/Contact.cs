// Copyright (c) 2017 Kastellanos Nikolaos

/* Original source Farseer Physics Engine:
 * Copyright (c) 2014 Ian Qvist, http://farseerphysics.codeplex.com
 * Microsoft Permissive License (Ms-PL) v1.1
 */

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
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    public sealed class Contact : IEquatable<Contact>
    {
        // Store these nodes so we can do fast removals when required, rather than having to iterate every node
        // trying to find it.

        /// <summary>
        /// The node of this contact on the map.
        /// </summary>
        public readonly LinkedListNode<Contact> MapNode;

        /// <summary>
        /// The node of this contact on body A.
        /// </summary>
        public readonly LinkedListNode<Contact> BodyANode;

        /// <summary>
        /// The node of this contact on body A.
        /// </summary>
        public readonly LinkedListNode<Contact> BodyBNode;

        public Fixture? FixtureA;
        public Fixture? FixtureB;

        public Manifold Manifold;

        internal ContactType Type;

        internal ContactFlags Flags = ContactFlags.None;

        internal Contact()
        {
            MapNode = new LinkedListNode<Contact>(this);
            BodyANode = new LinkedListNode<Contact>(this);
            BodyBNode = new LinkedListNode<Contact>(this);
        }

        /// <summary>
        ///     Determines whether the contact is touching.
        /// </summary>
        public bool IsTouching { get; internal set; }

        /// Enable/disable this contact. This can be used inside the pre-solve
        /// contact listener. The contact is only disabled for the current
        /// time step (or sub-step in continuous collisions).
        public bool Enabled { get; set; }

        /// <summary>
        ///     Get the child primitive index for fixture A.
        /// </summary>
        public int ChildIndexA { get; internal set; }

        /// <summary>
        ///     Get the child primitive index for fixture B.
        /// </summary>
        public int ChildIndexB { get; internal set; }

        /// <summary>
        ///     The mixed friction of the 2 fixtures.
        /// </summary>
        public float Friction { get; set; }

        /// <summary>
        ///     The mixed restitution of the 2 fixtures.
        /// </summary>
        public float Restitution { get; set; }

        /// <summary>
        ///     Used for conveyor belt behavior in m/s.
        /// </summary>
        public float TangentSpeed { get; set; }

        public bool IsSensor => !(FixtureA!.Hard && FixtureB!.Hard);

        public void ResetRestitution()
        {
            Restitution = MathF.Max(FixtureA?.Restitution ?? 0.0f, FixtureB?.Restitution ?? 0.0f);
        }

        public void ResetFriction()
        {
            Friction = MathF.Sqrt(FixtureA?.Friction ?? 0.0f * FixtureB?.Friction ?? 0.0f);
        }

        /// <summary>
        /// Gets the world manifold.
        /// </summary>
        public void GetWorldManifold(Transform transformA, Transform transformB, out Vector2 normal, Span<Vector2> points)
        {
            var shapeA = FixtureA?.Shape!;
            var shapeB = FixtureB?.Shape!;

            SharedPhysicsSystem.InitializeManifold(ref Manifold, transformA, transformB, shapeA.Radius, shapeB.Radius, out normal, points);
        }

        public enum ContactType : byte
        {
            NotSupported,
            Polygon,
            PolygonAndCircle,
            Circle,
            EdgeAndPolygon,
            EdgeAndCircle,
            ChainAndPolygon,
            ChainAndCircle,
        }

        public bool Equals(Contact? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(FixtureA, other.FixtureA) &&
                   Equals(FixtureB, other.FixtureB) &&
                   Manifold.Equals(other.Manifold) &&
                   Type == other.Type &&
                   Enabled == other.Enabled &&
                   ChildIndexA == other.ChildIndexA &&
                   ChildIndexB == other.ChildIndexB &&
                   Friction.Equals(other.Friction) &&
                   Restitution.Equals(other.Restitution);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is Contact other && Equals(other);
        }

        public override int GetHashCode()
        {
            // TODO: Need to suss this out
            return HashCode.Combine((FixtureA != null ? FixtureA.Body.Owner : EntityUid.Invalid), (FixtureB != null ? FixtureB.Body.Owner : EntityUid.Invalid));
        }
    }

    [Flags]
    internal enum ContactFlags : byte
    {
        None = 0,
        /// <summary>
        ///     Has this contact already been added to an island?
        /// </summary>
        Island = 1 << 0,

        /// <summary>
        ///     Does this contact need re-filtering?
        /// </summary>
        Filter = 1 << 1,

        /// <summary>
        /// Is this a special contact for grid-grid collisions
        /// </summary>
        Grid = 1 << 2,
    }
}
