using System.Collections.Generic;
using Robust.Shared.GameObjects;

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
/// This event is broadcast just before the map loader reads the entity section. It can be used to somewhat modify
/// how the map data is read, as a super basic kind of map migration tool.
/// </summary>
public sealed class BeforeSaveEvent
{
    /// <summary>
    /// The entity that is going to be saved. usually a map or grid.
    /// </summary>
    public EntityUid Entity;

    /// <summary>
    /// The map that the <see cref="Entity"/> is on.
    /// </summary>
    public EntityUid? Map;

    public BeforeSaveEvent(EntityUid entity, EntityUid? map)
    {
        Entity = entity;
        Map = map;
    }
}
