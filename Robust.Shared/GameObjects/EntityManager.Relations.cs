using System.Collections.Generic;
using Robust.Shared.Collections;
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
            if (relation.Entity != null)
                ClearRelation(owner, ref relation, dirty);

            relation.Entity = null;
            return;
        }

        var ent = entity.Value;

        // TODO add EnsureComp methods to EntityQuery
        if (!_relationsQuery.Resolve(ent.Owner, ref ent.Comp, false))
            EnsureComponent<EntityRelationsComponent>(ent.Owner, out ent.Comp);

        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent<EntityRelationsComponent>(owner.Owner, out owner.Comp);

        if (relation.Entity != null)
            ClearRelation(owner, ref relation, dirty);

        relation.Entity = entity;

        ent.Comp.Relations.Add(new EntityRelation(owner));
        owner.Comp.Relations.Add(relation);

        if (!dirty)
            return;

        DirtyRelations(ent!);
        DirtyRelations(owner!);
    }

    /// <inheritdoc/>
    public void SetRelation(
        Entity<EntityRelationsComponent?> owner,
        ref EntityRelation? relation,
        Entity<EntityRelationsComponent?>? entity,
        bool dirty = true)
    {
        var copy = relation ?? EntityRelation.Null;
        SetRelation(owner, ref copy, entity, dirty);
        relation = copy;
    }

    /// <inheritdoc/>
    public void AddRelation(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        Entity<EntityRelationsComponent?>? entity,
        bool dirty = true)
    {
        var copy = EntityRelation.Null;
        SetRelation(owner, ref copy, entity, dirty);
        relations.Add(copy);
    }

    /// <inheritdoc/>
    public void AddRelation(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        Entity<EntityRelationsComponent?>? entity,
        bool dirty = true)
    {
        var copy = EntityRelation.Null;
        SetRelation(owner, ref copy, entity, dirty);
        relations.Add(copy);
    }

    /// <inheritdoc/>
    public void AddRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Entity<EntityRelationsComponent?>? entity,
        T value,
        bool dirty = true)
    {
        var copy = EntityRelation.Null;
        SetRelation(owner, ref copy, entity, dirty);
        relations.Add(copy, value);
    }

    /// <inheritdoc/>
    public void AddRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Entity<EntityRelationsComponent?>? entity,
        T key,
        bool dirty = true) where T : notnull
    {
        var copy =  EntityRelation.Null;
        SetRelation(owner, ref copy, entity, dirty);
        relations.Add(key, copy);
    }

    /// <inheritdoc/>
    public void AddRelations(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        List<EntityUid> entities,
        bool dirty = true)
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
    public void AddRelations(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        HashSet<EntityUid> entities,
        bool dirty = true)
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
    public void AddRelations(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        bool dirty = true,
        params Entity<EntityRelationsComponent?>?[] entities)
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
    public void AddRelations(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        bool dirty = true,
        params Entity<EntityRelationsComponent?>?[] entities)
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
    public void AddRelations<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Dictionary<EntityUid, T> entities,
        bool dirty = true)
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
    public void AddRelations<T>(
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
    public void SetRelations(Entity<EntityRelationsComponent?> owner, List<EntityRelation> relations, List<EntityUid> entities, bool dirty = true)
    {
        ClearRelations(owner, relations, false);
        AddRelations(owner, relations, entities, dirty);
    }

    /// <inheritdoc/>
    public void SetRelations(Entity<EntityRelationsComponent?> owner, HashSet<EntityRelation> relations, HashSet<EntityUid> entities, bool dirty = true)
    {
        ClearRelations(owner, relations, false);
        AddRelations(owner, relations, entities, dirty);
    }

    /// <inheritdoc/>
    public void SetRelations<T>(Entity<EntityRelationsComponent?> owner, Dictionary<EntityRelation, T> relations, Dictionary<EntityUid, T> entities, bool dirty = true)
    {
        ClearRelations(owner, relations, false);
        AddRelations(owner, relations, entities, dirty);
    }

    /// <inheritdoc/>
    public void SetRelations<T>(Entity<EntityRelationsComponent?> owner, Dictionary<T, EntityRelation> relations, Dictionary<T, EntityUid> entities, bool dirty = true) where T : notnull
    {
        ClearRelations(owner, relations, false);
        AddRelations(owner, relations, entities, dirty);
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
        relationComp.Relations.Remove(new EntityRelation(ent.Owner));

        if (dirty)
        {
            DirtyRelations(ent!);
            DirtyRelations((relation.Entity.Value, relationComp));
        }

        RemoveRelationCompIfEmpty(ent);
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
    public void ClearRelations(Entity<EntityRelationsComponent?> ent, List<EntityRelation> relations, bool dirty = true)
    {
        foreach (var relation in relations)
        {
            var copy = relation;
            ClearRelation(ent, ref copy);
        }
        relations.Clear();
    }

    /// <inheritdoc/>
    public void ClearRelations(Entity<EntityRelationsComponent?> ent, HashSet<EntityRelation> relations, bool dirty = true)
    {
        foreach (var relation in relations)
        {
            var copy = relation;
            ClearRelation(ent, ref copy);
        }
        relations.Clear();
    }

    /// <inheritdoc/>
    public void ClearRelations<T>(Entity<EntityRelationsComponent?> ent, Dictionary<EntityRelation, T> relations, bool dirty = true)
    {
        foreach (var relation in relations.Keys)
        {
            var copy = relation;
            ClearRelation(ent, ref copy);
        }
        relations.Clear();
    }

    /// <inheritdoc/>
    public void ClearRelations<T>(Entity<EntityRelationsComponent?> ent, Dictionary<T, EntityRelation> relations, bool dirty = true) where T : notnull
    {
        foreach (var (key, relation) in relations)
        {
            var copy = relation;
            ClearRelation(ent, ref copy);
            relations[key] = copy;
        }
    }

    /// <inheritdoc/>
    public void ClearRelation(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity,
        bool dirty = true)
    {
        var toRemove = new ValueList<EntityRelation>(relations.Count);
        foreach (var relation in relations)
        {
            if (relation.Entity != entity.Owner)
                continue;

            var copy = relation;
            ClearRelation(owner, ref copy);
            toRemove.Add(relation);
        }

        foreach (var remove in toRemove)
        {
            relations.Remove(remove);
        }
    }

    /// <inheritdoc/>
    public void ClearRelation(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity,
        bool dirty = true)
    {
        EntityRelation? toRemove = null;
        foreach (var relation in relations)
        {
            if (relation.Entity != entity.Owner)
                continue;

            var copy = relation;
            ClearRelation(owner, ref copy);
            toRemove = relation;
            break;
        }

        if (toRemove != null)
            relations.Remove(toRemove.Value);
    }

    /// <inheritdoc/>
    public void ClearRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Entity<EntityRelationsComponent?> entity,
        bool dirty = true)
    {
        EntityRelation? toRemove = null;
        foreach (var relation in relations.Keys)
        {
            if (relation.Entity != entity.Owner)
                continue;

            var copy = relation;
            ClearRelation(owner, ref copy);
            toRemove = relation;
            break;
        }

        if (toRemove != null)
            relations.Remove(toRemove.Value);
    }

    /// <inheritdoc/>
    public void ClearRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity,
        bool dirty = true) where T : notnull
    {
        var toRemove = new ValueList<T>(relations.Count);
        foreach (var (key, relation) in relations)
        {
            if (relation.Entity != entity.Owner)
                continue;

            var copy = relation;
            ClearRelation(owner, ref copy);
            toRemove.Add(key);
        }

        foreach (var remove in toRemove)
        {
            relations.Remove(remove);
        }
    }

    /// <inheritdoc/>
    public bool HasRelation(List<EntityRelation> relations, Entity<EntityRelationsComponent?>? entity)
    {
        return _relationsQuery.HasComp(entity) && relations.Contains(new EntityRelation(entity));
    }

    /// <inheritdoc/>
    public bool HasRelation(HashSet<EntityRelation> relations, Entity<EntityRelationsComponent?>? entity)
    {
        return _relationsQuery.HasComp(entity) && relations.Contains(new EntityRelation(entity));
    }

    /// <inheritdoc/>
    public bool HasRelation<T>(Dictionary<EntityRelation, T> relations, Entity<EntityRelationsComponent?>? entity)
    {
        return _relationsQuery.HasComp(entity) && relations.ContainsKey(new EntityRelation(entity));
    }

    /// <inheritdoc/>
    public bool HasRelation<T>(Dictionary<T, EntityRelation> relations, Entity<EntityRelationsComponent?>? entity) where T : notnull
    {
        return _relationsQuery.HasComp(entity) && relations.ContainsValue(new EntityRelation(entity));
    }

    private void RemoveRelationCompIfEmpty(Entity<EntityRelationsComponent?> ent)
    {
        // Don't log missing here because on flushing all entities this will fail
        // even though the relation actually existed.
        if (!_relationsQuery.Resolve(ent.Owner, ref ent.Comp, false))
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
