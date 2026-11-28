namespace Robust.Shared.GameObjects;

/// <summary>
///     Raised directed on an entity when the map has been initialized and the entity has been successfully reparented.
/// </summary>
[ComponentEvent(Exclusive = false)]
public sealed class MapInitEvent : EntityEventArgs
{
}
