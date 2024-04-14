using Robust.Shared.GameObjects;

namespace Robust.Shared.Enums;

public sealed class PlacementInformation
{
    /// <summary>
    /// Entity prototype to be placed
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Indiciates if the entity prototype to be placed is in fact a tile
    /// </summary>
    public bool IsTile { get; set; }

    /// <summary>
    /// ID of the mob that has permission to place the prototype
    /// </summary>
    public EntityUid MobUid { get; set; }

    /// <summary>
    /// Specifies the placement alignment
    /// </summary>
    public string? PlacementOption { get; set; }

    /// <summary>
    /// Determines the max range at which the entity prototype can be placed
    /// </summary>
    public int Range { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public int TileType { get; set; }

    /// <summary>
    /// Number of times the entity can be placed
    /// </summary>
    public int Uses { get; set; } = 1;

    /// <summary>
    /// Sets whether the input context should switch to 'editor' mode
    /// </summary>
    public bool UseEditorContext { get; set; } = true;
}
