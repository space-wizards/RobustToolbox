using JetBrains.Annotations;
using Robust.Client.Animus.States;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus.Triggers;

[ImplicitDataDefinitionForInheritors]
[PublicAPI]
public sealed partial class AnimusTriggerEventSubscription
{
    [DataField("component")]
    public string ComponentName;

    [DataField("event")]
    public string EventName;
}
