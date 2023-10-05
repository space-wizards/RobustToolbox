using System;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

/// <summary>
/// This is used for marking repeating timer entities, which will not delete after one fire
/// and have some metadata.
/// </summary>
[RegisterComponent]
public sealed partial class RepeatingEntityTimerComponent : Component
{
    /// <summary>
    ///     Relative delay for setting the next absolute time in <see cref="EntityTimerComponent"/> after each repetition.
    ///     This can be modified during subscription to determine when the next fire should happen.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Delay;

    /// <summary>
    ///     Total number of times this timer has already fired.
    ///     Starts at 0 for the first fire and is only incremented after.
    /// </summary>
    [DataField, Access(typeof(EntityTimerSystem))]
    public int TotalRepetitions;

    /// <summary>
    ///     Maximum number of times the event can be raised
    ///     before the repeating timer will be deleted.
    /// </summary>
    [DataField]
    public int MaxRepetitions = int.MaxValue;
}
