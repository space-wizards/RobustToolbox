using System;

namespace Robust.Shared.GameStates;

/// <summary>
/// This attribute marks a component as networked, so that it is replicated to clients.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class NetworkedComponentAttribute : Attribute
{
    public NetworkedComponentAttribute()
    {
    }

    public NetworkedComponentAttribute(StateRestriction restriction)
    {
        Restriction = restriction;
    }

    public readonly StateRestriction Restriction = StateRestriction.None;

}

public enum StateRestriction : byte
{
    /// <summary>
    /// No restrictions, every player can know about and receive this component's state.
    /// </summary>
    None = 0,

    /// <summary>
    /// This component will only be networked to players that are currently attached to this component's owning entity.
    /// Other players won't even be notified when the component is added or removed.
    /// </summary>
    /// <remarks>
    /// Replays will still always receive this component.
    /// </remarks>
    OwnerOnly = 1,

    /// <summary>
    /// This component will not be networked to any players, but will still be recorded in replays. Players won't even
    /// be notified when the component is added or removed.
    /// </summary>
    ReplayOnly = 2,

    /// <summary>
    /// This component will raise an <see cref="ComponentGetStateAttemptEvent"/> to determine whether the component
    /// should be networked to any specific session. If the get state attempt event is cancelled, players won't
    /// receive component states and won't even be notified when the component is added or removed.
    /// </summary>
    /// <remarks>
    /// Replays will still always receive this component.
    /// </remarks>
    SessionSpecific = 3
}
