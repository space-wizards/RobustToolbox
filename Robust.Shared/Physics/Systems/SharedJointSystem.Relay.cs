using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedJointSystem
{
    /*
     * Relays joints to the top-most parent so joints inside of containers can still function.
     * This is because we still want the joint to "function" it just needs to affect the parent instead.
     */

    [Serializable, NetSerializable]
    private sealed class JointRelayComponentState : ComponentState
    {
        public HashSet<NetEntity> Entities;

        public JointRelayComponentState(HashSet<NetEntity> entities)
        {
            Entities = entities;
        }
    }

    private void InitializeRelay()
    {
        SubscribeLocalEvent<JointRelayTargetComponent, ComponentShutdown>(OnRelayShutdown);
        SubscribeLocalEvent<JointRelayTargetComponent, ComponentGetState>(OnRelayGetState);
        SubscribeLocalEvent<JointRelayTargetComponent, ComponentHandleState>(OnRelayHandleState);
    }

    private void OnRelayGetState(EntityUid uid, JointRelayTargetComponent component, ref ComponentGetState args)
    {
        args.State = new JointRelayComponentState(GetNetEntitySet(component.Relayed));
    }

    private void OnRelayHandleState(EntityUid uid, JointRelayTargetComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not JointRelayComponentState state)
            return;

        EnsureEntitySet<JointRelayTargetComponent>(state.Entities, uid, component.Relayed);
    }

    private void OnRelayShutdown(EntityUid uid, JointRelayTargetComponent component, ComponentShutdown args)
    {
        if (_gameTiming.ApplyingState)
            return;

        foreach (var relay in component.Relayed)
        {
            if (TerminatingOrDeleted(relay) || !_jointsQuery.TryGetComponent(relay, out var joint))
                continue;

            RefreshRelay(relay, component: joint);
        }
    }

    /// <summary>
    /// Refreshes the joint relay for this entity, prefering its containing container or nothing.
    /// </summary>
    public void RefreshRelay(EntityUid uid, JointComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        EntityUid? relay = null;

        if (_container.TryGetOuterContainer(uid, Transform(uid), out var container))
        {
            relay = container.Owner;

            // Validate that the relay target is not being set to our own container.
            foreach (var joint in component.Joints.Values)
            {
                var other = joint.GetOther(uid);

                if (other == relay)
                {
                    SetRelay(uid, null, component);
                    return;
                }
            }
        }

        SetRelay(uid, relay, component);
    }

    /// <summary>
    /// Refreshes the joint relay for this entity.
    /// </summary>
    public void SetRelay(EntityUid uid, EntityUid? relay, JointComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.Relay == relay)
            return;

        if (TryComp<JointRelayTargetComponent>(component.Relay, out var relayTarget))
        {
            if (relayTarget.Relayed.Remove(uid))
            {
                if (relayTarget.Relayed.Count == 0)
                {
                    RemCompDeferred<JointRelayTargetComponent>(component.Relay.Value);
                }

                Dirty(component.Relay.Value, relayTarget);
            }
        }

        component.Relay = relay;

        if (relay != null)
        {
            relayTarget = EnsureComp<JointRelayTargetComponent>(relay.Value);
            if (relayTarget.Relayed.Add(uid))
            {
                _physics.WakeBody(relay.Value);
                Dirty(relay.Value, relayTarget);
            }
        }

        Dirty(uid, component);

#if DEBUG
        if (component.Relay == null)
            return;

        if (TryComp(uid, out JointComponent? jointComp))
        {
            foreach (var joint in jointComp.Joints.Values)
            {
                DebugTools.AssertNotEqual(joint.BodyAUid, component.Relay);
                DebugTools.AssertNotEqual(joint.BodyBUid, component.Relay);

            }
        }

        if (TryComp(component.Relay, out JointComponent? relayJointComp))
        {
            foreach (var joint in relayJointComp.Joints.Values)
            {
                DebugTools.AssertNotEqual(joint.BodyAUid, uid);
                DebugTools.AssertNotEqual(joint.BodyBUid, uid);
            }
        }
#endif
    }
}
