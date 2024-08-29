namespace Robust.Shared.Graphics.RSI;

/// <summary>
///     Specifies a direction in an RSI state.
/// </summary>
/// <remarks>
///     Value of the enum here matches the index used to store it in the icons array. If this ever changes, then
///     <see cref="Robust.Client.GameObjects.SpriteComponent.Layer._rsiDirectionMatrices"/> also needs to be updated.
/// </remarks>
public enum RsiDirection : byte
{
    South = 0,
    North = 1,
    East = 2,
    West = 3,
    SouthEast = 4,
    SouthWest = 5,
    NorthEast = 6,
    NorthWest = 7,
}
