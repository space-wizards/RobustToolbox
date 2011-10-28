public enum PlacementOption
{
    /// <summary>
    ///  Does not snap-to anything. Can not be used on walls.
    /// </summary>
    AlignNone = 0,
    /// <summary>
    ///  Snaps to similar, nearby objects. Can not be used on walls.
    /// </summary>
    AlignSimilar,
    /// <summary>
    ///  Snaps to tile grid. Tiles can *only use this option*.
    /// </summary>
    AlignTile,
    /// <summary>
    ///  Used for wall mounted objects. Snaps to walls. X variable, Y fixed.
    /// </summary>
    AlignWall,

    /// <summary>
    ///  Does not snap-to anything. Can not be used on walls. Range not limited. Requires admin.
    /// </summary>
    AlignNoneFree,
    /// <summary>
    ///  Snaps to similar, nearby objects. Can not be used on walls. Range not limited. Requires admin.
    /// </summary>
    AlignSimilarFree,
    /// <summary>
    ///  Snaps to tile grid. Tiles can *only use this option*. Range not limited. Requires admin.
    /// </summary>
    AlignTileFree,
    /// <summary>
    ///  Used for wall mounted objects. Snaps to walls. X variable, Y fixed. Range not limited. Requires admin.
    /// </summary>
    AlignWallFree,
    /// <summary>
    ///  Not limited in any way. Requires admin.
    /// </summary>
    Freeform,
}