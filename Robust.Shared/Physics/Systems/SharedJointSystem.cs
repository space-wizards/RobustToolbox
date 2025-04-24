using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedJointSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<JointComponent> _jointsQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<JointRelayTargetComponent> _relayQuery;

    // To avoid issues with component states we'll queue up all dirty joints and check it every tick to see if
    // we can delete the component.
    private readonly HashSet<Entity<JointComponent>> _dirtyJoints = new();
    protected readonly HashSet<Joint> AddedJoints = new();
    protected readonly List<Joint> ToRemove = new();

    public override void Initialize()
    {
        base.Initialize();

        _jointsQuery = GetEntityQuery<JointComponent>();
        _relayQuery = GetEntityQuery<JointRelayTargetComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        UpdatesOutsidePrediction = true;

        UpdatesBefore.Add(typeof(SharedPhysicsSystem));
        SubscribeLocalEvent<JointComponent, ComponentShutdown>(OnJointShutdown);
        SubscribeLocalEvent<JointComponent, ComponentInit>(OnJointInit);

        InitializeRelay();
    }

    #region Lifetime

    private void OnJointInit(EntityUid uid, JointComponent component, ComponentInit args)
    {
        foreach (var (id, joint) in component.Joints)
        {
            var other = uid == joint.BodyAUid ? joint.BodyBUid : joint.BodyAUid;

            // Client may not yet know about the other entity.
            // But whenever that other entity enters PVS, its own joint initialization should hopefully run this again anyways.
            if (!TryComp(joint.BodyAUid, out PhysicsComponent? bodyA) || !TryComp(joint.BodyBUid, out PhysicsComponent? bodyB) || !TryComp(other, out JointComponent? otherComp))
                continue;

            if (!otherComp.Joints.ContainsKey(id))
            {
                // This can happen if the other joint handled its state before this entity was initialized. In this
                // case we need to re-add the joint to the other entity.
                if (uid == joint.BodyAUid)
                    InitJoint(joint, bodyA, bodyB, component, otherComp, ignoreExisting: true);
                else
                    InitJoint(joint, bodyA, bodyB, otherComp, component, ignoreExisting: true);
                continue;
            }

            _physics.WakeBody(joint.BodyAUid, body: bodyA);
            _physics.WakeBody(joint.BodyBUid, body: bodyB);

            // Raise broadcast last so we can do both sides of directed first.
            var vera = new JointAddedEvent(joint, joint.BodyAUid, joint.BodyBUid, bodyA, bodyB);
            RaiseLocalEvent(joint.BodyAUid, vera);
            var smug = new JointAddedEvent(joint, joint.BodyBUid, joint.BodyAUid, bodyB, bodyA);
            RaiseLocalEvent(joint.BodyBUid, smug);
            EntityManager.EventBus.RaiseEvent(EventSource.Local, vera);
        }

        RefreshRelay(uid, component);
    }

    private void OnJointShutdown(EntityUid uid, JointComponent component, ComponentShutdown args)
    {
        foreach (var joint in component.Joints.Values)
        {
            RemoveJoint(joint);
        }

        // If we're relaying elsewhere then cleanup our old data.
        if (component.Relay != null && !TerminatingOrDeleted(component.Relay.Value))
            SetRelay(uid, null, component);
    }

    #endregion

    public void SetEnabled(Joint joint, bool value)
    {
        if (joint.Enabled == value)
            return;

        joint.Enabled = value;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var joint in AddedJoints)
        {
            InitJoint(joint);
        }

        AddedJoints.Clear();

        foreach (var joint in _dirtyJoints)
        {
            if (joint.Comp.Deleted || joint.Comp.JointCount != 0) continue;
            EntityManager.RemoveComponent<JointComponent>(joint);
        }

        _dirtyJoints.Clear();
    }

    private void InitJoint(Joint joint,
        PhysicsComponent? bodyA = null,
        PhysicsComponent? bodyB = null,
        JointComponent? jointComponentA = null,
        JointComponent? jointComponentB = null,
        bool ignoreExisting = false)
    {
        var aUid = joint.BodyAUid;
        var bUid = joint.BodyBUid;

        if (!_physicsQuery.Resolve(aUid, ref bodyA, false) || !_physicsQuery.Resolve(bUid, ref bodyB, false))
            return;

        DebugTools.Assert(Transform(aUid).MapID == Transform(bUid).MapID, "Attempted to initialize cross-map joint");

        jointComponentA ??= EnsureComp<JointComponent>(aUid);
        jointComponentB ??= EnsureComp<JointComponent>(bUid);
        DebugTools.AssertOwner(aUid, jointComponentA);
        DebugTools.AssertOwner(bUid, jointComponentB);
        DebugTools.AssertNotEqual(jointComponentA.Relay, bUid);
        DebugTools.AssertNotEqual(jointComponentB.Relay, aUid);

        var jointsA = jointComponentA.Joints;
        var jointsB = jointComponentB.Joints;

        if (_gameTiming.IsFirstTimePredicted)
            Log.Debug($"Initializing joint {joint.ID}");

        // Check for existing joints
        if (!ignoreExisting && jointsA.TryGetValue(joint.ID, out var existing))
        {
            if (existing.BodyBUid != bUid)
            {
                Log.Error($"While adding joint {joint.ID} to entity {ToPrettyString(bUid)}, the connected entity {ToPrettyString(aUid)} already had a joint with the same ID connected to another entity {ToPrettyString(existing.BodyBUid)}.");
                return;
            }

            // If they both already have it we should be gucci
            // This can occur because of client states coming in blah blah
            // The reason for this is we defer everything until Update
            // (and the reason we defer is to avoid modifying components during iteration when we do the EnsureComponent)
            if (jointsB.TryGetValue(joint.ID, out var value))
            {
                DebugTools.Assert(value.BodyAUid == aUid);
                return;
            }

            Log.Error($"While adding joint {joint.ID} to entity {ToPrettyString(bUid)}, the joint already existed for the connected entity {ToPrettyString(aUid)}.");
        }
        else if (!ignoreExisting && jointsB.TryGetValue(joint.ID, out existing))
        {
            if (existing.BodyAUid != aUid)
            {
                Log.Error($"While adding joint {joint.ID} to entity {ToPrettyString(aUid)}, the connected entity {ToPrettyString(bUid)} already had a joint with the same ID connected to another entity {ToPrettyString(existing.BodyAUid)}.");
                return;
            }

            Log.Error($"While adding joint {joint.ID} to entity {ToPrettyString(aUid)}, the joint already existed for the connected entity {ToPrettyString(bUid)}.");
        }

        jointsA.TryAdd(joint.ID, joint);
        jointsB.TryAdd(joint.ID, joint);

        // If the joint prevents collisions, then flag any contacts for filtering.
        if (!joint.CollideConnected)
        {
            FilterContactsForJoint(joint, bodyA, bodyB);
        }

        // TODO reduce metadata resolves.
        _physics.WakeBody(aUid, body: bodyA);
        _physics.WakeBody(bUid, body: bodyB);
        Dirty(aUid, bodyA);
        Dirty(bUid, bodyB);
        Dirty(aUid, jointComponentA);
        Dirty(bUid, jointComponentB);

        // Also flag these for checking juusssttt in case.
        _dirtyJoints.Add((aUid, jointComponentA));
        _dirtyJoints.Add((bUid, jointComponentB));
        // Note: creating a joint doesn't wake the bodies.

        // Raise broadcast last so we can do both sides of directed first.
        var vera = new JointAddedEvent(joint, aUid, bUid, bodyA, bodyB);
        EntityManager.EventBus.RaiseLocalEvent(aUid, vera);
        var smug = new JointAddedEvent(joint, bUid, aUid, bodyB, bodyA);
        EntityManager.EventBus.RaiseLocalEvent(bUid, smug);
        EntityManager.EventBus.RaiseEvent(EventSource.Local, vera);
    }

    private static string GetJointId(Joint joint)
    {
        var id = joint.ID;
        return !string.IsNullOrEmpty(id) ? id : joint.GetHashCode().ToString();
    }

    #region Helpers

    /// <summary>
    /// Create a DistanceJoint between 2 bodies. This should be called content-side whenever you need one.
    /// </summary>
    public DistanceJoint CreateDistanceJoint(
        EntityUid bodyA,
        EntityUid bodyB,
        Vector2? anchorA = null,
        Vector2? anchorB = null,
        string? id = null,
        TransformComponent? xformA = null,
        TransformComponent? xformB = null,
        float? minimumDistance = null)
    {
        if (!Resolve(bodyA, ref xformA) || !Resolve(bodyB, ref xformB))
        {
            throw new InvalidOperationException();
        }

        anchorA ??= Vector2.Zero;
        anchorB ??= Vector2.Zero;

        var vecA = Vector2.Transform(anchorA.Value, _transform.GetWorldMatrix(xformA));
        var vecB = Vector2.Transform(anchorB.Value, _transform.GetWorldMatrix(xformB));
        var length = (vecA - vecB).Length();
        if (minimumDistance != null)
            length = Math.Max(minimumDistance.Value, length);

        var joint = new DistanceJoint(bodyA, bodyB, anchorA.Value, anchorB.Value, length);
        id ??= GetJointId(joint);
        joint.ID = id;
        AddJoint(joint);

        return joint;
    }

    /// <summary>
    /// Create a MouseJoint between 2 bodies. This should be called content-side whenever you need one.
    /// </summary>
    public MouseJoint CreateMouseJoint(EntityUid bodyA, EntityUid bodyB, Vector2? anchorA = null, Vector2? anchorB = null, string? id = null)
    {
        anchorA ??= Vector2.Zero;
        anchorB ??= Vector2.Zero;

        var joint = new MouseJoint(bodyA, bodyB, anchorA.Value, anchorB.Value);
        id ??= GetJointId(joint);
        joint.ID = id;
        AddJoint(joint);

        return joint;
    }

    public PrismaticJoint CreatePrismaticJoint(EntityUid bodyA, EntityUid bodyB, string? id = null)
    {
        var joint = new PrismaticJoint(bodyA, bodyB);
        id ??= GetJointId(joint);
        joint.ID = id;
        AddJoint(joint);

        return joint;
    }

    public PrismaticJoint CreatePrismaticJoint(
        EntityUid bodyA,
        EntityUid bodyB,
        Vector2 anchorA,
        Vector2 anchorB,
        Vector2 worldAxis,
        float referenceAngle,
        string? id = null)
    {
        var axis = GetLocalVector2(bodyA, worldAxis);
        var joint = new PrismaticJoint(bodyA, bodyB, anchorA, anchorB, axis, referenceAngle);
        id ??= GetJointId(joint);
        joint.ID = id;
        AddJoint(joint);

        return joint;
    }

    public RevoluteJoint CreateRevoluteJoint(EntityUid bodyA, EntityUid bodyB, string? id = null)
    {
        var joint = new RevoluteJoint(bodyA, bodyB);
        id ??= GetJointId(joint);
        joint.ID = id;
        AddJoint(joint);

        return joint;
    }

    public WeldJoint GetOrCreateWeldJoint(EntityUid bodyA, EntityUid bodyB, string? id = null)
    {
        if (id != null &&
            _jointsQuery.TryComp(bodyA, out JointComponent? jointComponent) &&
            jointComponent.Joints.TryGetValue(id, out var weldJoint))
        {
            return (WeldJoint) weldJoint;
        }

        var joint = new WeldJoint(bodyA, bodyB);
        id ??= GetJointId(joint);
        joint.ID = id;
        AddJoint(joint);

        return joint;
    }

    public WeldJoint CreateWeldJoint(EntityUid bodyA, EntityUid bodyB, string? id = null)
    {
        var joint = new WeldJoint(bodyA, bodyB);
        id ??= GetJointId(joint);
        joint.ID = id;
        AddJoint(joint);

        return joint;
    }

    private Vector2 GetLocalVector2(EntityUid uid, Vector2 worldVector, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return Vector2.Zero;

        return Physics.Transform.MulT(new Quaternion2D((float) _transform.GetWorldRotation(xform).Theta), worldVector);
    }

    #endregion

    public static void LinearStiffness(
        float frequencyHertz,
        float dampingRatio,
        float massA,
        float massB,
        out float stiffness, out float damping)
    {
        float mass;
        if (massA > 0.0f && massB > 0.0f)
        {
            mass = massA * massB / (massA + massB);
        }
        else if (massA > 0.0f)
        {
            mass = massA;
        }
        else
        {
            mass = massB;
        }

        var omega = 2.0f * MathF.PI * frequencyHertz;
        stiffness = mass * omega * omega;
        damping = 2.0f * mass * dampingRatio * omega;
    }

    public static void AngularStiffness(
        float frequencyHertz,
        float dampingRatio,
        PhysicsComponent bodyA,
        PhysicsComponent bodyB,
        out float stiffness, out float damping)
    {
        var IA = bodyA.Inertia;
        var IB = bodyB.Inertia;

        float I;
        if (IA > 0.0f && IB > 0.0f)
        {
            I = IA * IB / (IA + IB);
        }
        else if (IA > 0.0f)
        {
            I = IA;
        }
        else
        {
            I = IB;
        }

        float omega = 2.0f * MathF.PI * frequencyHertz;
        stiffness = I * omega * omega;
        damping = 2.0f * I * dampingRatio * omega;
    }

    #region Joints

    protected void AddJoint(Joint joint, PhysicsComponent? bodyA = null, PhysicsComponent? bodyB = null)
    {
        if (!_physicsQuery.Resolve(joint.BodyAUid, ref bodyA) || !_physicsQuery.Resolve(joint.BodyBUid, ref bodyB))
            return;

        if (!joint.CollideConnected)
            FilterContactsForJoint(joint, bodyA, bodyB);

        // Maybe make this method AddOrUpdate so we can have an Add one that explicitly throws if present?
        var mapidA = Transform(joint.BodyAUid).MapID;

        if (mapidA == MapId.Nullspace ||
            mapidA != Transform(joint.BodyBUid).MapID)
        {
            Log.Error($"Tried to add joint to ineligible bodies");
            return;
        }

        if (string.IsNullOrEmpty(joint.ID))
        {
            Log.Error($"Can't add a joint with no ID");
            DebugTools.Assert($"Can't add a joint with no ID");
            return;
        }

        InitJoint(joint, bodyA, bodyB);

        if (_gameTiming.IsFirstTimePredicted)
        {
            Log.Debug($"Added {joint.JointType} Joint with ID {joint.ID} from {bodyA.BodyType} to {bodyB.BodyType} ");
        }
    }

    /// <summary>
    /// Removes joints on this entity and anything relaying to it.
    /// </summary>
    public void RecursiveClearJoints(
        EntityUid uid,
        TransformComponent? xform = null,
        JointComponent? component = null,
        JointRelayTargetComponent? relay = null)
    {
        if (!Resolve(uid, ref xform))
            return;

        _jointsQuery.Resolve(uid, ref component, false);
        _relayQuery.Resolve(uid, ref relay, false);

        if (relay != null)
        {
            foreach (var ree in relay.Relayed)
            {
                _jointsQuery.TryGetComponent(ree, out var rJoint);
                ClearJoints(ree, rJoint);
            }

            RemComp(uid, relay);
        }

        if (component != null)
        {
            ClearJoints(uid, component);
        }
    }

    /// <summary>
    /// Clears any joints for this entity, excluding relayed joints.
    /// </summary>
    public void ClearJoints(EntityUid uid, JointComponent? component = null)
    {
        if (!_jointsQuery.Resolve(uid, ref component, false))
            return;

        // TODO PERFORMANCE
        // This will re-fetch the joint & body component for this entity ( & ever connected
        // entity), for each and every joint. at the very least, we could pass in the joint & physics comp. As long
        // as most entities only have a single joint, fetching connected components probably isn't worth it.
        foreach (var a in component.Joints.Values.ToArray())
        {
            RemoveJoint(a);
        }

        foreach (var j in AddedJoints)
        {
            if (j.BodyAUid == uid || j.BodyBUid == uid)
                ToRemove.Add(j);
        }
        AddedJoints.ExceptWith(ToRemove);
        ToRemove.Clear();

        if (_gameTiming.IsFirstTimePredicted)
        {
            Log.Debug($"Removed all joints from entity {ToPrettyString(uid)}");
        }
    }

    public void RemoveJoint(EntityUid uid, string id)
    {
        if (!_jointsQuery.TryComp(uid, out var jointComp))
            return;

        if (!jointComp.Joints.TryGetValue(id, out var joint))
            return;

        RemoveJoint(joint);
    }

    public void RemoveJoint(Joint joint)
    {
        AddedJoints.Remove(joint);
        var bodyAUid = joint.BodyAUid;
        var bodyBUid = joint.BodyBUid;

        // Originally I logged these but because of prediction the client can just nuke them multiple times in a row
        // because each body has its own JointComponent, bleh.
        if (!_jointsQuery.TryComp(bodyAUid, out var jointComponentA))
        {
            return;
        }

        if (!_jointsQuery.TryComp(bodyBUid, out var jointComponentB))
        {
            return;
        }

        if (!jointComponentA.Joints.Remove(joint.ID))
        {
            return;
        }

        if (!jointComponentB.Joints.Remove(joint.ID))
        {
            return;
        }

        // Wake up connected bodies.
        if (_physicsQuery.TryComp(bodyAUid, out var bodyA) &&
            MetaData(bodyAUid).EntityLifeStage < EntityLifeStage.Terminating)
        {
            var uidA = jointComponentA.Relay ?? bodyAUid;
            _physics.WakeBody(uidA);
        }

        if (EntityManager.TryGetComponent<PhysicsComponent>(bodyBUid, out var bodyB) &&
            MetaData(bodyBUid).EntityLifeStage < EntityLifeStage.Terminating)
        {
            var uidB = jointComponentB.Relay ?? bodyBUid;
            _physics.WakeBody(uidB);
        }

        if (!jointComponentA.Deleted)
        {
            Dirty(bodyAUid, jointComponentA);
        }

        if (!jointComponentB.Deleted)
        {
            Dirty(bodyBUid, jointComponentB);
        }

        if (jointComponentA.Deleted && jointComponentB.Deleted)
            return;

        // If the joint prevents collisions, then flag any contacts for filtering.
        if (!joint.CollideConnected)
        {
            FilterContactsForJoint(joint);
        }

        if (bodyA == null)
        {
            Log.Debug($"Tried to remove joint from entity {ToPrettyString(bodyAUid)} without a physics component");
        }
        else if (bodyB == null)
        {
            Log.Debug($"Tried to remove joint from entity {ToPrettyString(bodyBUid)} without a physics component");
        }
        else
        {
            var vera = new JointRemovedEvent(joint, bodyAUid, bodyBUid, bodyA, bodyB);
            EntityManager.EventBus.RaiseLocalEvent(bodyAUid, vera, false);
            var smug = new JointRemovedEvent(joint, bodyBUid, bodyAUid, bodyB, bodyA);
            EntityManager.EventBus.RaiseLocalEvent(bodyBUid, smug, false);
            EntityManager.EventBus.RaiseEvent(EventSource.Local, vera);

            if (_gameTiming.IsFirstTimePredicted)
            {
                Log.Debug($"Removed {joint.JointType} joint with ID {joint.ID} from entity {ToPrettyString(bodyAUid)} to entity {ToPrettyString(bodyBUid)}");
            }
        }

        // We can't just check up front due to how prediction works.
        _dirtyJoints.Add((bodyAUid, jointComponentA));
        _dirtyJoints.Add((bodyBUid, jointComponentB));
    }

    #endregion

    internal void FilterContactsForJoint(Joint joint, PhysicsComponent? bodyA = null, PhysicsComponent? bodyB = null)
    {
        if (!_physicsQuery.Resolve(joint.BodyBUid, ref bodyB))
            return;

        var node = bodyB.Contacts.First;

        while (node != null)
        {
            var contact = node.Value;
            node = node.Next;

            if (contact.EntityA == joint.BodyAUid ||
                contact.EntityB == joint.BodyAUid)
            {
                // Flag the contact for filtering at the next time step (where either
                // body is awake).
                contact.Flags |= ContactFlags.Filter;
            }
        }
    }
}
