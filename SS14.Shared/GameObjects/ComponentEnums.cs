namespace SS14.Shared.GameObjects
{
    public enum ComponentMessageType
    {
        Null,
        Empty,
        BoundKeyChange, // U
        BoundKeyRepeat, // U
        SpriteChanged, // U
        Bumped, // U
        EntitySaidSomething, // U
    }

    public enum DrawDepth
    {
        /// <summary>
        ///     Floors that are low and below wires, such as plating without floor tiles.
        /// </summary>
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
        Objects = 4,
        Items = 5,
        Mobs = 6,
        Walls = 7,
        WallMountedItems = 8,
        WallTops = 9,
        Overlays = 10,
    }
}
