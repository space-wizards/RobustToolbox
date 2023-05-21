using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedJointSystem
{
    /*
     * Relays joints to the top-most parent so joints inside of containers can still function.
     * This is because we still want the joint to "function" it just needs to affect the parent instead.
     */

    // Have "JointRelayTarget" that has the entities it relays from / joint id
    // On parent change clear old one and then add to new one.
    // In the island solve check for the comp and add a dummy joint for it
    // At the end of the step feed the data back into the original joint.

    private void InitializeRelay()
    {
        SubscribeLocalEvent<JointRelayTargetComponent, ComponentShutdown>(OnRelayShutdown);
    }

    public EntityUid GetOther(EntityUid uid, Joint joint)
    {
        DebugTools.Assert(joint.BodyAUid == uid || joint.BodyBUid == uid);
        return uid == joint.BodyAUid ? joint.BodyBUid : joint.BodyAUid;
    }

    private void OnRelayShutdown(EntityUid uid, JointRelayTargetComponent component, ComponentShutdown args)
    {
        var jointQuery = GetEntityQuery<JointComponent>();

        foreach (var relay in component.Relayed)
        {
            if (Deleted(relay) || !jointQuery.TryGetComponent(relay, out var joint))
                continue;

            RefreshRelay(relay, joint);
        }
    }

    public void RefreshRelay(EntityUid uid, JointComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var relay = component.Relay;

        component.Relay = null;

        if (_container.TryGetOuterContainer(uid, Transform(uid), out var container))
        {
            relay = container.Owner;
        }

        if (component.Relay == relay)
            return;

        if (TryComp<JointRelayTargetComponent>(relay, out var relayTarget))
        {
            if (relayTarget.Relayed.Remove(uid))
            {
                Dirty(relayTarget);
            }
        }

        component.Relay = relay;
        Dirty(component);
    }
}
