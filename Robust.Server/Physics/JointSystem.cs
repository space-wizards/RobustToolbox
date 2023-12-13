using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;

namespace Robust.Server.Physics;

public sealed class JointSystem : SharedJointSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JointComponent, ComponentGetState>(GetCompState);
    }

    private void GetCompState(EntityUid uid, JointComponent component, ref ComponentGetState args)
    {
        var states = new Dictionary<string, JointState>(component.Joints.Count);

        foreach (var (id, joint) in component.Joints)
        {
            states.Add(id, joint.GetState(EntityManager));
        }

        args.State = new JointComponentState(GetNetEntity(component.Relay), states);
    }
}
