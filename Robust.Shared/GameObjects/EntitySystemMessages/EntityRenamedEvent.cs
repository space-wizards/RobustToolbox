namespace Robust.Shared.GameObjects;

/// <summary>
/// Raised directed on an entity when its name is changed.
/// Contains the EntityUid as systems may need to subscribe to it without targeting a specific component.
/// </summary>
[ByRefEvent]
public readonly record struct EntityRenamedEvent(EntityUid Uid, string OldName, string NewName);
