using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Represents a reference to an entity that automatically sets to null when that entity is deleted.
/// </summary>
[DataDefinition, Access(typeof(EntityManager), typeof(EntityRelationsSystem), Other = AccessPermissions.ReadExecute)]
public partial record struct EntityRelation
{
    public EntityUid? Entity;

    public static implicit operator EntityUid?(EntityRelation relation)
    {
        return relation.Entity;
    }

    public static readonly EntityRelation Null = new() { Entity = null };

    public bool HasValue => Entity != null;
}
