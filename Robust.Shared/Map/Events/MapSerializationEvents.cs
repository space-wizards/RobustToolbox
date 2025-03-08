using System.Collections.Generic;
using Robust.Shared.EntitySerialization;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Robust.Shared.Map.Events;

/// <summary>
/// This event is broadcast just before the map loader reads the entity section. It can be used to somewhat modify
/// how the map data is read, as a super basic kind of map migration tool.
/// </summary>
public sealed class BeforeEntityReadEvent
{
    /// <summary>
    /// Set of deleted entity prototypes.
    /// </summary>
    /// <remarks>
    /// While reading the map, these entities will be treated as if they have no prototype. After the map has been
    /// loaded, these entities will get deleted. This is so that entities parented to this entity (e.g., stored in
    /// containers) also get deleted, instead of just causing errors. Note that this has not been properly tested is
    /// quite likely to cause unexpected errors and should be used with care.
    /// </remarks>
    public readonly HashSet<string> DeletedPrototypes = new();

    /// <summary>
    /// This dictionary maps old entity prototype IDs to some new value. As with <see cref="DeletedPrototypes"/>, this
    /// might cause unexpected errors, user beware.
    /// </summary>
    public readonly Dictionary<string, string> RenamedPrototypes = new();
}

/// <summary>
/// This event is broadcast just before the given entities (and their children) are serialized.
/// For convenience, the event also contains a set with all the maps that the entities are on. This does not
/// necessarily mean that the maps are themselves getting serialized.
/// </summary>
public readonly record struct BeforeSerializationEvent(HashSet<EntityUid> Entities, HashSet<MapId> MapIds);

/// <summary>
/// This event is broadcast just after entities (and their children) have been serialized, but before it gets written to a yaml file.
/// </summary>
public readonly record struct AfterSerializationEvent(HashSet<EntityUid> Entities, MappingDataNode Node, FileCategory Category);
