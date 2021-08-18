using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;

namespace Robust.Client.Physics
{
    public sealed class JointSystem : SharedJointSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<JointComponent, ComponentHandleState>(HandleComponentState);
        }

        private void HandleComponentState(EntityUid uid, JointComponent component, ComponentHandleState args)
        {
            if (args.Current is not JointComponent.JointComponentState jointState) return;

            var changed = new List<string>();

            foreach (var (existing, _) in component.Joints)
            {
                if (!jointState.Joints.ContainsKey(existing))
                {
                    changed.Add(existing);
                }
            }

            foreach (var removed in changed)
            {
                RemoveJointDeferred(component.Joints[removed]);
            }

            foreach (var (id, state) in jointState.Joints)
            {
                var joint = state.GetJoint();

                if (!component.Joints.ContainsKey(id))
                {
                    // Add new joint (if possible).
                    // Need to wait for BOTH joint components to come in first before we can add it
                    if (!joint.BodyA.Owner.HasComponent<JointComponent>() ||
                        !joint.BodyB.Owner.HasComponent<JointComponent>()) continue;

                    AddJointDeferred(joint.BodyA, joint.BodyB, joint);
                    continue;
                }

                var existing = component.Joints[id];

                if (!existing.Equals(joint))
                {
                    existing.ApplyState(state);
                    component.Dirty();
                }
            }

            if (changed.Count > 0)
            {
                component. Dirty();
            }
        }
    }
}
