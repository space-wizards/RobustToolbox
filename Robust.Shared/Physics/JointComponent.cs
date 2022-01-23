using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics
{
    [RegisterComponent]
    [NetworkedComponent]
    // [Friend(typeof(SharedJointSystem))]
    [ComponentProtoName("Joint")]
    public sealed class JointComponent : Component
    {
        [ViewVariables]
        public int JointCount => Joints.Count;

        [ViewVariables]
        public IEnumerable<Joint> GetJoints => Joints.Values;

        [DataField("joints")]
        internal Dictionary<string, Joint> Joints = new();

        [Serializable, NetSerializable]
        public sealed class JointComponentState : ComponentState
        {
            public Dictionary<string, JointState> Joints;

            public JointComponentState(Dictionary<string, JointState> joints)
            {
                Joints = joints;
            }
        }
    }
}
