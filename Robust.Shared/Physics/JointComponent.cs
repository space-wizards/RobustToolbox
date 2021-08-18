using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class JointComponent : Component
    {
        public override string Name => "Joint";

        [ViewVariables]
        public int JointCount => Joints.Count;

        [ViewVariables]
        public IEnumerable<Joint> GetJoints => Joints.Values;

        internal Dictionary<string, Joint> Joints = new();

        protected override void OnAdd()
        {
            base.OnAdd();
            Logger.DebugS("physics", $"Added joint component to {Owner}");
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            var jointSystem = EntitySystem.Get<SharedJointSystem>();

            foreach (var joint in Joints.Values.ToArray())
            {
                jointSystem.RemoveJointDeferred(joint);
            }

            Logger.DebugS("physics", $"Removed joint component for {Owner}");
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            var states = new Dictionary<string, JointState>(Joints.Count);

            foreach (var (_, joint) in Joints)
            {
                states.Add(joint.ID, joint.GetState());
            }

            return new JointComponentState(states);
        }

        [Serializable, NetSerializable]
        public sealed class JointComponentState : ComponentState
        {
            public Dictionary<string, JointState> Joints = new();

            public JointComponentState(Dictionary<string, JointState> joints)
            {
                Joints = joints;
            }
        }
    }
}
