using JetBrains.Annotations;
using Robust.Shared.IoC;

namespace Robust.Shared.GameObjects;

/// <summary>
/// A system that manually handles <see cref="EntityRelationsComponent"/>'s entity relation events,
/// since it can't modify itself without copying the set.
/// </summary>
public sealed partial class EntityRelationsSystem : EntitySystem
{
    [Dependency] private EntityQuery<EntityRelationsComponent> _relationsQuery = default!;

    /// <summary>
    /// Sets whether the component should be removed when it's empty or not.
    /// </summary>
    /// <param name="ent">The target entity with relations.</param>
    /// <param name="value">The value to set. If true,
    /// the component will be removed when this entity isn't related to anything.</param>
    [PublicAPI]
    public void SetRemoveOnEmpty(Entity<EntityRelationsComponent?> ent, bool value)
    {
        if (!_relationsQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.RemoveOnEmpty = value;
        DirtyField(ent, nameof(ent.Comp.RemoveOnEmpty));
    }

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

/// <summary>
/// An event that is raised on the entity owning an <see cref="EntityRelationsComponent"/> that is currently shutting down.
/// Sets all relation fields in the component to null.
/// This is raised if the owning entity isn't terminating and the component itself was removed explicitly.
/// </summary>
[ByRefEvent]
public readonly record struct EntityRelationShutdownEvent;
