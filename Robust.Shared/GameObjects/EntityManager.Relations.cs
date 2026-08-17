using System.Collections.Generic;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    /// <inheritdoc/>
    public void SetRelation(
        Entity<EntityRelationsComponent?> owner,
        ref EntityRelation relation,
        Entity<EntityRelationsComponent?>? entity,
        bool dirty = true)
    {
        DebugTools.Assert(owner.Owner != entity,
            $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

        if (!entity.HasValue)
        {
            relation.Entity = null;
            return;
        }

        var ent = entity.Value;

        // TODO add EnsureComp methods to EntityQuery
        if (!_relationsQuery.Resolve(ent.Owner, ref ent.Comp, false))
            EnsureComponent<EntityRelationsComponent>(ent.Owner, out ent.Comp);

        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent<EntityRelationsComponent>(owner.Owner, out owner.Comp);

        relation.Entity = entity;

        ent.Comp.Relations.Add(new EntityRelation(owner));
        owner.Comp.Relations.Add(relation);

        if (!dirty)
            return;

        DirtyRelations(ent!);
        DirtyRelations(owner!);
    }

    /// <inheritdoc/>
    public void SetRelation(Entity<EntityRelationsComponent?> owner, ref EntityRelation? relation, Entity<EntityRelationsComponent?>? entity, bool dirty = true)
    {
        var copy = relation ?? EntityRelation.Null;
        SetRelation(owner, ref copy, entity, dirty);
        relation = copy;
    }

    /// <inheritdoc/>
    public void SetRelations(Entity<EntityRelationsComponent?> owner, List<EntityRelation> relations, List<EntityUid> entities, bool dirty = true)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent<EntityRelationsComponent>(owner.Owner, out owner.Comp);

        foreach (var entity in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelation(owner, ref relation, entity, false);
            relations.Add(relation);
        }

        if (!dirty)
            return;

        foreach (var relation in relations)
        {
            if (!_relationsQuery.TryComp(relation.Entity, out var entityRelations))
                continue;

            DirtyRelations((relation.Entity.Value, entityRelations));
        }

        DirtyRelations(owner!);
    }

    /// <inheritdoc/>
    public void SetRelations(Entity<EntityRelationsComponent?> owner, HashSet<EntityRelation> relations, HashSet<EntityUid> entities, bool dirty = true)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent<EntityRelationsComponent>(owner.Owner, out owner.Comp);

        foreach (var entity in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelation(owner, ref relation, entity, false);
            relations.Add(relation);
        }

        if (!dirty)
            return;

        foreach (var relation in relations)
        {
            if (!_relationsQuery.TryComp(relation.Entity, out var entityRelations))
                continue;

            DirtyRelations((relation.Entity.Value, entityRelations));
        }

        DirtyRelations(owner!);
    }

    /// <inheritdoc/>
    public void SetRelations<T>(Entity<EntityRelationsComponent?> owner, Dictionary<EntityRelation, T> relations, Dictionary<EntityUid, T> entities, bool dirty = true)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent<EntityRelationsComponent>(owner.Owner, out owner.Comp);

        foreach (var (entity, value) in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelation(owner, ref relation, entity, false);
            relations.Add(relation, value);
        }

        if (!dirty)
            return;

        foreach (var relation in relations.Keys)
        {
            if (!_relationsQuery.TryComp(relation.Entity, out var entityRelations))
                continue;

            DirtyRelations((relation.Entity.Value, entityRelations));
        }

        DirtyRelations(owner!);
    }

    /// <inheritdoc/>
    public void SetRelations<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Dictionary<T, EntityUid> entities,
        bool dirty = true) where T : notnull
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent<EntityRelationsComponent>(owner.Owner, out owner.Comp);

        foreach (var (key, entity) in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelation(owner, ref relation, entity, false);
            relations.Add(key, relation);
        }

        if (!dirty)
            return;

        foreach (var relation in relations.Values)
        {
            if (!_relationsQuery.TryComp(relation.Entity, out var entityRelations))
                continue;

            DirtyRelations((relation.Entity.Value, entityRelations));
        }

        DirtyRelations(owner!);
    }

    /// <inheritdoc/>
    public void ClearRelations(EntityRelation relation, EntityRelationsComponent? relations = null, bool dirty = true)
    {
        if (relation.Entity == null)
            return;

        if (!_relationsQuery.Resolve(relation.Entity.Value, ref relations))
            return;

        var ev = new EntityRelationDeleteEvent(relation);
        foreach (var related in relations.Relations)
        {
            if (!related.Entity.HasValue)
                continue;

            EventBus.RaiseLocalEvent(related.Entity.Value, ref ev);
            RemoveRelationCompIfEmpty(related.Entity.Value);
        }

        if (MetaQuery.Comp(relation.Entity.Value).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        var selfEv = new EntityRelationShutdownEvent();
        EventBus.RaiseLocalEvent(relation.Entity.Value, ref selfEv);
    }

    /// <inheritdoc/>
    public void ClearRelations(Entity<EntityRelationsComponent?> ent, bool dirty = true)
    {
        var relation = new EntityRelation(ent.Owner);
        ClearRelations(relation, ent.Comp);
    }

    /// <inheritdoc/>
    public void ClearRelation(Entity<EntityRelationsComponent?> ent, ref EntityRelation relation, bool dirty = true)
    {
        if (relation.Entity == null)
            return;

        if (!_relationsQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        var relationComp = _relationsQuery.Comp(relation.Entity.Value);

        ent.Comp.Relations.Remove(relation);
        RemoveRelationCompIfEmpty(ent);

        relationComp.Relations.Remove(new EntityRelation(ent.Owner));
        RemoveRelationCompIfEmpty((relation.Entity.Value, relationComp));

        relation = EntityRelation.Null;
    }

    /// <inheritdoc/>
    public void ClearRelation(Entity<EntityRelationsComponent?> ent, ref EntityRelation? relation, bool dirty = true)
    {
        if (relation == null)
            return;

        var copy = relation.Value;
        ClearRelation(ent, ref copy);
        relation = null;
    }

    /// <inheritdoc/>
    public void ClearRelation(Entity<EntityRelationsComponent?> ent, List<EntityRelation> relations, bool dirty = true)
    {
        foreach (var relation in relations)
        {
            var copy = relation;
            ClearRelation(ent, ref copy);
        }
        relations.Clear();
    }

    /// <inheritdoc/>
    public void ClearRelation(Entity<EntityRelationsComponent?> ent, HashSet<EntityRelation> relations, bool dirty = true)
    {
        foreach (var relation in relations)
        {
            var copy = relation;
            ClearRelation(ent, ref copy);
        }
        relations.Clear();
    }

    /// <inheritdoc/>
    public void ClearRelation<T>(Entity<EntityRelationsComponent?> ent, Dictionary<EntityRelation, T> relations, bool dirty = true)
    {
        foreach (var relation in relations.Keys)
        {
            var copy = relation;
            ClearRelation(ent, ref copy);
        }
        relations.Clear();
    }

    /// <inheritdoc/>
    public void ClearRelation<T>(Entity<EntityRelationsComponent?> ent, Dictionary<T, EntityRelation> relations, bool dirty = true) where T : notnull
    {
        foreach (var (key, relation) in relations)
        {
            var copy = relation;
            ClearRelation(ent, ref copy);
            relations[key] = copy;
        }
    }

    private void RemoveRelationCompIfEmpty(Entity<EntityRelationsComponent?> ent)
    {
        if (!_relationsQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        if (!ent.Comp.RemoveOnEmpty)
            return;

        if (ent.Comp.Relations.Count == 0 && ent.Comp.LifeStage < ComponentLifeStage.Stopping)
            RemoveComponent(ent.Owner, ent.Comp);
    }

    private void DirtyRelations(Entity<EntityRelationsComponent> ent)
    {
        DirtyField(ent.Owner, ent.Comp, nameof(EntityRelationsComponent.Relations));
    }
}
