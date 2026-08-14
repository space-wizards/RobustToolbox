using JetBrains.Annotations;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    /// <summary>
    /// Assign an entity to have a relation.
    /// </summary>
    /// <param name="owner">
    /// Owner of the provided <see cref="relation"/>.
    /// An event will be raised to the owner when the specified <see cref="entity"/> gets deleted.
    /// </param>
    /// <param name="relation">
    /// The relation struct will hold the reference to <see cref="entity"/>.
    /// </param>
    /// <param name="entity">
    /// An entity that will become related to <see cref="owner"/> and stored in the <see cref="relation"/>.
    /// </param>
    [PublicAPI]
    public void SetRelation(Entity<EntityRelationsComponent?> owner, ref EntityRelation relation, EntityUid? entity)
    {
        if (!entity.HasValue)
        {
            relation.Entity = null;
            return;
        }

        var entityRelations = EnsureComponent<EntityRelationsComponent>(entity.Value);

        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent<EntityRelationsComponent>(owner.Owner, out owner.Comp);

        entityRelations.Relations.Add(relation);
        owner.Comp.Relations.Add(relation);

        relation.Entity = entity;
    }

    /// <summary>
    /// Manually removes all relations from an <see cref="EntityRelation"/>.
    /// </summary>
    [PublicAPI]
    public void ClearRelation(EntityRelation relation, EntityRelationsComponent? relations = null)
    {
        if (relation.Entity == null)
            return; // Already cleared

        if (!_relationsQuery.Resolve(relation.Entity.Value, ref relations))
            return;

        var ev = new EntityRelationDeleteEvent(relation);
        foreach (var related in relations.Relations)
        {
            if (!related.Entity.HasValue)
                continue;

            EventBus.RaiseLocalEvent(related.Entity.Value, ref ev);
        }
    }

    /// <summary>
    /// Manually removes all relations from an entity with <see cref="EntityRelationsComponent"/>.
    /// </summary>
    [PublicAPI]
    public void ClearRelation(Entity<EntityRelationsComponent?> ent)
    {
        var relation = new EntityRelation { Entity = ent.Owner };
        ClearRelation(relation, ent.Comp);
    }
}
