public enum AlignmentOptions
{
    /// <summary>
    ///  Does not snap-to anything. *DO NOT USE WITH TILES.*
    /// </summary>
    AlignNone = 0,
    /// <summary>
    ///  Snaps to similar, nearby objects. *DO NOT USE WITH TILES.*
    /// </summary>
    AlignSimilar,
    /// <summary>
    ///  Snaps to tile grid. Tiles can only use this option.
    /// </summary>
    AlignTile,
    /// <summary>
    ///  Used for wall mounted objects *ONLY*. Snaps to walls. X variable, Y fixed.
    /// </summary>
    AlignWall
}