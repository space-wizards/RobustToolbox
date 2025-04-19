using Robust.Shared.GameObjects;
using Robust.Shared.Upload;

namespace Robust.Shared.EntitySerialization;

/// <summary>
/// This enum is used to indicate the type of entity data that was written to a file. The actual format of the file does
/// not change, but it helps avoid mistakes like accidentally using a map file when trying to load a single grid.
/// </summary>
public enum FileCategory : byte
{
    Unknown,

    /// <summary>
    /// File should contain a single orphaned entity, its children, and maybe some null-space entities.
    /// </summary>
    Entity,

    /// <summary>
    /// File should contain a single grid, its children, and maybe some null-space entities.
    /// </summary>
    Grid,

    /// <summary>
    /// File should contain a single map, its children, and maybe some null-space entities.
    /// </summary>
    Map,

    /// <summary>
    /// File is a full game save, and will likely contain at least one map and a few null-space entities.
    /// </summary>
    /// <remarks>
    /// The file might also contain additional yaml entries for things like prototypes uploaded via
    /// <see cref="IGamePrototypeLoadManager"/>, and might contain references to additional resources that need to be
    /// loaded (e.g., files uploaded using <see cref="SharedNetworkResourceManager"/>).
    /// </remarks>
    Save,
}

public enum MissingEntityBehaviour
{
    /// <summary>
    /// Log an error and replace the reference with <see cref="EntityUid.Invalid"/>
    /// </summary>
    Error,

    /// <summary>
    /// Ignore the reference, replace it with <see cref="EntityUid.Invalid"/>
    /// </summary>
    Ignore,

    /// <summary>
    /// Automatically include & serialize any referenced null-space entities and their children.
    /// I.e., entities that are not attached to any parent and are not maps. Any non-nullspace entities will result in
    /// an error.
    /// </summary>
    /// <remarks>
    /// This is primarily intended to make it easy to auto-include information carrying null-space entities. E.g., the
    /// "minds" of players, or entities that represent power or gas networks on a grid. Note that a full game save
    /// should still try to explicitly include all relevant entities, as this could still easily fail to auto-include
    /// relevant entities if they are not explicitly referenced in a data-field by some other entity.
    /// </remarks>
    IncludeNullspace,

    /// <summary>
    /// Automatically include & serialize any referenced entity. Note that this means that the missing entity's
    /// parents will (generally) also be included, however this will not include other children. E.g., if serializing a
    /// grid that references an entity on the map, this will also cause the map to get serialized, but will not necessarily
    /// serialize everything on the map.
    /// </summary>
    /// <remarks>
    /// If trying to serialize an entity without its parent (i.e., its parent is truncated via
    /// <see cref="EntitySerializer.Truncate"/>), this will try to respect that. E.g., if a referenced entity is on the
    /// same map as a grid that is getting serialized, it should include the entity without including the map.
    /// </remarks>
    /// <remarks>
    /// Note that this might unexpectedly change the <see cref="FileCategory"/>. I.e., trying to serialize a grid might
    /// accidentally lead to serializing a (partial?) map file.
    /// </remarks>
    PartialInclude,

    /// <summary>
    /// Variant of <see cref="PartialInclude"/> that will also automatically include the children of any entities that
    /// that are automatically included. Note that because auto-inclusion generally needs to include an entity's
    /// parents, this will include more than just the missing entity's direct children.
    /// </summary>
    AutoInclude,
}
