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
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    public sealed class Contact : IEquatable<Contact>
    {
        private readonly IManifoldManager _manifoldManager;

#if DEBUG
        internal SharedDebugPhysicsSystem _debugPhysics = default!;
#endif

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

        public EntityUid EntityA;
        public EntityUid EntityB;

        public string FixtureAId = string.Empty;
        public string FixtureBId = string.Empty;

        public Fixture? FixtureA;
        public Fixture? FixtureB;

        public PhysicsComponent? BodyA;
        public PhysicsComponent? BodyB;

        public Manifold Manifold;

        internal ContactType Type;

        internal ContactFlags Flags = ContactFlags.None;

        internal Contact(IManifoldManager manifoldManager)
        {
            _manifoldManager = manifoldManager;

            MapNode = new LinkedListNode<Contact>(this);
            BodyANode = new LinkedListNode<Contact>(this);
            BodyBNode = new LinkedListNode<Contact>(this);
        }

        /// <summary>
        ///     Determines whether the contact is touching.
        /// </summary>
        [ViewVariables]
        public bool IsTouching { get; internal set; }

        /// Enable/disable this contact. This can be used inside the pre-solve
        /// contact listener. The contact is only disabled for the current
        /// time step (or sub-step in continuous collisions).
        [ViewVariables]
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

        [ViewVariables]
        public bool Deleting => (Flags & ContactFlags.Deleting) == ContactFlags.Deleting;

        /// <summary>
        /// If either fixture is hard then it's a hard contact.
        /// </summary>
        public bool Hard => FixtureA != null && FixtureB != null && (FixtureA.Hard && FixtureB.Hard);

        public void ResetRestitution()
        {
            Restitution = MathF.Max(FixtureA?.Restitution ?? 0.0f, FixtureB?.Restitution ?? 0.0f);
        }

        public void ResetFriction()
        {
            Friction = MathF.Sqrt((FixtureA?.Friction ?? 0.0f) * (FixtureB?.Friction ?? 0.0f));
        }

        public void GetWorldManifold(Transform transformA, Transform transformB, out Vector2 normal)
        {
            var shapeA = FixtureA?.Shape!;
            var shapeB = FixtureB?.Shape!;
            Span<Vector2> points = stackalloc Vector2[PhysicsConstants.MaxPolygonVertices];

            SharedPhysicsSystem.InitializeManifold(ref Manifold, transformA, transformB, shapeA.Radius, shapeB.Radius, out normal, points);
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

        /// <summary>
        /// Update the contact manifold and touching status.
        /// Note: do not assume the fixture AABBs are overlapping or are valid.
        /// </summary>
        /// <param name="wake">Whether we should wake the bodies due to touching changing.</param>
        /// <returns>What current status of the contact is (e.g. start touching, end touching, etc.)</returns>
        internal ContactStatus Update(Transform bodyATransform, Transform bodyBTransform, out bool wake)
        {
            var oldManifold = Manifold;

            // Re-enable this contact.
            Enabled = true;

            bool touching;
            var wasTouching = IsTouching;

            wake = false;
            var sensor = !(FixtureA!.Hard && FixtureB!.Hard);

            // Is this contact a sensor?
            if (sensor)
            {
                var shapeA = FixtureA!.Shape;
                var shapeB = FixtureB!.Shape;
                touching = _manifoldManager.TestOverlap(shapeA,  ChildIndexA, shapeB, ChildIndexB, bodyATransform, bodyBTransform);

                // Sensors don't generate manifolds.
                Manifold.PointCount = 0;
            }
            else
            {
                Evaluate(ref Manifold, bodyATransform, bodyBTransform);
                touching = Manifold.PointCount > 0;

                // Match old contact ids to new contact ids and copy the
                // stored impulses to warm start the solver.
                var points = Manifold.Points.AsSpan;
                var oldPoints = oldManifold.Points.AsSpan;

                for (var i = 0; i < Manifold.PointCount; ++i)
                {
                    var mp2 = points[i];
                    mp2.NormalImpulse = 0.0f;
                    mp2.TangentImpulse = 0.0f;
                    var id2 = mp2.Id;

                    for (var j = 0; j < oldManifold.PointCount; ++j)
                    {
                        var mp1 = oldPoints[j];

                        if (mp1.Id.Key == id2.Key)
                        {
                            mp2.NormalImpulse = mp1.NormalImpulse;
                            mp2.TangentImpulse = mp1.TangentImpulse;
                            break;
                        }
                    }

                    points[i] = mp2;
                }

                if (touching != wasTouching)
                {
                    wake = true;
                }
            }

            IsTouching = touching;
            var status = ContactStatus.NoContact;

            if (!wasTouching)
            {
                if (touching)
                {
                    status = ContactStatus.StartTouching;
                }
            }
            else
            {
                if (!touching)
                {
                    status = ContactStatus.EndTouching;
                }
            }

#if DEBUG
            if (!sensor)
            {
                _debugPhysics.HandlePreSolve(this, oldManifold);
            }
#endif

            return status;
        }

        /// <summary>
        /// Trimmed down version of <see cref="Update"/> that only updates whether or not the contact's shapes are
        /// touching.
        /// </summary>
        internal void UpdateIsTouching(Transform bodyATransform, Transform bodyBTransform)
        {
            var sensor = !(FixtureA!.Hard && FixtureB!.Hard);
            if (sensor)
            {
                var shapeA = FixtureA!.Shape;
                var shapeB = FixtureB!.Shape;
                IsTouching = _manifoldManager.TestOverlap(shapeA,  ChildIndexA, shapeB, ChildIndexB, bodyATransform, bodyBTransform);
            }
            else
            {
                var manifold = Manifold;
                Evaluate(ref manifold, bodyATransform, bodyBTransform);
                IsTouching = manifold.PointCount > 0;
            }
        }

        /// <summary>
        ///     Evaluate this contact with your own manifold and transforms.
        /// </summary>
        /// <param name="manifold">The manifold.</param>
        /// <param name="transformA">The first transform.</param>
        /// <param name="transformB">The second transform.</param>
        private void Evaluate(ref Manifold manifold, in Transform transformA, in Transform transformB)
        {
            // This is expensive and shitcodey, see below.
            switch (Type)
            {
                // TODO: Need a unit test for these.
                case ContactType.Polygon:
                    _manifoldManager.CollidePolygons(ref manifold, (PolygonShape) FixtureA!.Shape, transformA, (PolygonShape) FixtureB!.Shape, transformB);
                    break;
                case ContactType.PolygonAndCircle:
                    _manifoldManager.CollidePolygonAndCircle(ref manifold, (PolygonShape) FixtureA!.Shape, transformA, (PhysShapeCircle) FixtureB!.Shape, transformB);
                    break;
                case ContactType.EdgeAndCircle:
                    _manifoldManager.CollideEdgeAndCircle(ref manifold, (EdgeShape) FixtureA!.Shape, transformA, (PhysShapeCircle) FixtureB!.Shape, transformB);
                    break;
                case ContactType.EdgeAndPolygon:
                    _manifoldManager.CollideEdgeAndPolygon(ref manifold, (EdgeShape) FixtureA!.Shape, transformA, (PolygonShape) FixtureB!.Shape, transformB);
                    break;
                case ContactType.ChainAndCircle:
                {
                    var chain = (ChainShape) FixtureA!.Shape;
                    var edge = _manifoldManager.GetContactEdge();
                    chain.GetChildEdge(ref edge, ChildIndexA);
                    _manifoldManager.CollideEdgeAndCircle(ref manifold, edge, in transformA, (PhysShapeCircle) FixtureB!.Shape, in transformB);
                    _manifoldManager.ReturnEdge(edge);
                    break;
                }
                case ContactType.ChainAndPolygon:
                {
                    var loop2 = (ChainShape) FixtureA!.Shape;
                    var edge = _manifoldManager.GetContactEdge();
                    loop2.GetChildEdge(ref edge, ChildIndexA);
                    _manifoldManager.CollideEdgeAndPolygon(ref manifold, edge, in transformA, (PolygonShape) FixtureB!.Shape, in transformB);
                    _manifoldManager.ReturnEdge(edge);
                    break;
                }
                case ContactType.Circle:
                    _manifoldManager.CollideCircles(ref manifold, (PhysShapeCircle) FixtureA!.Shape, in transformA, (PhysShapeCircle) FixtureB!.Shape, in transformB);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Collision between {FixtureA!.Shape.GetType()} and {FixtureB!.Shape.GetType()} not supported");
            }
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
            return HashCode.Combine(EntityA, EntityB);
        }

        [Pure]
        public EntityUid OurEnt(EntityUid uid)
        {
            if (uid == EntityA)
                return EntityA;
            else if (uid == EntityB)
                return EntityB;

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Gets the other ent for this contact.
        /// </summary>
        [Pure]
        public EntityUid OtherEnt(EntityUid uid)
        {
            if (uid == EntityA)
                return EntityB;
            else if (uid == EntityB)
                return EntityA;

            throw new InvalidOperationException();
        }

        [Pure, PublicAPI]
        public (string Id, Fixture) OurFixture(EntityUid uid)
        {
            if (uid == EntityA)
                return (FixtureAId, FixtureA!);
            else if (uid == EntityB)
                return (FixtureBId, FixtureB!);

            throw new InvalidOperationException();
        }

        [Pure, PublicAPI]
        public (string Id, Fixture) OtherFixture(EntityUid uid)
        {
            if (uid == EntityA)
                return (FixtureBId, FixtureB!);
            else if (uid == EntityB)
                return (FixtureAId, FixtureA!);

            throw new InvalidOperationException();
        }

        [Pure, PublicAPI]
        public PhysicsComponent OurBody(EntityUid uid)
        {
            if (uid == EntityA)
                return BodyA!;
            else if (uid == EntityB)
                return BodyB!;

            throw new InvalidOperationException();
        }

        [Pure, PublicAPI]
        public PhysicsComponent OtherBody(EntityUid uid)
        {
            if (uid == EntityA)
                return BodyB!;
            else if (uid == EntityB)
                return BodyA!;

            throw new InvalidOperationException();
        }
    }

    [Flags]
    internal enum ContactFlags : byte
    {
        None = 0,

        /// <summary>
        /// Is the contact pending its first manifold generation.
        /// </summary>
        PreInit = 1 << 0,

        /// <summary>
        ///     Has this contact already been added to an island?
        /// </summary>
        Island = 1 << 1,

        /// <summary>
        ///     Does this contact need re-filtering?
        /// </summary>
        Filter = 1 << 2,

        /// <summary>
        /// Is this a special contact for grid-grid collisions
        /// </summary>
        Grid = 1 << 3,

        /// <summary>
        /// Set right before the contact is deleted
        /// </summary>
        Deleting = 1 << 4,

        /// <summary>
        /// Set after a contact has been deleted and returned to the contact pool.
        /// </summary>
        Deleted = 1 << 5,
    }
}
