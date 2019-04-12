using System;

namespace SS14.Shared.Physics
{
    /// <summary>
    ///     Defined collision groups for the physics system.
    /// </summary>
    [Flags]
    public enum CollisionGroup
    {
        None = 0,
        Grid = 1 << 0, // Walls
        Mob = 1 << 1, // Mobs, like the player or NPCs
        Fixture = 1 << 2, // wall fixtures, like APC or posters
        Items = 1 << 3, // Items on the ground
        Floor = 1 << 4, // Items on the ground
    }
}
