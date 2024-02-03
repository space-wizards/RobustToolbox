using System;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Robust.Shared.GameObjects;

/// <summary>
/// This component is attached to timer entities, which are then parented to the entity that <see cref="Event"/>
/// will be raised on after some specified amount of time.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EntityTimerComponent : Component
{
    /// <summary>
    ///     The event to raise on fire. Should be initialized.
    /// </summary>
    [DataField(required: true)]
    public IEntityTimerEvent Event = default!;

    /// <summary>
    ///     The absolute time at which to fire this timer. Should be initialized.
    /// </summary>
    [DataField(required: true, customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan AbsoluteTime = default!;
}
