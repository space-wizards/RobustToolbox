using System.Collections.Generic;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    /// <inheritdoc/>
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

        relation.Entity = entity;

        entityRelations.Relations.Add(new EntityRelation(owner));
        owner.Comp.Relations.Add(relation);
    }

    /// <inheritdoc/>
    public void SetRelations(Entity<EntityRelationsComponent?> owner, List<EntityRelation> relations, List<EntityUid> entities)
    {
        foreach (var entity in entities)
        {
            var relation = EntityRelation.Null;
            SetRelation(owner, ref relation, entity);
            relations.Add(relation);
        }
    }

    /// <inheritdoc/>
    public void SetRelations(Entity<EntityRelationsComponent?> owner, HashSet<EntityRelation> relations, HashSet<EntityUid> entities)
    {
        foreach (var entity in entities)
        {
            var relation = EntityRelation.Null;
            SetRelation(owner, ref relation, entity);
            relations.Add(relation);
        }
    }

    /// <inheritdoc/>
    public void ClearRelations(EntityRelation relation, EntityRelationsComponent? relations = null)
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

    /// <inheritdoc/>
    public void ClearRelations(Entity<EntityRelationsComponent?> ent)
    {
        var relation = new EntityRelation(ent.Owner);
        ClearRelations(relation, ent.Comp);
    }

    /// <inheritdoc/>
    public void ClearRelation(Entity<EntityRelationsComponent?> ent, ref EntityRelation relation)
    {
        if (relation.Entity == null)
            return;

        if (!_relationsQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        var relationComp = _relationsQuery.Comp(relation.Entity.Value);

        ent.Comp.Relations.Remove(relation);
        if (ent.Comp.Relations.Count == 0 && ent.Comp.LifeStage < ComponentLifeStage.Stopping)
            RemoveComponent(ent.Owner, ent.Comp);

        relationComp.Relations.Remove(new EntityRelation(ent.Owner));
        if (relationComp.Relations.Count == 0 && relationComp.LifeStage < ComponentLifeStage.Stopping)
            RemoveComponent(relation.Entity.Value, relationComp);

        relation = EntityRelation.Null;
    }

    /// <inheritdoc/>
    public void ClearRelation(Entity<EntityRelationsComponent?> ent, ref EntityRelation? relation)
    {
        if (relation == null)
            return;

        var copy = relation.Value;
        ClearRelation(ent, ref copy);
        relation = null;
    }

    /// <inheritdoc/>
    public void ClearRelation(Entity<EntityRelationsComponent?> ent, List<EntityRelation> relations)
    {
        foreach (var relation in relations)
        {
            var copy = relation;
            ClearRelation(ent, ref copy);
        }
        relations.Clear();
    }

    /// <inheritdoc/>
    public void ClearRelation(Entity<EntityRelationsComponent?> ent, HashSet<EntityRelation> relations)
    {
        foreach (var relation in relations)
        {
            var copy = relation;
            ClearRelation(ent, ref copy);
        }
        relations.Clear();
    }

    /// <inheritdoc/>
    public void ClearRelation<T>(Entity<EntityRelationsComponent?> ent, Dictionary<EntityRelation, T> relations)
    {
        foreach (var relation in relations.Keys)
        {
            var copy = relation;
            ClearRelation(ent, ref copy);
        }
        relations.Clear();
    }

    /// <inheritdoc/>
    public void ClearRelation<T>(Entity<EntityRelationsComponent?> ent, Dictionary<T, EntityRelation> relations) where T : notnull
    {
        foreach (var (key, relation) in relations)
        {
            var copy = relation;
            ClearRelation(ent, ref copy);
            relations[key] = copy;
        }
    }
}
