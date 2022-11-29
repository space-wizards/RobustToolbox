using Robust.Shared.GameObjects;

namespace Robust.Shared.Map.Events;

/// <summary>
/// Raised whenever a grid becomes empty due to no more tiles with data.
/// </summary>
public sealed class EmptyGridEvent : EntityEventArgs
{
    public EntityUid GridId { get; init;  }
}