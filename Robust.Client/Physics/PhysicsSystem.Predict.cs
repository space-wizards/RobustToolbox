using System.Buffers;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Client.Physics;

// This partial class contains code related to client-side prediction.
public sealed partial class PhysicsSystem
{
    private HashSet<EntityUid> _toUpdate = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnDetach);
        SubscribeLocalEvent<PhysicsComponent, JointAddedEvent>(OnJointAdded);
        SubscribeLocalEvent<PhysicsComponent, JointRemovedEvent>(OnJointRemoved);
    }

    private void UpdateIsPredicted()
    {
        foreach (var uid in _toUpdate)
        {
            if (!PhysicsQuery.TryGetComponent(uid, out var physics))
                continue;

            var ev = new UpdateIsPredictedEvent(uid);

            RaiseLocalEvent(uid, ref ev, true);
            ev.IsPredicted &= !ev.BlockPrediction;

            if (physics.Predict == ev.IsPredicted)
                continue;

            physics.Predict = ev.IsPredicted;
            if (ev.IsPredicted)
                EnsureComp<PredictedPhysicsComponent>(uid);
            else
                RemComp<PredictedPhysicsComponent>(uid);
        }

        _toUpdate.Clear();
    }

    private void OnJointAdded(EntityUid uid, PhysicsComponent component, JointAddedEvent args)
    {
        UpdateIsPredicted(args.Joint.BodyAUid);
        UpdateIsPredicted(args.Joint.BodyBUid);
    }

    private void OnJointRemoved(EntityUid uid, PhysicsComponent component, JointRemovedEvent args)
    {
        UpdateIsPredicted(args.Joint.BodyAUid);
        UpdateIsPredicted(args.Joint.BodyBUid);
    }

    private void OnAttach(LocalPlayerAttachedEvent ev)
    {
        UpdateIsPredicted(ev.Entity);
    }

    private void OnDetach(LocalPlayerDetachedEvent ev)
    {
        UpdateIsPredicted(ev.Entity);
    }

    public override void UpdateIsPredicted(EntityUid? uid, PhysicsComponent? physics = null)
    {
        if (uid != null)
            _toUpdate.Add(uid.Value);
    }

    internal void ResetContacts()
    {
        // Physics Contacts are not stored in any component state.
        // Unfortunately this means that collision start/stop tends to mis-predict when resetting entity states/
        // E.g., imagine a scenario where we resetting an entity from colliding to non-colliding, and then predicting
        // the start of that same collision. When physics runs, it will just see that contact as a continuation of the
        // existing collision, and will not raise a new collision started event. Therefore, we first need to update
        // existing contacts for predicted entities before performing any actual prediction.

        var contacts = new List<Contact>();
        var maps = new HashSet<EntityUid>();

        var enumerator = AllEntityQuery<PredictedPhysicsComponent, PhysicsComponent, TransformComponent>();
        while (enumerator.MoveNext(out _, out var physics, out var xform))
        {
            DebugTools.Assert(physics.Predict);

            if (xform.MapUid is not { } map)
                continue;

            if (maps.Add(map) && PhysMapQuery.TryGetComponent(map, out var physMap) &&
                MapQuery.TryGetComponent(map, out var mapComp))
                _broadphase.FindNewContacts(physMap, mapComp.MapId);

            contacts.AddRange(physics.Contacts);
        }

        UpdateIsTouching(contacts);
    }

    /// <summary>
    /// This is a stripped down version of <see cref="SharedPhysicsSystem.CollideContacts"/> that exists only to update
    /// <see cref="Contact.IsTouching"/> for client-side prediction.
    /// </summary>
    internal void UpdateIsTouching(List<Contact> toUpdate)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        var contacts = ArrayPool<Contact>.Shared.Rent(toUpdate.Count);
        var index = 0;

        foreach (var contact in toUpdate)
        {
            Fixture fixtureA = contact.FixtureA!;
            Fixture fixtureB = contact.FixtureB!;
            int indexA = contact.ChildIndexA;
            int indexB = contact.ChildIndexB;

            var bodyA = contact.BodyA!;
            var bodyB = contact.BodyB!;
            var uidA = contact.EntityA;
            var uidB = contact.EntityB;

            if (!bodyA.CanCollide || !bodyB.CanCollide)
            {
                contact.IsTouching = false;
                continue;
            }

            var xformA = xformQuery.GetComponent(uidA);
            var xformB = xformQuery.GetComponent(uidB);

            if ((contact.Flags & ContactFlags.Filter) != 0x0)
            {
                if (!ShouldCollide(fixtureA, fixtureB) ||
                    !ShouldCollide(uidA, uidB, bodyA, bodyB, fixtureA, fixtureB, xformA, xformB))
                {
                    contact.IsTouching = false;
                    continue;
                }
            }

            bool activeA = bodyA.Awake && bodyA.BodyType != BodyType.Static;
            bool activeB = bodyB.Awake && bodyB.BodyType != BodyType.Static;

            if (activeA == false && activeB == false)
            {
                continue;
            }

            if (xformA.MapUid == null || xformA.MapUid != xformB.MapUid)
            {
                contact.IsTouching = false;
                continue;
            }

            if (indexA >= fixtureA.Proxies.Length || indexB >= fixtureB.Proxies.Length)
                continue;

            var broadphaseA = xformA.Broadphase?.Uid;
            var broadphaseB = xformB.Broadphase?.Uid;

            if (broadphaseA == null || broadphaseB == null)
            {
                contact.IsTouching = false;
                continue;
            }

            var proxyA = fixtureA.Proxies[indexA];
            var proxyB = fixtureB.Proxies[indexB];
            var overlap = false;

            if (broadphaseA == broadphaseB)
            {
                overlap = proxyA.AABB.Intersects(proxyB.AABB);
            }
            else
            {
                var proxyAWorldAABB = _transform
                    .GetWorldMatrix(xformQuery.GetComponent(broadphaseA.Value), xformQuery)
                    .TransformBox(proxyA.AABB);
                var proxyBWorldAABB = _transform
                    .GetWorldMatrix(xformQuery.GetComponent(broadphaseB.Value), xformQuery)
                    .TransformBox(proxyB.AABB);
                overlap = proxyAWorldAABB.Intersects(proxyBWorldAABB);
            }

            if (overlap)
                contacts[index++] = contact;
            else
                contact.IsTouching = false;
        }

        for (var i = 0; i < index; i++)
        {
            var contact = contacts[i];
            var uidA = contact.EntityA;
            var uidB = contact.EntityB;
            var bodyATransform = GetPhysicsTransform(uidA, xformQuery.GetComponent(uidA));
            var bodyBTransform = GetPhysicsTransform(uidB, xformQuery.GetComponent(uidB));
            contact.UpdateIsTouching(bodyATransform, bodyBTransform);
        }

        ArrayPool<Contact>.Shared.Return(contacts);
    }
}

/// <summary>
/// Event raised to check whether physics prediction should be enabled.
/// </summary>
[ByRefEvent]
public struct UpdateIsPredictedEvent
{
    public readonly EntityUid Uid;

    public bool IsPredicted = false;

    /// <summary>
    /// Can be used to block prediction of entities that would otherwise be predicted.
    /// E.g., if a player is being pulled by a non-predicted entity.
    /// </summary>
    public bool BlockPrediction = false;

    public UpdateIsPredictedEvent(EntityUid uid)
    {
        Uid = uid;
    }
}
