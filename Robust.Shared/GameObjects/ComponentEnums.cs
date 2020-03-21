namespace Robust.Shared.GameObjects
{
    public enum DrawDepth
    {

        LowFloors = 0,
        /// <summary>
        ///     Things that are beneath regular floors, such as wires.
        /// </summary>
        BelowFloor = 1,
        FloorTiles = 2,
        /// <summary>
        ///     Things that are actually right on the floor, like vents.
        /// </summary>
        FloorObjects = 3,

        Walls = 4,
        WallTops = 5,
        WallMountedItems = 6,
        Objects = 7,
        Items = 8,
        Mobs = 9,
        Overlays = 10,
    }
}
