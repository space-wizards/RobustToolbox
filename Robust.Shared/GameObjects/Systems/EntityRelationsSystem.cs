namespace Robust.Shared.GameObjects;

public sealed partial class EntityRelationsSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnEntityRelationDelete(Entity<EntityRelationsComponent> ent, ref EntityTerminatingEvent args)
    {
        ClearRelations(ent.AsNullable());
    }
}

/// <summary>
/// Raised by some entity on each of its relations to inform them about removing
/// </summary>
/// <param name="Relation">A relation that is about to become invalid.</param>
[ByRefEvent]
public readonly record struct EntityRelationDeleteEvent(EntityRelation Relation);
