using System.Collections.Generic;
using Robust.Shared.Collections;

namespace Robust.Shared.GameObjects;

/// <summary>
/// A system that manually handles <see cref="EntityRelationsComponent"/>'s entity relation events,
/// since it can't modify itself without copying the set.
/// </summary>
public sealed partial class EntityRelationsSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnEntityRelationDelete(Entity<EntityRelationsComponent> ent, ref EntityTerminatingEvent args)
    {
        ClearRelations(ent.AsNullable());
    }

    [SubscribeLocalEvent]
    private static void OnEntityRelationDelete(Entity<EntityRelationsComponent> ent, ref EntityRelationDeleteEvent args)
    {
        ent.Comp.Relations.Remove(args.Relation);
    }

    [SubscribeLocalEvent]
    private void OnEntityRelationDelete(Entity<EntityRelationsComponent> ent, ref ComponentRelationsRemove args)
    {
        ClearRelationCopy(ent.AsNullable(), ent.Comp.Relations);
    }

    /// <summary>
    /// Copies a set of relations and clears all relations.
    /// This is used to prevent <see cref="EntityRelationsComponent"/>
    /// from modifying its own set during enumeration.
    /// </summary>
    private void ClearRelationCopy(Entity<EntityRelationsComponent?> ent, List<EntityRelation> relations)
    {
        var copyList = new ValueList<EntityRelation>(relations);
        foreach (var relation in copyList)
        {
            var copy = relation;
            ClearRelation(ent, ref copy);
        }
        relations.Clear();
    }
}

/// <summary>
/// Raised by some entity on each of its relations to inform them about removing
/// </summary>
/// <param name="Relation">A relation that is about to become invalid.</param>
[ByRefEvent]
public readonly record struct EntityRelationDeleteEvent(EntityRelation Relation);
