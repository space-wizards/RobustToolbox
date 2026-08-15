namespace Robust.Shared.GameObjects;

/// <summary>
/// A system that manually handles <see cref="EntityRelationsComponent"/>'s entity relation events,
/// since it can't modify itself without copying the set.
/// </summary>
public sealed partial class EntityRelationsSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private static void OnEntityRelationDelete(Entity<EntityRelationsComponent> ent, ref EntityRelationDeleteEvent args)
    {
        ent.Comp.Relations.Remove(args.Relation);
    }

    [SubscribeLocalEvent]
    private void OnEntityRelationDelete(Entity<EntityRelationsComponent> ent, ref ComponentShutdown args)
    {
        ClearRelations(ent.AsNullable());
    }
}

/// <summary>
/// Raised by an entity on each of its relations, so the subscribers can clear the relation
/// to prevent storing a reference to an invalid EntityUid.
/// </summary>
/// <param name="Relation">
/// A relation that is about to become invalid.
/// After the event is handled, it has to be removed from all fields of the handler component.
/// </param>
[ByRefEvent]
public readonly record struct EntityRelationDeleteEvent(EntityRelation Relation);
