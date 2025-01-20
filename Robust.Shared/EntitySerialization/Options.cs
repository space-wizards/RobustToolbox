using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.EntitySerialization.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.EntitySerialization;

public record struct SerializationOptions
{
    public static readonly SerializationOptions Default = new();

    /// <summary>
    /// What to do when serializing the EntityUid of an entity that is not one of entities currently being serialized.
    /// I.e., What should happen when serializing a map that has entities with components that store references to a
    /// null-space entity? Note that this does not affect the treatment of <see cref="TransformComponent.ParentUid"/>,
    /// which will never auto-include parents.
    /// </summary>
    public MissingEntityBehaviour MissingEntityBehaviour = MissingEntityBehaviour.IncludeNullspace;

    /// <summary>
    /// Whether or not to log an error when serializing an entity without its parent.
    /// </summary>
    public bool ErrorOnOrphan = true;

    /// <summary>
    /// Log level to use when auto-including entities while serializing. Null implies no logs.
    /// See <see cref="MissingEntityBehaviour"/>.
    /// </summary>
    public LogLevel? LogAutoInclude = LogLevel.Info;

    /// <summary>
    /// If true, the serializer will log an error if it encounters a post map-init entity.
    /// </summary>
    public bool ExpectPreInit;

    public FileCategory Category;

    public SerializationOptions()
    {
    }
}

public record struct DeserializationOptions()
{
    public static readonly DeserializationOptions Default = new();

    /// <summary>
    /// If true, each loaded entity will get a <see cref="YamlUidComponent"/> that stores the uid that the entity
    /// had in the yaml file. This is used to maintain consistent entity labelling on subsequent saves.
    /// </summary>
    public bool StoreYamlUids = false;

    /// <summary>
    /// If true, all maps that get created while loading this file will get map-initialized.
    /// </summary>
    public bool InitializeMaps = false;

    /// <summary>
    /// If true, all maps that get created while loading this file will get paused.
    /// Note that the converse is not true, paused maps will not get un-paused if this is false.
    /// Pre-mapinit maps are assumed to be paused.
    /// </summary>
    public bool PauseMaps = false;

    /// <summary>
    /// Whether or not to log an error when starting up a grid entity that has no map.
    /// This usually indicates that someone is attempting to load an incorrect file type (e.g. loading a grid as a map).
    /// </summary>
    public bool LogOrphanedGrids = true;

    /// <summary>
    /// Whether or not to log an error when encountering an yaml entity id.
    /// <see cref="TransformComponent.ParentUid"/> is exempt from this.
    /// </summary>
    public bool LogInvalidEntities = true;

    /// <summary>
    /// Whether or not to automatically assign map ids to any deserialized map entities.
    /// If false, maps need to be manually given ids before entities are initialized.
    /// </summary>
    public bool AssignMapids = true;
}

/// <summary>
/// Superset of <see cref="EntitySerialization.DeserializationOptions"/> that contain information relevant to loading
/// maps & grids, potentially onto other existing maps.
/// </summary>
public struct MapLoadOptions()
{
    public static readonly MapLoadOptions Default = new();

    /// <summary>
    /// If specified, all orphaned entities and the children of all loaded maps will be re-parented onto this map.
    /// I.e., this will merge map contents onto an existing map. This will also cause any maps that get loaded to
    /// delete themselves after their children have been moved.
    /// </summary>
    /// <remarks>
    /// Note that this option effectively causes <see cref="DeserializationOptions.InitializeMaps"/> and
    /// <see cref="DeserializationOptions.PauseMaps"/> to have no effect, as the target map is not a map that was
    /// created by the deserialization.
    /// </remarks>
    public MapId? MergeMap = null;

    /// <summary>
    /// Offset to apply to the position of any loaded entities that are directly parented to a map.
    /// </summary>
    public Vector2 Offset;

    /// <summary>
    /// Rotation to apply to the position & local rotation of any loaded entities that are directly parented to a map.
    /// </summary>
    public Angle Rotation;

    /// <summary>
    /// Options to use when deserializing entities.
    /// </summary>
    public DeserializationOptions DeserializationOptions = DeserializationOptions.Default;

    /// <summary>
    /// When loading a single map, this will attempt to force the map to use the given map id. Generally, it is better
    /// to allow the map system to auto-allocate a map id, to avoid accidentally re-using an old id.
    /// </summary>
    public MapId? ForceMapId;

    /// <summary>
    /// The expected <see cref="LoadResult.Category"/> for the file currently being read in, at the end of the entity
    /// creation step. Will log errors if the category doesn't match the expected one (e.g., trying to load a "map" from a file
    /// that doesn't contain any map entities).
    /// </summary>
    /// <remarks>
    /// Note that the effective final category may change by the time the file has fully loaded. E.g., when loading a
    /// file containing an orphaned grid, a map may be automatically created for the grid, but the category will still
    /// be <see cref="FileCategory.Grid"/>
    /// </remarks>
    public FileCategory? ExpectedCategory;
}
