using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Analyzers;
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
    [Friend(typeof(SharedJointSystem))]
    public sealed class JointComponent : Component
    {
        public override string Name => "Joint";

        [ViewVariables]
        public int JointCount => Joints.Count;

        [ViewVariables]
        public IEnumerable<Joint> GetJoints => Joints.Values;

        internal Dictionary<string, Joint> Joints = new();

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
