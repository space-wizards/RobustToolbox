using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;

namespace Robust.Server.Physics
{
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

            foreach (var (_, joint) in component.Joints)
            {
                states.Add(joint.ID, joint.GetState());
            }

            args.State = new JointComponent.JointComponentState(states);
        }
    }
}
