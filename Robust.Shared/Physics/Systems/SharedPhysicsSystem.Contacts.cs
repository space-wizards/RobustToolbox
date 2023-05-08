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
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Events;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    // TODO: Jesus we should really have a test for this
    /// <summary>
    ///     Ordering is under <see cref="ShapeType"/>
    ///     uses enum to work out which collision evaluation to use.
    /// </summary>
    private static Contact.ContactType[,] _registers =
    {
       {
           // Circle register
           Contact.ContactType.Circle,
           Contact.ContactType.EdgeAndCircle,
           Contact.ContactType.PolygonAndCircle,
           Contact.ContactType.ChainAndCircle,
       },
       {
           // Edge register
           Contact.ContactType.EdgeAndCircle,
           Contact.ContactType.NotSupported, // Edge
           Contact.ContactType.EdgeAndPolygon,
           Contact.ContactType.NotSupported, // Chain
       },
       {
           // Polygon register
           Contact.ContactType.PolygonAndCircle,
           Contact.ContactType.EdgeAndPolygon,
           Contact.ContactType.Polygon,
           Contact.ContactType.ChainAndPolygon,
       },
       {
           // Chain register
           Contact.ContactType.ChainAndCircle,
           Contact.ContactType.NotSupported, // Edge
           Contact.ContactType.ChainAndPolygon,
           Contact.ContactType.NotSupported, // Chain
       }
   };

    private int ContactCount => _activeContacts.Count;

    private const int ContactPoolInitialSize = 128;
    private const int ContactsPerThread = 32;

    private ObjectPool<Contact> _contactPool = default!;

    private readonly LinkedList<Contact> _activeContacts = new();

    private sealed class ContactPoolPolicy : IPooledObjectPolicy<Contact>
    {
        private readonly SharedDebugPhysicsSystem _debugPhysicsSystem;
        private readonly IManifoldManager _manifoldManager;

        public ContactPoolPolicy(SharedDebugPhysicsSystem debugPhysicsSystem, IManifoldManager manifoldManager)
        {
            _debugPhysicsSystem = debugPhysicsSystem;
            _manifoldManager = manifoldManager;
        }

        public Contact Create()
        {
            var contact = new Contact(_manifoldManager);
#if DEBUG
            contact._debugPhysics = _debugPhysicsSystem;
#endif
            contact.Manifold = new Manifold
            {
                Points = new ManifoldPoint[2]
            };

            return contact;
        }

        public bool Return(Contact obj)
        {
            SetContact(obj, EntityUid.Invalid, EntityUid.Invalid, null, 0, null, 0);
            return true;
        }
    }

    private static void SetContact(Contact contact, EntityUid uidA, EntityUid uidB, Fixture? fixtureA, int indexA, Fixture? fixtureB, int indexB)
    {
        contact.Enabled = true;
        contact.IsTouching = false;
        contact.Flags = ContactFlags.None;
        // TOIFlag = false;

        contact.EntityA = uidA;
        contact.EntityB = uidB;

        contact.FixtureA = fixtureA;
        contact.FixtureB = fixtureB;

        contact.BodyA = fixtureA?.Body;
        contact.BodyB = fixtureB?.Body;

        contact.ChildIndexA = indexA;
        contact.ChildIndexB = indexB;

        contact.Manifold.PointCount = 0;

        //FPE: We only set the friction and restitution if we are not destroying the contact
        if (fixtureA != null && fixtureB != null)
        {
            contact.Friction = MathF.Sqrt(fixtureA.Friction * fixtureB.Friction);
            contact.Restitution = MathF.Max(fixtureA.Restitution, fixtureB.Restitution);
        }

        contact.TangentSpeed = 0;
    }

    private void InitializeContacts()
    {
        _contactPool = new DefaultObjectPool<Contact>(
            new ContactPoolPolicy(_debugPhysics, _manifoldManager),
            4096);

        InitializePool();
    }

    private void InitializePool()
    {
        var dummy = new Contact[ContactPoolInitialSize];

        for (var i = 0; i < ContactPoolInitialSize; i++)
        {
            dummy[i] = _contactPool.Get();
        }

        for (var i = 0; i < ContactPoolInitialSize; i++)
        {
            _contactPool.Return(dummy[i]);
        }
    }

    private Contact CreateContact(EntityUid uidA, EntityUid uidB, Fixture fixtureA, int indexA, Fixture fixtureB, int indexB)
    {
        var type1 = fixtureA.Shape.ShapeType;
        var type2 = fixtureB.Shape.ShapeType;

        DebugTools.Assert(ShapeType.Unknown < type1 && type1 < ShapeType.TypeCount);
        DebugTools.Assert(ShapeType.Unknown < type2 && type2 < ShapeType.TypeCount);

        // Pull out a spare contact object
        var contact = _contactPool.Get();

        // Edge+Polygon is non-symmetrical due to the way Erin handles collision type registration.
        if ((type1 >= type2 || (type1 == ShapeType.Edge && type2 == ShapeType.Polygon)) && !(type2 == ShapeType.Edge && type1 == ShapeType.Polygon))
        {
            SetContact(contact, uidA, uidB, fixtureA, indexA, fixtureB, indexB);
        }
        else
        {
            SetContact(contact, uidB, uidA, fixtureB, indexB, fixtureA, indexA);
        }

        contact.Type = _registers[(int)type1, (int)type2];

        return contact;
    }

    /// <summary>
    /// Try to create a contact between these 2 fixtures.
    /// </summary>
    internal void AddPair(EntityUid uidA, EntityUid uidB, Fixture fixtureA, int indexA, Fixture fixtureB, int indexB, ContactFlags flags = ContactFlags.None)
    {
        var bodyA = fixtureA.Body;
        var bodyB = fixtureB.Body;

        // Broadphase has already done the faster check for collision mask / layers
        // so no point duplicating

        // Does a contact already exist?
        if (fixtureA.Contacts.ContainsKey(fixtureB))
            return;

        DebugTools.Assert(!fixtureB.Contacts.ContainsKey(fixtureA));

        // Does a joint override collision? Is at least one body dynamic?
        if (!ShouldCollide(bodyB, bodyA, fixtureA, fixtureB))
            return;

        // Call the factory.
        var contact = CreateContact(uidA, uidB, fixtureA, indexA, fixtureB, indexB);
        contact.Flags = flags;

        // Contact creation may swap fixtures.
        fixtureA = contact.FixtureA!;
        fixtureB = contact.FixtureB!;
        bodyA = contact.BodyA!;
        bodyB = contact.BodyB!;

        // Insert into world
        _activeContacts.AddLast(contact.MapNode);

        // Connect to body A
        DebugTools.Assert(!fixtureA.Contacts.ContainsKey(fixtureB));
        fixtureA.Contacts.Add(fixtureB, contact);
        bodyA.Contacts.AddLast(contact.BodyANode);

        // Connect to body B
        DebugTools.Assert(!fixtureB.Contacts.ContainsKey(fixtureA));
        fixtureB.Contacts.Add(fixtureA, contact);
        bodyB.Contacts.AddLast(contact.BodyBNode);
    }

    /// <summary>
    ///     Go through the cached broadphase movement and update contacts.
    /// </summary>
    internal void AddPair(in FixtureProxy proxyA, in FixtureProxy proxyB)
    {
        AddPair(proxyA.Entity, proxyB.Entity, proxyA.Fixture, proxyA.ChildIndex, proxyB.Fixture, proxyB.ChildIndex);
    }

    internal static bool ShouldCollide(Fixture fixtureA, Fixture fixtureB)
    {
        return !((fixtureA.CollisionMask & fixtureB.CollisionLayer) == 0x0 &&
                 (fixtureB.CollisionMask & fixtureA.CollisionLayer) == 0x0);
    }

    public void DestroyContact(Contact contact)
    {
        Fixture fixtureA = contact.FixtureA!;
        Fixture fixtureB = contact.FixtureB!;
        var bodyA = contact.BodyA!;
        var bodyB = contact.BodyB!;
        var aUid = bodyA.Owner;
        var bUid = bodyB.Owner;

        if (contact.IsTouching)
        {
            var ev1 = new EndCollideEvent(aUid, bUid, fixtureA, fixtureB, bodyA, bodyB);
            var ev2 = new EndCollideEvent(bUid, aUid, fixtureB, fixtureA, bodyB, bodyA);
            RaiseLocalEvent(aUid, ref ev1);
            RaiseLocalEvent(bUid, ref ev2);
        }

        if (contact.Manifold.PointCount > 0 && contact.FixtureA?.Hard == true && contact.FixtureB?.Hard == true)
        {
            if (bodyA.CanCollide)
                SetAwake(aUid, bodyA, true);

            if (bodyB.CanCollide)
                SetAwake(bUid, bodyB, true);
        }

        // Remove from the world
        _activeContacts.Remove(contact.MapNode);

        // Remove from body 1
        DebugTools.Assert(fixtureA.Contacts.ContainsKey(fixtureB));
        fixtureA.Contacts.Remove(fixtureB);
        DebugTools.Assert(bodyA.Contacts.Contains(contact.BodyANode!.Value));
        bodyA.Contacts.Remove(contact.BodyANode);

        // Remove from body 2
        DebugTools.Assert(fixtureB.Contacts.ContainsKey(fixtureA));
        fixtureB.Contacts.Remove(fixtureA);
        bodyB.Contacts.Remove(contact.BodyBNode);

        // Insert into the pool.
        _contactPool.Return(contact);
    }

    internal void CollideContacts()
    {
        // Due to the fact some contacts may be removed (and we need to update this array as we iterate).
        // the length may not match the actual contact count, hence we track the index.
        var contacts = ArrayPool<Contact>.Shared.Rent(ContactCount);
        var index = 0;

        // Can be changed while enumerating
        // TODO: check for null instead?
        // Work out which contacts are still valid before we decide to update manifolds.
        var node = _activeContacts.First;
        var xformQuery = GetEntityQuery<TransformComponent>();

        while (node != null)
        {
            var contact = node.Value;
            node = node.Next;

            Fixture fixtureA = contact.FixtureA!;
            Fixture fixtureB = contact.FixtureB!;
            int indexA = contact.ChildIndexA;
            int indexB = contact.ChildIndexB;

            var bodyA = contact.BodyA!;
            var bodyB = contact.BodyB!;
            var uidA = contact.EntityA;
            var uidB = contact.EntityB;

            // Do not try to collide disabled bodies
            if (!bodyA.CanCollide || !bodyB.CanCollide)
            {
                DestroyContact(contact);
                continue;
            }

            // Is this contact flagged for filtering?
            if ((contact.Flags & ContactFlags.Filter) != 0x0)
            {
                // Check default filtering
                if (!ShouldCollide(fixtureA, fixtureB) ||
                    !ShouldCollide(bodyB, bodyA, fixtureA, fixtureB))
                {
                    DestroyContact(contact);
                    continue;
                }

                // Clear the filtering flag.
                contact.Flags &= ~ContactFlags.Filter;
            }

            bool activeA = bodyA.Awake && bodyA.BodyType != BodyType.Static;
            bool activeB = bodyB.Awake && bodyB.BodyType != BodyType.Static;

            // At least one body must be awake and it must be dynamic or kinematic.
            if (activeA == false && activeB == false)
            {
                continue;
            }

            var xformA = xformQuery.GetComponent(uidA);
            var xformB = xformQuery.GetComponent(uidB);

            if (xformA.MapUid == null || xformA.MapUid != xformB.MapUid)
            {
                DestroyContact(contact);
                continue;
            }

            // Special-case grid contacts.
            if ((contact.Flags & ContactFlags.Grid) != 0x0)
            {
                var gridABounds = fixtureA.Shape.ComputeAABB(GetPhysicsTransform(uidA, xformA, xformQuery), 0);
                var gridBBounds = fixtureB.Shape.ComputeAABB(GetPhysicsTransform(uidB, xformB, xformQuery), 0);

                if (!gridABounds.Intersects(gridBBounds))
                {
                    DestroyContact(contact);
                }
                else
                {
                    // Grid contact is still alive.
                    contact.Flags &= ~ContactFlags.Island;
                    if (index >= contacts.Length)
                    {
                        _sawmill.Error($"Insufficient contact length at 388! Index {index} and length is {contacts.Length}. Tell Sloth");
                    }
                    contacts[index++] = contact;
                }

                continue;
            }

            if (indexA >= fixtureA.Proxies.Length)
            {
                _sawmill.Error($"Found invalid contact index of {indexA} on {fixtureA.ID} / {ToPrettyString(uidA)}, expected {fixtureA.Proxies.Length}");
                DestroyContact(contact);
                continue;
            }

            if (indexB >= fixtureB.Proxies.Length)
            {
                _sawmill.Error($"Found invalid contact index of {indexB} on {fixtureB.ID} / {ToPrettyString(uidB)}, expected {fixtureB.Proxies.Length}");
                DestroyContact(contact);
                continue;
            }

            var proxyA = fixtureA.Proxies[indexA];
            var proxyB = fixtureB.Proxies[indexB];
            var broadphaseA = xformA.Broadphase?.Uid;
            var broadphaseB = xformB.Broadphase?.Uid;
            var overlap = false;

            // We can have cross-broadphase proxies hence need to change them to worldspace
            if (broadphaseA != null && broadphaseB != null)
            {
                if (broadphaseA == broadphaseB)
                {
                    overlap = proxyA.AABB.Intersects(proxyB.AABB);
                }
                else
                {
                    var proxyAWorldAABB = _transform.GetWorldMatrix(xformQuery.GetComponent(broadphaseA.Value), xformQuery).TransformBox(proxyA.AABB);
                    var proxyBWorldAABB = _transform.GetWorldMatrix(xformQuery.GetComponent(broadphaseB.Value), xformQuery).TransformBox(proxyB.AABB);
                    overlap = proxyAWorldAABB.Intersects(proxyBWorldAABB);
                }
            }

            // Here we destroy contacts that cease to overlap in the broad-phase.
            if (!overlap)
            {
                DestroyContact(contact);
                continue;
            }

            // Contact is actually going to live for manifold generation and solving.
            // This can also short-circuit above for grid contacts.
            contact.Flags &= ~ContactFlags.Island;
            if (index >= contacts.Length)
            {
                _sawmill.Error($"Insufficient contact length at 429! Index {index} and length is {contacts.Length}. Tell Sloth");
            }
            contacts[index++] = contact;
        }

        var status = ArrayPool<ContactStatus>.Shared.Rent(index);
        var worldPoints = ArrayPool<Vector2>.Shared.Rent(index);

        // Update contacts all at once.
        BuildManifolds(contacts, index, status, worldPoints);

        // Single-threaded so content doesn't need to worry about race conditions.
        for (var i = 0; i < index; i++)
        {
            if (i >= contacts.Length)
            {
                _sawmill.Error($"Invalid contact length for contact events!");
                continue;
            }

            var contact = contacts[i];

            switch (status[i])
            {
                case ContactStatus.StartTouching:
                {
                    if (!contact.IsTouching) continue;

                    var fixtureA = contact.FixtureA!;
                    var fixtureB = contact.FixtureB!;
                    var bodyA = fixtureA.Body;
                    var bodyB = fixtureB.Body;
                    var uidA = contact.EntityA;
                    var uidB = contact.EntityB;
                    var worldPoint = worldPoints[i];

                    var ev1 = new StartCollideEvent(uidA, uidB, fixtureA, fixtureB, bodyA, bodyB, worldPoint);
                    var ev2 = new StartCollideEvent(uidB, uidA, fixtureB, fixtureA, bodyB, bodyA, worldPoint);

                    RaiseLocalEvent(uidA, ref ev1, true);
                    RaiseLocalEvent(uidB, ref ev2, true);
                    break;
                }
                case ContactStatus.Touching:
                    break;
                case ContactStatus.EndTouching:
                {
                    var fixtureA = contact.FixtureA;
                    var fixtureB = contact.FixtureB;

                    // If something under StartCollideEvent potentially nukes other contacts (e.g. if the entity is deleted)
                    // then we'll just skip the EndCollide.
                    if (fixtureA == null || fixtureB == null) continue;

                    var bodyA = contact.BodyA!;
                    var bodyB = contact.BodyB!;
                    var uidA = contact.EntityA;
                    var uidB = contact.EntityB;

                    var ev1 = new EndCollideEvent(uidA, uidB, fixtureA, fixtureB, bodyA, bodyB);
                    var ev2 = new EndCollideEvent(uidB, uidA, fixtureB, fixtureA, bodyB, bodyA);

                    RaiseLocalEvent(uidA, ref ev1);
                    RaiseLocalEvent(uidB, ref ev2);
                    break;
                }
                case ContactStatus.NoContact:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        ArrayPool<Contact>.Shared.Return(contacts);
        ArrayPool<ContactStatus>.Shared.Return(status);
        ArrayPool<Vector2>.Shared.Return(worldPoints);
    }

    private void BuildManifolds(Contact[] contacts, int count, ContactStatus[] status, Vector2[] worldPoints)
    {
        var wake = ArrayPool<bool>.Shared.Rent(count);

        if (count > ContactsPerThread * 2)
        {
            var batches = (int) Math.Ceiling((float) count / ContactsPerThread);

            Parallel.For(0, batches, i =>
            {
                var start = i * ContactsPerThread;
                var end = Math.Min(start + ContactsPerThread, count);
                UpdateContacts(contacts, start, end, status, wake, worldPoints);
            });

        }
        else
        {
            UpdateContacts(contacts, 0, count, status, wake, worldPoints);
        }

        // Can't do this during UpdateContacts due to IoC threading issues.
        for (var i = 0; i < count; i++)
        {
            var shouldWake = wake[i];
            if (!shouldWake) continue;

            var contact = contacts[i];
            var bodyA = contact.BodyA!;
            var bodyB = contact.BodyB!;
            var aUid = contact.EntityA;
            var bUid = contact.EntityB;

            SetAwake(aUid, bodyA, true);
            SetAwake(bUid, bodyB, true);
        }

        ArrayPool<bool>.Shared.Return(wake);
    }

    private void UpdateContacts(Contact[] contacts, int start, int end, ContactStatus[] status, bool[] wake, Vector2[] worldPoints)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();

        for (var i = start; i < end; i++)
        {
            var contact = contacts[i];

            // TODO: Temporary measure. When Box2D 3.0 comes out expect a major refactor
            // of everything
            if (contact.FixtureA == null || contact.FixtureB == null)
            {
                _sawmill.Error($"Found a null contact for contact at {i}");
                status[i] = ContactStatus.NoContact;
                wake[i] = false;
                DebugTools.Assert(false);
                continue;
            }

            var uidA = contact.EntityA;
            var uidB = contact.EntityB;
            var bodyATransform = GetPhysicsTransform(uidA, xformQuery.GetComponent(uidA), xformQuery);
            var bodyBTransform = GetPhysicsTransform(uidB, xformQuery.GetComponent(uidB), xformQuery);

            var contactStatus = contact.Update(bodyATransform, bodyBTransform, out wake[i]);
            status[i] = contactStatus;

            if (contactStatus == ContactStatus.StartTouching)
            {
                worldPoints[i] = Physics.Transform.Mul(bodyATransform, contacts[i].Manifold.LocalPoint);
            }
        }
    }

    /// <summary>
    ///     Used to prevent bodies from colliding; may lie depending on joints.
    /// </summary>
    private bool ShouldCollide(PhysicsComponent body, PhysicsComponent other, Fixture fixture, Fixture otherFixture)
    {
        if (((body.BodyType & (BodyType.Kinematic | BodyType.Static)) != 0 &&
            (other.BodyType & (BodyType.Kinematic | BodyType.Static)) != 0) ||
            // Kinematic controllers can't collide.
            (body.BodyType == BodyType.KinematicController &&
             other.BodyType == BodyType.KinematicController))
        {
            return false;
        }

        // Does a joint prevent collision?
        // if one of them doesn't have jointcomp then they can't share a common joint.
        // otherwise, only need to iterate over the joints of one component as they both store the same joint.
        if (TryComp(body.Owner, out JointComponent? jointComponentA) &&
            TryComp(other.Owner, out JointComponent? jointComponentB))
        {
            var aUid = jointComponentA.Owner;
            var bUid = jointComponentB.Owner;

            foreach (var joint in jointComponentA.Joints.Values)
            {
                // Check if either: the joint even allows collisions OR the other body on the joint is actually the other body we're checking.
                if (!joint.CollideConnected &&
                    ((aUid == joint.BodyAUid &&
                     bUid == joint.BodyBUid) ||
                    (bUid == joint.BodyAUid &&
                     aUid == joint.BodyBUid))) return false;
            }
        }

        var preventCollideMessage = new PreventCollideEvent(body, other, fixture, otherFixture);
        RaiseLocalEvent(body.Owner, ref preventCollideMessage);

        if (preventCollideMessage.Cancelled) return false;

        preventCollideMessage = new PreventCollideEvent(other, body, otherFixture, fixture);
        RaiseLocalEvent(other.Owner, ref preventCollideMessage);

        if (preventCollideMessage.Cancelled) return false;

        return true;
    }
}

internal enum ContactStatus : byte
{
    NoContact = 0,
    StartTouching = 1,
    Touching = 2,
    EndTouching = 3,
}
