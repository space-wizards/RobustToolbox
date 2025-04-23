using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Audio.Components;

/// <summary>
/// Stores filtering information for <see cref="AudioComponent"/>,
/// so replay clients can filter audio events they shouldn't be able to hear.
/// </summary>
/// <remarks>
/// This is a separate component so that we don't send this data to regular players, only replay recording.
/// </remarks>
[RegisterComponent]
[NetworkedComponent]
internal sealed partial class AudioPlayerFilterComponent : Component
{
    /// <summary>
    /// The list of player entities that should be able to hear this sound.
    /// </summary>
    [DataField]
    public IReadOnlyCollection<EntityUid> IncludedEntities = [];

    /// <summary>
    /// Filter expression that can be used to evaluate elegibility for replay clients.
    /// </summary>
    [DataField]
    public FilterExpression? FilterExpression;

    [Serializable, NetSerializable]
    public sealed class ComponentState(NetEntity[] entities, FilterExpression? filterExpression) : IComponentState
    {
        public NetEntity[] Entities = entities;
        public FilterExpression? FilterExpression = filterExpression;
    }
}
