using Robust.Shared.GameObjects;

namespace Robust.Client.GameStates;

/// <summary>
/// Raised directed on an entity after it re-enters the client's PVS range and its states have been applied.
/// </summary>
/// <remarks>
/// On re-entry only components that changed (or were dirtied while detached) have their states re-handled.
/// </remarks>
[ByRefEvent]
public readonly record struct ReenteredPvsEvent;
