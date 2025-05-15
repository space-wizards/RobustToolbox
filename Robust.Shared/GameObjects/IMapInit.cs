namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Raised directed on an entity when the map is initialized.
    /// </summary>
    [ComponentEvent(Exclusive = false)]
    public sealed class MapInitEvent : EntityEventArgs
    {
    }
}
