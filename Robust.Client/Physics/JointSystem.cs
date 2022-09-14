using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;

namespace Robust.Client.Physics
{
    public sealed class JointSystem : SharedJointSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<JointComponent, ComponentHandleState>(HandleComponentState);
        }

        private void HandleComponentState(EntityUid uid, JointComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not JointComponent.JointComponentState jointState) return;

            // Initial state gets applied before the entity (& entity's transform) have been initialized.
            // So just let joint init code handle that.
            if (!component.Initialized)
            {
                component.Joints.Clear();
                foreach (var (id, state) in jointState.Joints)
                {
                    component.Joints[id] = state.GetJoint();
                }
                return;
            }

            var removed = new List<Joint>();
            foreach (var (existing, j) in component.Joints)
            {
                if (!jointState.Joints.ContainsKey(existing))
                    removed.Add(j);
            }

            foreach (var j in removed)
            {
                RemoveJoint(j);
            }

            foreach (var (id, state) in jointState.Joints)
            {
                if (component.Joints.TryGetValue(id, out var joint))
                {
                    joint.ApplyState(state);
                    continue;
                }

                var other = state.UidA == uid ? state.UidB : state.UidA;


                // Add new joint (if possible).
                // Need to wait for BOTH joint components to come in first before we can add it. Yay dependencies!
                if (!EntityManager.HasComponent<JointComponent>(other))
                    continue;

                // TODO: if (other entity is outside of PVS range) continue;
                // for now, half-assed check until something like PR #3000 gets merged.
                if (Transform(other).MapID == MapId.Nullspace)
                    continue;

                // oh jolly what good fun: the joint component state can get handled prior to the transform component state.
                // so if our current transform is on another map, this would throw an error.
                // so lets just... assume the server state isn't messed up, and defer the joint processing.
                // alternatively:
                // TODO: component state handling ordering.
                if (Transform(uid).MapID == MapId.Nullspace)
                {
                    AddedJoints.Add(state.GetJoint());
                    continue;
                }

                AddJoint(state.GetJoint());
            }
        }
    }
}
