using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Represents a reference to an entity with <see cref="EntityRelationsComponent"/>.
/// When the referenced <see cref="EntityUid"/> is deleted, <see cref="EntityRelationDeleteEvent"/>
/// is raised on all entities that reference it, and each entity will automatically reset the reference.
/// </summary>
[DataDefinition, Access(typeof(EntityManager), typeof(EntityRelationsSystem), Other = AccessPermissions.ReadExecute)]
public partial record struct EntityRelation
{
    /// <summary>
    /// Reference to an entity that if not null guaranteed to have <see cref="EntityRelationsComponent"/>.
    /// </summary>
    [DataField]
    public EntityUid? Entity;

    /// <summary>
    /// Internal constructor. Use it only if you know what you're doing!
    /// </summary>
    internal EntityRelation(EntityUid? entity)
    {
        Entity = entity;
    }

    public static implicit operator EntityUid?(EntityRelation relation)
    {
        return relation.Entity;
    }

    public static readonly EntityRelation Null = new(null);

    /// <summary>
    /// A helper method that returns true if the stored Entity exists in this reation.
    /// </summary>
    public bool HasValue => Entity != null;
}
