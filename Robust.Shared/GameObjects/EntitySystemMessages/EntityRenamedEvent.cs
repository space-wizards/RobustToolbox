namespace Robust.Shared.GameObjects;

/// <summary>
/// Raised directed on an entity when its name is changed.
/// </summary>
[ByRefEvent]
public readonly record struct EntityRenamedEvent(string NewName);
