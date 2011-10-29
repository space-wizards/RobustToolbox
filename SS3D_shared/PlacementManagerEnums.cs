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
    ///  Snaps to tile grid. Can be placed on solid, non solid and empty tiles.
    /// </summary>
    AlignTileAny,
    /// <summary>
    ///  Snaps to tile grid. Can only be placed on solid tiles.
    /// </summary>
    AlignTileSolid,
    /// <summary>
    ///  Snaps to tile grid. Can only be placed on non-solid tiles.
    /// </summary>
    AlignTileNonSolid,
    /// <summary>
    ///  Snaps to tile grid. Can only be placed on empty tiles (space etc).
    /// </summary>
    AlignTileEmpty,

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
    ///  Snaps to tile grid. Can be placed on solid, non solid and empty tiles. Range not limited. Requires admin. 
    /// </summary>
    AlignTileAnyFree,
    /// <summary>
    ///  Snaps to tile grid. Can only be placed on solid tiles. Range not limited. Requires admin.
    /// </summary>
    AlignTileSolidFree,
    /// <summary>
    ///  Snaps to tile grid. Can only be placed on non-solid tiles. Range not limited. Requires admin.
    /// </summary>
    AlignTileNonSolidFree,
    /// <summary>
    ///  Snaps to tile grid. Can only be placed on empty tiles (space etc). Range not limited. Requires admin.
    /// </summary>
    AlignTileEmptyFree,

    /// <summary>
    ///  Used for wall mounted objects. Snaps to walls. X variable, Y fixed. Range not limited. Requires admin.
    /// </summary>
    AlignWallFree,
    /// <summary>
    ///  Not limited in any way. Requires admin.
    /// </summary>
    Freeform,
}