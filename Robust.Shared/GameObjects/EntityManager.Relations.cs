using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Collections;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    /// <inheritdoc/>
    public void SetRelation(
        Entity<EntityRelationsComponent?> owner,
        ref EntityRelation relation,
        Entity<EntityRelationsComponent?>? entity)
    {
        SetRelationInternal(owner, ref relation, entity);

        DirtyRelations(owner);
        if (entity is { Comp: not null })
            DirtyRelations(entity.Value);
    }

    /// <summary>
    /// A version of <see cref="SetRelation(Entity{EntityRelationsComponent?}, ref EntityRelation, Entity{EntityRelationsComponent?}?)"/>
    /// that doesn't dirty the 2 <see cref="EntityRelationsComponent"/>s.
    /// </summary>
    private void SetRelationInternal(
        Entity<EntityRelationsComponent?> owner,
        ref EntityRelation relation,
        Entity<EntityRelationsComponent?>? entity)
    {
        DebugTools.Assert(owner.Owner != entity,
            $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

        if (!entity.HasValue)
        {
            if (relation.Entity != null)
                ClearRelation(owner, ref relation);

            relation.Entity = null;
            return;
        }

        var ent = entity.Value;

        // TODO add EnsureComp methods to EntityQuery
        if (!_relationsQuery.Resolve(ent.Owner, ref ent.Comp, false))
            EnsureComponent(ref ent);

        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent(ref owner);

        if (relation.Entity != null)
            ClearRelation(owner, ref relation);

        relation.Entity = entity;

        ent.Comp!.Relations.Add(new EntityRelation(owner));
        owner.Comp!.Relations.Add(relation);
    }

    /// <inheritdoc/>
    public void SetRelation(
        Entity<EntityRelationsComponent?> owner,
        ref EntityRelation? relation,
        Entity<EntityRelationsComponent?>? entity)
    {
        var copy = relation ?? EntityRelation.Null;
        SetRelation(owner, ref copy, entity);
        relation = copy;
    }

    /// <inheritdoc/>
    public void AddRelation(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        Entity<EntityRelationsComponent?>? entity)
    {
        var copy = EntityRelation.Null;
        SetRelation(owner, ref copy, entity);
        relations.Add(copy);
    }

    /// <inheritdoc/>
    public void AddRelation(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        Entity<EntityRelationsComponent?>? entity)
    {
        var copy = EntityRelation.Null;
        SetRelation(owner, ref copy, entity);
        relations.Add(copy);
    }

    /// <inheritdoc/>
    public void AddRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Entity<EntityRelationsComponent?>? entity,
        T value)
    {
        var copy = EntityRelation.Null;
        SetRelation(owner, ref copy, entity);
        relations.Add(copy, value);
    }

    /// <inheritdoc/>
    public void AddRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Entity<EntityRelationsComponent?>? entity,
        T key) where T : notnull
    {
        var copy = EntityRelation.Null;
        SetRelation(owner, ref copy, entity);
        relations.Add(key, copy);
    }

    /// <inheritdoc/>
    public void AddRelations(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        List<EntityUid> entities)
    {
        AddRelationsInternal(owner, relations, entities);

        foreach (var relation in relations)
        {
            if (!_relationsQuery.TryComp(relation.Entity, out var entityRelations))
                continue;

            DirtyRelations((relation.Entity.Value, entityRelations));
        }

        DirtyRelations(owner);
    }

    private void AddRelationsInternal(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        List<EntityUid> entities)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent(ref owner);

        foreach (var entity in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelationInternal(owner, ref relation, entity);
            relations.Add(relation);
        }
    }

    /// <inheritdoc/>
    public void AddRelations(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        HashSet<EntityUid> entities)
    {
        AddRelationsInternal(owner, relations, entities);

        foreach (var relation in relations)
        {
            if (!_relationsQuery.TryComp(relation.Entity, out var entityRelations))
                continue;

            DirtyRelations((relation.Entity.Value, entityRelations));
        }

        DirtyRelations(owner);
    }

    private void AddRelationsInternal(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        HashSet<EntityUid> entities)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent(ref owner);

        foreach (var entity in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelationInternal(owner, ref relation, entity);
            relations.Add(relation);
        }
    }

    /// <inheritdoc/>
    public void AddRelations(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        params Entity<EntityRelationsComponent?>?[] entities)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent(ref owner);

        foreach (var entity in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelation(owner, ref relation, entity);
            relations.Add(relation);
        }

        foreach (var relation in entities)
        {
            if (relation == null)
                continue;

            var relEnt = relation.Value;
            if (!_relationsQuery.Resolve(relEnt.Owner, ref relEnt.Comp, false))
                continue;

            DirtyRelations(relEnt);
        }

        DirtyRelations(owner);
    }

    /// <inheritdoc/>
    public void AddRelations(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        params Entity<EntityRelationsComponent?>?[] entities)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent(ref owner);

        foreach (var entity in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelationInternal(owner, ref relation, entity);
            relations.Add(relation);
        }

        foreach (var relation in entities)
        {
            if (relation == null)
                continue;

            var relEnt = relation.Value;
            if (!_relationsQuery.Resolve(relEnt.Owner, ref relEnt.Comp, false))
                continue;

            DirtyRelations(relEnt);
        }

        DirtyRelations(owner);
    }

    /// <inheritdoc/>
    public void AddRelations<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Dictionary<EntityUid, T> entities)
    {
        AddRelationsInternal(owner, relations, entities);

        foreach (var relation in entities.Keys)
        {
            if (!_relationsQuery.TryComp(relation, out var entityRelations))
                continue;

            DirtyRelations((relation, entityRelations));
        }

        DirtyRelations(owner);
    }

    private void AddRelationsInternal<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Dictionary<EntityUid, T> entities)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent(ref owner);

        foreach (var (entity, value) in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelationInternal(owner, ref relation, entity);
            relations.Add(relation, value);
        }
    }

    /// <inheritdoc/>
    public void AddRelations<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Dictionary<T, EntityUid> entities) where T : notnull
    {
        AddRelationsInternal(owner, relations, entities);

        foreach (var relation in entities.Values)
        {
            if (!_relationsQuery.TryComp(relation, out var entityRelations))
                continue;

            DirtyRelations((relation, entityRelations));
        }

        DirtyRelations(owner);
    }

    private void AddRelationsInternal<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Dictionary<T, EntityUid> entities) where T : notnull
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent(ref owner);

        foreach (var (key, entity) in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelationInternal(owner, ref relation, entity);
            relations.Add(key, relation);
        }

        foreach (var relation in relations.Values)
        {
            if (!_relationsQuery.TryComp(relation.Entity, out var entityRelations))
                continue;

            DirtyRelations((relation.Entity.Value, entityRelations));
        }

        DirtyRelations(owner);
    }

    /// <inheritdoc/>
    public void SetRelations(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        List<EntityUid> entities)
    {
        ClearRelationsInternal(owner, relations);
        RemoveRelationCompsIfEmpty(relations);
        AddRelationsInternal(owner, relations, entities);
        DirtyRelationsCollection(relations);
    }

    /// <inheritdoc/>
    public void SetRelations(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        HashSet<EntityUid> entities)
    {
        ClearRelationsInternal(owner, relations);
        AddRelationsInternal(owner, relations, entities);
        RemoveRelationCompsIfEmpty(relations);
        DirtyRelationsCollection(relations);
    }

    /// <inheritdoc/>
    public void SetRelations<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Dictionary<EntityUid, T> entities)
    {
        ClearRelationsInternal(owner, relations);
        AddRelationsInternal(owner, relations, entities);
        RemoveRelationCompsIfEmpty(relations);
        DirtyRelationsCollection(relations);
    }

    /// <inheritdoc/>
    public void SetRelations<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Dictionary<T, EntityUid> entities)
        where T : notnull
    {
        ClearRelationsInternal(owner, relations);
        AddRelationsInternal(owner, relations, entities);
        RemoveRelationCompsIfEmpty(relations);
        DirtyRelationsCollection(relations);
    }

    /// <inheritdoc/>
    public void ClearRelations(EntityRelation relation, EntityRelationsComponent? relations = null)
    {
        if (relation.Entity == null)
            return;

        if (!_relationsQuery.Resolve(relation.Entity.Value, ref relations))
            return;

        ClearRelationsInternal((relation.Entity.Value, relations));
        if (relation.Entity != null)
            DirtyRelations((relation.Entity.Value, relations));
    }

    private void ClearRelationsInternal(Entity<EntityRelationsComponent?> ent)
    {
        if (!_relationsQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        var ev = new EntityRelationDeleteEvent(new EntityRelation(ent.Owner));
        foreach (var related in ent.Comp.Relations)
        {
            if (!related.Entity.HasValue)
                continue;

            EventBus.RaiseLocalEvent(related.Entity.Value, ref ev);
            RemoveRelationCompIfEmpty(related.Entity.Value);
        }

        if (MetaQuery.Comp(ent.Owner).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        var selfEv = new EntityRelationShutdownEvent();
        EventBus.RaiseLocalEvent(ent.Owner, ref selfEv);
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
        ClearRelationInternal(ent, ref relation, out var relationEnt);
        RemoveRelationCompIfEmpty(ref ent);
        DirtyRelations(ent);

        if (relationEnt == null)
            return;

        var relEnt = relationEnt.Value;
        RemoveRelationCompIfEmpty(ref relEnt);
        DirtyRelations(relEnt);
    }

    private bool ClearRelationInternal(
        Entity<EntityRelationsComponent?> ent,
        ref EntityRelation relation,
        [NotNullWhen(true)] out Entity<EntityRelationsComponent?>? relationEnt)
    {
        relationEnt = null;
        if (relation.Entity == null)
            return false;

        if (!_relationsQuery.Resolve(ent.Owner, ref ent.Comp))
            return false;

        ent.Comp.Relations.Remove(relation);

        if (_relationsQuery.TryComp(relation.Entity.Value, out var relationComp))
        {
            relationComp.Relations.Remove(new EntityRelation(ent.Owner));
            relationEnt = (relation.Entity.Value, relationComp);
            relation = EntityRelation.Null;
            return true;
        }

        relation = EntityRelation.Null;
        return false;
    }

    private void ClearRelationInternal(
        Entity<EntityRelationsComponent?> ent,
        ref EntityRelation relation)
    {
        ClearRelationInternal(ent, ref relation, out _);
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
    public void ClearRelations(Entity<EntityRelationsComponent?> ent, List<EntityRelation> relations)
    {
        ClearRelationsInternal(ent, relations);
        RemoveRelationCompIfEmpty(ref ent);
        RemoveRelationCompsIfEmpty(relations);
        relations.Clear();
        DirtyRelations(ent);
    }

    /// <summary>
    /// Cuts a list of connections from an entity without modifying the list nor removing any components.
    /// </summary>
    private void ClearRelationsInternal(Entity<EntityRelationsComponent?> ent, List<EntityRelation> relations)
    {
        foreach (var relation in relations)
        {
            var copy = relation;
            ClearRelationInternal(ent, ref copy);
        }
    }

    /// <inheritdoc/>
    public void ClearRelations(Entity<EntityRelationsComponent?> ent, HashSet<EntityRelation> relations)
    {
        ClearRelationsInternal(ent, relations);
        RemoveRelationCompIfEmpty(ref ent);
        RemoveRelationCompsIfEmpty(relations);
        relations.Clear();
        DirtyRelations(ent);
    }

    /// <summary>
    /// Cuts a list of connections from an entity without modifying the list nor removing any components.
    /// </summary>
    private void ClearRelationsInternal(Entity<EntityRelationsComponent?> ent, HashSet<EntityRelation> relations)
    {
        foreach (var relation in relations)
        {
            var copy = relation;
            ClearRelationInternal(ent, ref copy);
        }
    }

    /// <inheritdoc/>
    public void ClearRelations<T>(Entity<EntityRelationsComponent?> ent, Dictionary<EntityRelation, T> relations)
    {
        ClearRelationsInternal(ent, relations);
        RemoveRelationCompIfEmpty(ref ent);
        RemoveRelationCompsIfEmpty(relations);
        relations.Clear();
        DirtyRelations(ent);
    }

    private void ClearRelationsInternal<T>(Entity<EntityRelationsComponent?> ent, Dictionary<EntityRelation, T> relations)
    {
        foreach (var relation in relations.Keys)
        {
            var copy = relation;
            ClearRelationInternal(ent, ref copy);
        }
        relations.Clear();
    }

    /// <inheritdoc/>
    public void ClearRelations<T>(Entity<EntityRelationsComponent?> ent, Dictionary<T, EntityRelation> relations) where T : notnull
    {
        ClearRelationsInternal(ent, relations);
        RemoveRelationCompIfEmpty(ref ent);
        RemoveRelationCompsIfEmpty(relations);
        foreach (var relation in relations.Keys)
        {
            relations[relation] = EntityRelation.Null;
        }
    }

    private void ClearRelationsInternal<T>(Entity<EntityRelationsComponent?> ent, Dictionary<T, EntityRelation> relations) where T : notnull
    {
        foreach (var relation in relations.Values)
        {
            var copy = relation;
            ClearRelationInternal(ent, ref copy);
        }
    }

    /// <inheritdoc/>
    public void ClearRelation(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp)
            || !_relationsQuery.Resolve(entity.Owner, ref entity.Comp))
            return;

        var relation = new EntityRelation(entity.Owner);
        foreach (var rel in relations)
        {
            if (rel != relation)
                continue;

            var copy = relation;
            ClearRelationInternal(owner, ref copy);
        }

        relations.RemoveAll(x => x == relation);

        DirtyRelations(owner);
        DirtyRelations(entity);
    }

    /// <inheritdoc/>
    public void ClearRelation(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp)
            || !_relationsQuery.Resolve(entity.Owner, ref entity.Comp))
            return;

        var relation = new EntityRelation(entity.Owner);
        if (!relations.Contains(relation))
            return;

        var copy = relation;
        ClearRelationInternal(owner, ref copy);
        relations.Remove(relation);

        RemoveRelationCompIfEmpty(ref owner);
        RemoveRelationCompIfEmpty(ref entity);
        DirtyRelations(owner);
        DirtyRelations(entity);
    }

    /// <inheritdoc/>
    public void ClearRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Entity<EntityRelationsComponent?> entity)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp)
            || !_relationsQuery.Resolve(entity.Owner, ref entity.Comp))
            return;

        var relation = new EntityRelation(entity.Owner);
        if (!relations.ContainsKey(relation))
            return;

        var copy = relation;
        ClearRelationInternal(owner, ref copy);
        relations.Remove(relation);

        RemoveRelationCompIfEmpty(ref owner);
        RemoveRelationCompIfEmpty(ref entity);
        DirtyRelations(owner);
        DirtyRelations(entity);
    }

    /// <inheritdoc/>
    public void ClearRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity,
        bool removeKey = false) where T : notnull
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp)
            || !_relationsQuery.Resolve(entity.Owner, ref entity.Comp))
            return;

        var toRemove = new ValueList<T>(relations.Count);
        foreach (var (key, relation) in relations)
        {
            if (relation.Entity != entity.Owner)
                continue;

            var copy = relation;
            ClearRelationInternal(owner, ref copy);
            toRemove.Add(key);
        }

        foreach (var remove in toRemove)
        {
            if (removeKey)
                relations.Remove(remove);
            else
                relations[remove] = EntityRelation.Null;
        }

        RemoveRelationCompIfEmpty(ref owner);
        RemoveRelationCompIfEmpty(ref entity);
        DirtyRelations(owner);
        DirtyRelations(entity);
    }

    /// <inheritdoc/>
    public bool HasRelation(List<EntityRelation> relations, EntityUid? entity)
    {
        DebugTools.Assert(_relationsQuery.HasComp(entity));
        return relations.Contains(new EntityRelation(entity));
    }

    /// <inheritdoc/>
    public bool HasRelation(HashSet<EntityRelation> relations, EntityUid? entity)
    {
        DebugTools.Assert(_relationsQuery.HasComp(entity));
        return relations.Contains(new EntityRelation(entity));
    }

    /// <inheritdoc/>
    public bool HasRelation<T>(Dictionary<EntityRelation, T> relations, EntityUid? entity)
    {
        DebugTools.Assert(_relationsQuery.HasComp(entity));
        return relations.ContainsKey(new EntityRelation(entity));
    }

    /// <inheritdoc/>
    public bool HasRelation<T>(Dictionary<T, EntityRelation> relations, EntityUid? entity) where T : notnull
    {
        DebugTools.Assert(_relationsQuery.HasComp(entity));
        return relations.ContainsValue(new EntityRelation(entity));
    }

    private bool RemoveRelationCompIfEmpty(Entity<EntityRelationsComponent?> ent)
    {
        RemoveRelationCompIfEmpty(ref ent);
        return ent.Comp == null;
    }

    private void RemoveRelationCompIfEmpty(ref Entity<EntityRelationsComponent?> ent)
    {
        // Don't log missing here because on flushing all entities this will fail
        // even though the relation actually existed.
        if (!_relationsQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!ent.Comp.RemoveOnEmpty)
            return;

        if (ent.Comp.Relations.Count != 0
            || ent.Comp.LifeStage >= ComponentLifeStage.Stopping)
            return;

        RemoveComponent(ent.Owner, ent.Comp);
        ent.Comp = null;
    }

    private void RemoveRelationCompsIfEmpty(List<EntityRelation> relations)
    {
        foreach (var rel in relations)
        {
            if (rel.Entity == null)
                continue;

            RemoveRelationCompIfEmpty(rel.Entity.Value);
        }
    }

    private void RemoveRelationCompsIfEmpty(HashSet<EntityRelation> relations)
    {
        foreach (var rel in relations)
        {
            if (rel.Entity == null)
                continue;

            RemoveRelationCompIfEmpty(rel.Entity.Value);
        }
    }

    private void RemoveRelationCompsIfEmpty<T>(Dictionary<EntityRelation, T> relations)
    {
        foreach (var rel in relations.Keys)
        {
            if (rel.Entity == null)
                continue;

            RemoveRelationCompIfEmpty(rel.Entity.Value);
        }
    }

    private void RemoveRelationCompsIfEmpty<T>(Dictionary<T, EntityRelation> relations) where T : notnull
    {
        foreach (var rel in relations.Values)
        {
            if (rel.Entity == null)
                continue;

            RemoveRelationCompIfEmpty(rel.Entity.Value);
        }
    }

    private void DirtyRelations(Entity<EntityRelationsComponent?> ent)
    {
        if (_relationsQuery.Resolve(ent.Owner, ref ent.Comp, false))
            Dirty(ent.Owner, ent.Comp);
    }

    private void DirtyRelationsCollection(List<EntityRelation> relations)
    {
        foreach (var rel in relations)
        {
            if (rel.Entity != null && _relationsQuery.TryComp(rel.Entity.Value, out var comp))
                DirtyField(rel.Entity.Value, comp, nameof(EntityRelationsComponent.Relations));
        }
    }

    private void DirtyRelationsCollection(HashSet<EntityRelation> relations)
    {
        foreach (var rel in relations)
        {
            if (rel.Entity != null && _relationsQuery.TryComp(rel.Entity.Value, out var comp))
                DirtyField(rel.Entity.Value, comp, nameof(EntityRelationsComponent.Relations));
        }
    }

    private void DirtyRelationsCollection<T>(Dictionary<EntityRelation, T> relations)
    {
        foreach (var rel in relations.Keys)
        {
            if (rel.Entity != null && _relationsQuery.TryComp(rel.Entity.Value, out var comp))
                DirtyField(rel.Entity.Value, comp, nameof(EntityRelationsComponent.Relations));
        }
    }

    private void DirtyRelationsCollection<T>(Dictionary<T, EntityRelation> relations) where T : notnull
    {
        foreach (var rel in relations.Values)
        {
            if (rel.Entity != null && _relationsQuery.TryComp(rel.Entity.Value, out var comp))
                DirtyField(rel.Entity.Value, comp, nameof(EntityRelationsComponent.Relations));
        }
    }
}
