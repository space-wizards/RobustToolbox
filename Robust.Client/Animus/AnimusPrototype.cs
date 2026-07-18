using System.Collections.Generic;
using Robust.Client.Animus.States;
using Robust.Client.Animus.Timers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Animus;

[Prototype]
internal sealed partial class AnimusPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// A collection of possible states for this animus.
    /// </summary>
    [DataField]
    internal List<AnimusStateBase> States = [];

    /// <summary>
    /// Optional timer for executing an update.
    /// Setting this disables periodic condition checks.
    /// </summary>
    [DataField]
    internal AnimusTimerBase? Timer;

    /// <summary>
    /// The default state to enter when no other state matches their conditions.
    /// </summary>
    [DataField]
    internal AnimusStateBase DefaultState = new AnimusStateNull();
}
