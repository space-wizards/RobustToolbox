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
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using JetBrains.Annotations;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Events;
using Robust.Shared.Threading;
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
            contact.Manifold = new Manifold();

            return contact;
        }

        public bool Return(Contact obj)
        {
            DebugTools.Assert(obj.Flags is ContactFlags.None or ContactFlags.Deleted);
            SetContact(obj,
                false,
                EntityUid.Invalid, EntityUid.Invalid,
                string.Empty, string.Empty,
                null, 0,
                null, 0,
                null, null);
            return true;
        }
    }

    private static void SetContact(Contact contact,
        bool enabled,
        EntityUid uidA, EntityUid uidB,
        string fixtureAId, string fixtureBId,
        Fixture? fixtureA, int indexA,
        Fixture? fixtureB, int indexB,
        PhysicsComponent? bodyA,
        PhysicsComponent? bodyB)
    {
        contact.Enabled = enabled;
        contact.IsTouching = false;
        DebugTools.Assert(contact.Flags is ContactFlags.None or ContactFlags.PreInit or ContactFlags.Deleted);
        // TOIFlag = false;

        contact.EntityA = uidA;
        contact.EntityB = uidB;

        contact.FixtureAId = fixtureAId;
        contact.FixtureBId = fixtureBId;

        contact.FixtureA = fixtureA;
        contact.FixtureB = fixtureB;

        contact.BodyA = bodyA;
        contact.BodyB = bodyB;

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
        EntityManager.EntityQueueDeleted += OnContactEntityQueueDel;
    }

    private void ShutdownContacts()
    {
        EntityManager.EntityQueueDeleted -= OnContactEntityQueueDel;
    }

    private void OnContactEntityQueueDel(EntityUid obj)
    {
        // If an entity is queuedeleted then we want to purge its contacts before SimulateWorld runs in the same tick.
        if (!TryComp<PhysicsComponent>(obj, out var physicsComp))
            return;

        DestroyContacts(physicsComp);
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

    private Contact CreateContact(
        EntityUid uidA, EntityUid uidB,
        string fixtureAId, string fixtureBId,
        Fixture fixtureA, int indexA,
        Fixture fixtureB, int indexB,
        PhysicsComponent bodyA, PhysicsComponent bodyB)
    {
        var type1 = fixtureA.Shape.ShapeType;
        var type2 = fixtureB.Shape.ShapeType;

        DebugTools.Assert(ShapeType.Unknown < type1 && type1 < ShapeType.TypeCount);
        DebugTools.Assert(ShapeType.Unknown < type2 && type2 < ShapeType.TypeCount);

        // Pull out a spare contact object
        var contact = _contactPool.Get();
        DebugTools.Assert(contact.Flags is ContactFlags.None or ContactFlags.Deleted);
        contact.Flags = ContactFlags.PreInit;

        // Edge+Polygon is non-symmetrical due to the way Erin handles collision type registration.
        if ((type1 >= type2 || (type1 == ShapeType.Edge && type2 == ShapeType.Polygon)) && !(type2 == ShapeType.Edge && type1 == ShapeType.Polygon))
        {
            SetContact(contact, true, uidA, uidB, fixtureAId, fixtureBId, fixtureA, indexA, fixtureB, indexB, bodyA, bodyB);
        }
        else
        {
            SetContact(contact, true, uidB, uidA, fixtureBId, fixtureAId, fixtureB, indexB, fixtureA, indexA, bodyB, bodyA);
        }

        contact.Type = _registers[(int)type1, (int)type2];

        return contact;
    }

    /// <summary>
    /// Try to create a contact between these 2 fixtures.
    /// </summary>
    internal void AddPair(
        EntityUid uidA, EntityUid uidB,
        string fixtureAId, string fixtureBId,
        Fixture fixtureA, int indexA,
        Fixture fixtureB, int indexB,
        PhysicsComponent bodyA, PhysicsComponent bodyB,
        ContactFlags flags = ContactFlags.None)
    {
        // Broadphase has already done the faster check for collision mask / layers
        // so no point duplicating

        // Does a contact already exist?
        if (fixtureA.Contacts.ContainsKey(fixtureB))
            return;

        DebugTools.Assert(!fixtureB.Contacts.ContainsKey(fixtureA));

        var xformA = _xformQuery.GetComponent(uidA);
        var xformB = _xformQuery.GetComponent(uidB);

        // Does a joint override collision? Is at least one body dynamic?
        if (!ShouldCollide(uidA, uidB, bodyA, bodyB, fixtureA, fixtureB, xformA, xformB))
            return;

        // Call the factory.
        var contact = CreateContact(uidA, uidB, fixtureAId, fixtureBId, fixtureA, indexA, fixtureB, indexB, bodyA, bodyB);
        contact.Flags = flags;

        // Contact creation may swap fixtures.
        var fixA = contact.FixtureA!;
        var fixB = contact.FixtureB!;
        var bodA = contact.BodyA!;
        var bodB = contact.BodyB!;

        // Insert into world
        _activeContacts.AddLast(contact.MapNode);

        // Connect to body A
        DebugTools.Assert(!fixA.Contacts.ContainsKey(fixB));
        fixA.Contacts.Add(fixB, contact);
        bodA.Contacts.AddLast(contact.BodyANode);

        // Connect to body B
        DebugTools.Assert(!fixB.Contacts.ContainsKey(fixA));
        fixB.Contacts.Add(fixA, contact);
        bodB.Contacts.AddLast(contact.BodyBNode);
    }

    /// <summary>
    ///     Go through the cached broadphase movement and update contacts.
    /// </summary>
    internal void AddPair(string fixtureAId, string fixtureBId, in FixtureProxy proxyA, in FixtureProxy proxyB)
    {
        AddPair(proxyA.Entity, proxyB.Entity,
            fixtureAId, fixtureBId,
            proxyA.Fixture, proxyA.ChildIndex,
            proxyB.Fixture, proxyB.ChildIndex,
            proxyA.Body, proxyB.Body);
    }

    internal static bool ShouldCollide(Fixture fixtureA, Fixture fixtureB)
    {
        return !((fixtureA.CollisionMask & fixtureB.CollisionLayer) == 0x0 &&
                 (fixtureB.CollisionMask & fixtureA.CollisionLayer) == 0x0);
    }

    public void DestroyContact(Contact contact)
    {
        DestroyContact(contact, null, out _);
    }

    internal void DestroyContact(Contact contact, LinkedListNode<Contact>? node, out LinkedListNode<Contact>? next)
    {
        // EndCollideEvent may cause knock on effects that cause contacts to be destroyed.
        // This check prevents us from trying to destroy a contact that is already being, or already has been, destroyed.
        if ((contact.Flags & (ContactFlags.Deleting | ContactFlags.Deleted)) != 0x0)
        {
            next = node?.Next;
            return;
        }

        DebugTools.Assert((contact.Flags & ContactFlags.PreInit) == 0);
        // Contact flag might be None here as CollideContacts() might destroy the contact after having removed the PreInit flag

        var fixtureA = contact.FixtureA!;
        var fixtureB = contact.FixtureB!;
        var bodyA = contact.BodyA!;
        var bodyB = contact.BodyB!;
        var aUid = contact.EntityA;
        var bUid = contact.EntityB;
        contact.Flags |= ContactFlags.Deleting;

        if (contact.IsTouching)
        {
            var ev1 = new EndCollideEvent(aUid, bUid, contact.FixtureAId, contact.FixtureBId, fixtureA, fixtureB, bodyA, bodyB);
            var ev2 = new EndCollideEvent(bUid, aUid, contact.FixtureBId, contact.FixtureAId, fixtureB, fixtureA, bodyB, bodyA);
            RaiseLocalEvent(aUid, ref ev1);
            RaiseLocalEvent(bUid, ref ev2);
        }

        if (contact.Manifold.PointCount > 0 && contact.FixtureA?.Hard == true && contact.FixtureB?.Hard == true)
        {
            if (bodyA.CanCollide)
                SetAwake((aUid, bodyA), true);

            if (bodyB.CanCollide)
                SetAwake((bUid, bodyB), true);
        }

        // Fetch next node AFTER all event raising has finished.
        // This ensures that we actually get the next node in case the linked list was modified by the events that were
        // raised
        next = node?.Next;

        // Remove from the world
        _activeContacts.Remove(contact.MapNode);

        // Remove from body 1
        DebugTools.Assert(fixtureA.Contacts.ContainsKey(fixtureB));
        fixtureA.Contacts.Remove(fixtureB);
        DebugTools.Assert(bodyA.Contacts.Contains(contact.BodyANode.Value));
        bodyA.Contacts.Remove(contact.BodyANode);

        // Remove from body 2
        DebugTools.Assert(fixtureB.Contacts.ContainsKey(fixtureA));
        fixtureB.Contacts.Remove(fixtureA);
        DebugTools.Assert(bodyB.Contacts.Contains(contact.BodyBNode.Value));
        bodyB.Contacts.Remove(contact.BodyBNode);

        contact.Flags = ContactFlags.Deleted;
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

        while (node != null)
        {
            var contact = node.Value;
            node = node.Next;

            // It's possible the contact was destroyed by content in which case we just skip it.
            if (!contact.Enabled)
                continue;

            // No longer pre-init and can be used in the solver.
            contact.Flags &= ~ContactFlags.PreInit;
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

            var xformA = _xformQuery.GetComponent(uidA);
            var xformB = _xformQuery.GetComponent(uidB);

            if (xformA.MapID == MapId.Nullspace || xformB.MapID == MapId.Nullspace)
            {
                DestroyContact(contact);
                continue;
            }

            // Is this contact flagged for filtering?
            if ((contact.Flags & ContactFlags.Filter) != 0x0)
            {
                // Check default filtering
                if (!ShouldCollide(fixtureA, fixtureB) ||
                    !ShouldCollide(uidA, uidB, bodyA, bodyB, fixtureA, fixtureB, xformA, xformB))
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


            if (xformA.MapUid == null || xformA.MapUid != xformB.MapUid)
            {
                DestroyContact(contact);
                continue;
            }

            // Special-case grid contacts.
            if ((contact.Flags & ContactFlags.Grid) != 0x0)
            {
                var gridABounds = fixtureA.Shape.ComputeAABB(GetPhysicsTransform(uidA, xformA), 0);
                var gridBBounds = fixtureB.Shape.ComputeAABB(GetPhysicsTransform(uidB, xformB), 0);

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
                        Log.Error($"Insufficient contact length at 388! Index {index} and length is {contacts.Length}. Tell Sloth");
                    }
                    contacts[index++] = contact;
                }

                continue;
            }

            if (indexA >= fixtureA.Proxies.Length)
            {
                Log.Error($"Found invalid contact index of {indexA} on {contact.FixtureAId} / {ToPrettyString(uidA)}, expected {fixtureA.Proxies.Length}");
                DestroyContact(contact);
                continue;
            }

            if (indexB >= fixtureB.Proxies.Length)
            {
                Log.Error($"Found invalid contact index of {indexB} on {contact.FixtureBId} / {ToPrettyString(uidB)}, expected {fixtureB.Proxies.Length}");
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
                    // TODO maybe change this? Needs benchmarking.
                    // Instead of transforming both boxes (which enlarges both aabbs), maybe just transform one box.
                    // I.e. use (matrixA * invMatrixB).TransformBox(). Or (invMatrixB * matrixA), whichever is correct.
                    // Alternatively, maybe just directly construct the relative transform matrix?
                    var proxyAWorldAABB = _transform.GetWorldMatrix(_xformQuery.GetComponent(broadphaseA.Value), _xformQuery).TransformBox(proxyA.AABB);
                    var proxyBWorldAABB = _transform.GetWorldMatrix(_xformQuery.GetComponent(broadphaseB.Value), _xformQuery).TransformBox(proxyB.AABB);
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
                Log.Error($"Insufficient contact length at 429! Index {index} and length is {contacts.Length}. Tell Sloth");
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
                Log.Error($"Invalid contact length for contact events!");
                continue;
            }

            var contact = contacts[i];

            // It's possible the contact was disabled above if DestroyContact lead to even more being destroyed.
            if (!contact.Enabled)
            {
                continue;
            }

            switch (status[i])
            {
                case ContactStatus.StartTouching:
                {
                    if (!contact.IsTouching) continue;

                    var fixtureA = contact.FixtureA!;
                    var fixtureB = contact.FixtureB!;
                    var bodyA = contact.BodyA!;
                    var bodyB = contact.BodyB!;
                    var uidA = contact.EntityA;
                    var uidB = contact.EntityB;
                    var worldPoint = worldPoints[i];

                    var ev1 = new StartCollideEvent(uidA, uidB, contact.FixtureAId, contact.FixtureBId, fixtureA, fixtureB, bodyA, bodyB, worldPoint);
                    var ev2 = new StartCollideEvent(uidB, uidA, contact.FixtureBId, contact.FixtureAId, fixtureB, fixtureA, bodyB, bodyA, worldPoint);

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

                    var ev1 = new EndCollideEvent(uidA, uidB, contact.FixtureAId, contact.FixtureBId, fixtureA, fixtureB, bodyA, bodyB);
                    var ev2 = new EndCollideEvent(uidB, uidA, contact.FixtureBId, contact.FixtureAId, fixtureB, fixtureA, bodyB, bodyA);

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

    private record struct UpdateTreesJob : IRobustJob
    {
        public IEntityManager EntManager;

        public void Execute()
        {
            var query = EntManager.AllEntityQueryEnumerator<BroadphaseComponent>();

            while (query.MoveNext(out var broadphase))
            {
                broadphase.DynamicTree.Rebuild(false);
                broadphase.StaticTree.Rebuild(false);
                broadphase.SundriesTree._b2Tree.Rebuild(false);
                broadphase.StaticSundriesTree._b2Tree.Rebuild(false);
            }
        }
    }

    private void BuildManifolds(Contact[] contacts, int count, ContactStatus[] status, Vector2[] worldPoints)
    {
        if (count == 0)
            return;

        var wake = ArrayPool<bool>.Shared.Rent(count);

        _parallel.ProcessNow(new ManifoldsJob()
        {
            Physics = this,
            Status = status,
            WorldPoints = worldPoints,
            Contacts = contacts,
            Wake = wake,
        }, count);

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

            SetAwake((aUid, bodyA), true);
            SetAwake((bUid, bodyB), true);
        }

        ArrayPool<bool>.Shared.Return(wake);
    }

    private record struct ManifoldsJob : IParallelRobustJob
    {
        public int BatchSize => ContactsPerThread;

        public SharedPhysicsSystem Physics;

        public Contact[] Contacts;
        public ContactStatus[] Status;
        public Vector2[] WorldPoints;
        public bool[] Wake;

        public void Execute(int index)
        {
            Physics.UpdateContact(Contacts, index, Status, Wake, WorldPoints);
        }
    }

    private void UpdateContact(Contact[] contacts, int index, ContactStatus[] status, bool[] wake, Vector2[] worldPoints)
    {
        var contact = contacts[index];

        // TODO: Temporary measure. When Box2D 3.0 comes out expect a major refactor
        // of everything
        // It's okay past sloth it can't hurt you anymore.
        // This can happen if DestroyContact is called and content deletes contacts that were already processed.
        if (!contact.Enabled)
        {
            status[index] = ContactStatus.NoContact;
            wake[index] = false;
            return;
        }

        var uidA = contact.EntityA;
        var uidB = contact.EntityB;
        var bodyATransform = GetPhysicsTransform(uidA);
        var bodyBTransform = GetPhysicsTransform(uidB);

        var contactStatus = contact.Update(bodyATransform, bodyBTransform, out wake[index]);
        status[index] = contactStatus;

        if (contactStatus == ContactStatus.StartTouching)
        {
            worldPoints[index] = Physics.Transform.Mul(bodyATransform, contacts[index].Manifold.LocalPoint);
        }
    }

    /// <summary>
    ///     Used to prevent bodies from colliding; may lie depending on joints.
    /// </summary>
    protected bool ShouldCollide(
        EntityUid uid,
        EntityUid other,
        PhysicsComponent body,
        PhysicsComponent otherBody,
        Fixture fixture,
        Fixture otherFixture,
        TransformComponent xform,
        TransformComponent otherXform)
    {
        if (((body.BodyType & (BodyType.Kinematic | BodyType.Static)) != 0 &&
             (otherBody.BodyType & (BodyType.Kinematic | BodyType.Static)) != 0) ||
            // Kinematic controllers can't collide.
            (fixture.Hard && body.BodyType == BodyType.KinematicController &&
             otherFixture.Hard && otherBody.BodyType == BodyType.KinematicController))
        {
            return false;
        }

        if (fixture.Hard && otherFixture.Hard)
        {
            // Prevent self-propelling entities. I.e., prevent a fixture on a static child entity from propelling the
            // parent forwards.
            // TODO Add recursive parent checks, without somehow killing performance.
            if (uid == other)
                return false;

            if (other == xform.ParentUid && body.BodyType == BodyType.Static)
                return false;

            if (uid == otherXform.ParentUid && otherBody.BodyType == BodyType.Static)
                return false;
        }

        // Does a joint prevent collision?
        // if one of them doesn't have jointcomp then they can't share a common joint.
        // otherwise, only need to iterate over the joints of one component as they both store the same joint.
        if (TryComp(uid, out JointComponent? jointComponentA) && HasComp<JointComponent>(other))
        {
            foreach (var joint in jointComponentA.Joints.Values)
            {
                // Check if either: the joint even allows collisions OR the other body on the joint is actually the other body we're checking.
                if (!joint.CollideConnected && (other == joint.BodyAUid || other == joint.BodyBUid))
                    return false;
            }
        }

        var preventCollideMessage = new PreventCollideEvent(uid, other, body, otherBody, fixture, otherFixture);
        RaiseLocalEvent(uid, ref preventCollideMessage);

        if (preventCollideMessage.Cancelled)
            return false;

        preventCollideMessage = new PreventCollideEvent(other, uid, otherBody, body, otherFixture, fixture);
        RaiseLocalEvent(other, ref preventCollideMessage);

        if (preventCollideMessage.Cancelled)
            return false;

        return true;
    }

    /// <summary>
    /// Will destroy all contacts and queue for rebuild.
    /// Useful if you have one that may no longer be relevant and don't want to destroy it directly.
    /// </summary>
    public void RegenerateContacts(Entity<PhysicsComponent?> entity)
    {
        if (!PhysicsQuery.Resolve(entity.Owner, ref entity.Comp))
            return;

        _broadphase.RegenerateContacts(entity);
    }

    /// <summary>
    /// Returns the number of touching contacts this entity has.
    /// </summary>
    /// <param name="ignoredFixtureId">Fixture we should ignore if applicable</param>
    [Pure]
    public int GetTouchingContacts(Entity<FixturesComponent?> entity, string? ignoredFixtureId = null)
    {
        if (!_fixturesQuery.Resolve(entity.Owner, ref entity.Comp))
            return 0;

        var count = 0;

        foreach (var (id, fixture) in entity.Comp.Fixtures)
        {
            if (ignoredFixtureId == id)
                continue;

            foreach (var contact in fixture.Contacts.Values)
            {
                if (!contact.IsTouching)
                    continue;

                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Returns all of this entity's contacts.
    /// </summary>
    [Pure]
    public ContactEnumerator GetContacts(Entity<FixturesComponent?> entity, bool includeDeleting = false)
    {
        _fixturesQuery.Resolve(entity.Owner, ref entity.Comp);
        return new ContactEnumerator(entity.Comp, includeDeleting);
    }
}

public record struct ContactEnumerator
{
    public static readonly ContactEnumerator Empty = new(null);

    private Dictionary<string, Fixture>.ValueCollection.Enumerator _fixtureEnumerator;
    private Dictionary<Fixture, Contact>.ValueCollection.Enumerator _contactEnumerator;

    /// <summary>
    /// Also include deleting contacts.
    /// This typically includes the current contact if you're invoking this in the eventbus for an EndCollideEvent.
    /// </summary>
    public bool IncludeDeleting;

    public ContactEnumerator(FixturesComponent? fixtures, bool includeDeleting = false)
    {
        IncludeDeleting = includeDeleting;

        if (fixtures == null || fixtures.Fixtures.Count == 0)
        {
            this = Empty;
            return;
        }

        _fixtureEnumerator = fixtures.Fixtures.Values.GetEnumerator();
        _fixtureEnumerator.MoveNext();
        _contactEnumerator = _fixtureEnumerator.Current.Contacts.Values.GetEnumerator();
    }

    public bool MoveNext([NotNullWhen(true)] out Contact? contact)
    {
        if (!_contactEnumerator.MoveNext())
        {
            if (!_fixtureEnumerator.MoveNext())
            {
                contact = null;
                return false;
            }

            _contactEnumerator = _fixtureEnumerator.Current.Contacts.Values.GetEnumerator();
            return MoveNext(out contact);
        }

        contact = _contactEnumerator.Current;

        if (!IncludeDeleting && contact.Deleting)
            return MoveNext(out contact);

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
