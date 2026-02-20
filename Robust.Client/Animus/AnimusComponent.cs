using System;
using System.Collections.Generic;
using Robust.Client.Animus.States;
using Robust.Client.Animus.Timers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus;

[RegisterComponent]
internal sealed partial class AnimusComponent : Component
{
    [DataField]
    internal List<ProtoId<AnimusPrototype>> StateMachines;

    internal readonly List<AnimusInstance> ActiveStateMachines = [];
}

/// <summary>
/// Prototypes are global instances but conditions and other objects may require per-instance data.
/// Because of this, conditions
/// </summary>
internal sealed class AnimusInstance
{
    internal ProtoId<AnimusPrototype> Prototype = default;
    internal AnimusStateBase[] States = [];
    internal AnimusTimerBase? Timer = null;
    internal AnimusStateBase ActiveState = new AnimusStateNull();
    internal AnimusStateBase DefaultState = new AnimusStateNull();
    internal TimeSpan ActiveStateExitTime = TimeSpan.Zero;
    internal TimeSpan NextUpdate = TimeSpan.Zero;
}
