namespace Robust.Shared.Graphics.RSI;

/// <summary>
///     Specifies which types of directions an RSI state has.
/// </summary>
public enum RsiDirectionType : byte
{
    /// <summary>
    ///     A single direction, namely South.
    /// </summary>
    Dir1,

    /// <summary>
    ///     4 cardinal directions.
    /// </summary>
    Dir4,

    /// <summary>
    ///     4 cardinal + 4 diagonal directions.
    /// </summary>
    Dir8,
}