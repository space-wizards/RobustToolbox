namespace Robust.Shared.GameObjects;

/// <summary>
/// Raised during <see cref="SharedVisibilitySystem.RefreshVisibility(EntityUid, VisibilityComponent?, MetaDataComponent?)"/>
/// so systems can contribute temporary visibility layer modifiers without mutating the base layer directly.
/// </summary>
[ByRefEvent]
public record struct RefreshVisibilityModifiersEvent(ushort Layer)
{
    /// <summary>
    /// Adds the specified visibility bits to the refreshed layer.
    /// </summary>
    public void AddLayer(ushort layer)
    {
        Layer |= layer;
    }

    /// <summary>
    /// Removes the specified visibility bits from the refreshed layer.
    /// </summary>
    public void RemoveLayer(ushort layer)
    {
        Layer &= (ushort) ~layer;
    }
}
