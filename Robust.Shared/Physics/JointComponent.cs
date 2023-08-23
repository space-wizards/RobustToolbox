using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class JointComponent : Component
{
    /// <summary>
    /// Are we relaying our joints to a parent entity.
    /// </summary>
    [DataField("relay")]
    public EntityUid? Relay;

    [ViewVariables]
    public int JointCount => Joints.Count;

    public IReadOnlyDictionary<string, Joint> GetJoints => Joints;

    [DataField("joints")]
    internal Dictionary<string, Joint> Joints = new();
}

[Serializable, NetSerializable]
public sealed class JointComponentState : ComponentState
{
    public NetEntity? Relay;
    public Dictionary<string, JointState> Joints;

    public JointComponentState(NetEntity? relay, Dictionary<string, JointState> joints)
    {
        Relay = relay;
        Joints = joints;
    }
}
