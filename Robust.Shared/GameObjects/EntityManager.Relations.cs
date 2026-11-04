using System.Collections.Generic;
using System.Diagnostics;
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
        Entity<EntityRelationsComponent?>? entity,
        bool local = false)
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

        if (local)
        {
            ent.Comp!.LocalRelations.Add(new EntityRelation(owner));
            owner.Comp!.LocalRelations.Add(relation);
        }
        else
        {
            ent.Comp!.Relations.Add(new EntityRelation(owner));
            owner.Comp!.Relations.Add(relation);
        }
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
        List<EntityUid> entities,
        bool local = false)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent(ref owner);

        foreach (var entity in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelationInternal(owner, ref relation, entity, local);
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
        HashSet<EntityUid> entities,
        bool local = false)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent(ref owner);

        foreach (var entity in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelationInternal(owner, ref relation, entity, local);
            relations.Add(relation);
        }
    }

    /// <inheritdoc/>
    public void AddRelations(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        params Entity<EntityRelationsComponent?>?[] entities)
    {
        AddRelationsInternal(owner, relations, false, entities);

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

    private void AddRelationsInternal(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        bool local = false,
        params Entity<EntityRelationsComponent?>?[] entities)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent(ref owner);

        foreach (var entity in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelationInternal(owner, ref relation, entity, local);
            relations.Add(relation);
        }
    }

    /// <inheritdoc/>
    public void AddRelations(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        params Entity<EntityRelationsComponent?>?[] entities)
    {
        AddRelationsInternal(owner, relations, false, entities);

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

    private void AddRelationsInternal(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        bool local = false,
        params Entity<EntityRelationsComponent?>?[] entities)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent(ref owner);

        foreach (var entity in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelationInternal(owner, ref relation, entity, local);
            relations.Add(relation);
        }
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
        Dictionary<EntityUid, T> entities,
        bool local = false)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent(ref owner);

        foreach (var (entity, value) in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelationInternal(owner, ref relation, entity, local);
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
        Dictionary<T, EntityUid> entities,
        bool local = false) where T : notnull
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
            EnsureComponent(ref owner);

        foreach (var (key, entity) in entities)
        {
            DebugTools.Assert(owner.Owner != entity,
                $"Entity {ToPrettyString(owner.Owner)} attempted to set an {nameof(EntityRelation)} to itself!");

            var relation = EntityRelation.Null;
            SetRelationInternal(owner, ref relation, entity, local);
            relations.Add(key, relation);
        }
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

    private void ClearRelationsInternal(Entity<EntityRelationsComponent?> ent, bool local = false)
    {
        if (!_relationsQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        var ev = new EntityRelationDeleteEvent(new EntityRelation(ent.Owner));
        var list = local ? ent.Comp.LocalRelations : ent.Comp.Relations;
        foreach (var related in list)
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

    private void ClearRelationInternal(
        Entity<EntityRelationsComponent?> ent,
        ref EntityRelation relation,
        out Entity<EntityRelationsComponent?>? relationEnt,
        bool local = false)
    {
        relationEnt = null;
        if (relation.Entity == null)
            return;

        if (!_relationsQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        if (local)
            ent.Comp.LocalRelations.Remove(relation);
        else
            ent.Comp.Relations.Remove(relation);

        if (_relationsQuery.TryComp(relation.Entity.Value, out var relationComp))
        {
            if (local)
                relationComp.LocalRelations.Remove(new EntityRelation(ent.Owner));
            else
                relationComp.Relations.Remove(new EntityRelation(ent.Owner));

            relationEnt = (relation.Entity.Value, relationComp);
        }

        relation = EntityRelation.Null;
    }

    private void ClearRelationInternal(
        Entity<EntityRelationsComponent?> ent,
        ref EntityRelation relation,
        bool local = false)
    {
        ClearRelationInternal(ent, ref relation, out _, local);
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
    private void ClearRelationsInternal(Entity<EntityRelationsComponent?> ent, List<EntityRelation> relations, bool local = false)
    {
        foreach (var relation in relations)
        {
            var copy = relation;
            ClearRelationInternal(ent, ref copy, local);
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
    private void ClearRelationsInternal(Entity<EntityRelationsComponent?> ent, HashSet<EntityRelation> relations, bool local = false)
    {
        foreach (var relation in relations)
        {
            var copy = relation;
            ClearRelationInternal(ent, ref copy, local);
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

    private void ClearRelationsInternal<T>(Entity<EntityRelationsComponent?> ent, Dictionary<EntityRelation, T> relations, bool local = false)
    {
        foreach (var relation in relations.Keys)
        {
            var copy = relation;
            ClearRelationInternal(ent, ref copy, local);
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

    private void ClearRelationsInternal<T>(Entity<EntityRelationsComponent?> ent, Dictionary<T, EntityRelation> relations, bool local = false) where T : notnull
    {
        foreach (var relation in relations.Values)
        {
            var copy = relation;
            ClearRelationInternal(ent, ref copy, local);
        }
    }

    /// <inheritdoc/>
    public void ClearRelation(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity)
    {
        ClearRelationInternal(owner, relations, entity);
        RemoveRelationCompIfEmpty(ref owner);
        RemoveRelationCompIfEmpty(ref entity);
        DirtyRelations(owner);
        DirtyRelations(entity);
    }

    private void ClearRelationInternal(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity,
        bool local = false)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp)
            || !_relationsQuery.Resolve(entity.Owner, ref entity.Comp))
            return;

        var count = 0;
        var relation = new EntityRelation(entity.Owner);
        foreach (var rel in relations)
        {
            if (rel != relation)
                continue;

            var copy = relation;
            ClearRelationInternal(owner, ref copy, local);
            count++;
        }

        for (int i = 0; i < count; i++)
        {
            relations.Remove(relation);
        }
    }

    /// <inheritdoc/>
    public void ClearRelation(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity)
    {
        ClearRelationInternal(owner, relations, entity);
        RemoveRelationCompIfEmpty(ref owner);
        RemoveRelationCompIfEmpty(ref entity);
        DirtyRelations(owner);
        DirtyRelations(entity);
    }

    private void ClearRelationInternal(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity,
        bool local = false)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp)
            || !_relationsQuery.Resolve(entity.Owner, ref entity.Comp))
            return;

        var relation = new EntityRelation(entity.Owner);
        if (!relations.Contains(relation))
            return;

        var copy = relation;
        ClearRelationInternal(owner, ref copy, local);
        relations.Remove(relation);
    }

    /// <inheritdoc/>
    public void ClearRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Entity<EntityRelationsComponent?> entity)
    {
        ClearRelationInternal(owner, relations, entity);
        RemoveRelationCompIfEmpty(ref owner);
        RemoveRelationCompIfEmpty(ref entity);
        DirtyRelations(owner);
        DirtyRelations(entity);
    }

    private void ClearRelationInternal<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Entity<EntityRelationsComponent?> entity,
        bool local = false)
    {
        if (!_relationsQuery.Resolve(owner.Owner, ref owner.Comp)
            || !_relationsQuery.Resolve(entity.Owner, ref entity.Comp))
            return;

        var relation = new EntityRelation(entity.Owner);
        if (!relations.ContainsKey(relation))
            return;

        var copy = relation;
        ClearRelationInternal(owner, ref copy, local);
        relations.Remove(relation);
    }

    /// <inheritdoc/>
    public void ClearRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity,
        bool removeKey = false) where T : notnull
    {
        ClearRelationInternal(owner, relations, entity, removeKey);
        RemoveRelationCompIfEmpty(ref owner);
        RemoveRelationCompIfEmpty(ref entity);
        DirtyRelations(owner);
        DirtyRelations(entity);
    }

    private void ClearRelationInternal<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity,
        bool removeKey = false,
        bool local = false) where T : notnull
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
            ClearRelationInternal(owner, ref copy, local);
            toRemove.Add(key);
        }

        foreach (var remove in toRemove)
        {
            if (removeKey)
                relations.Remove(remove);
            else
                relations[remove] = EntityRelation.Null;
        }
    }

    // -- Local relations --

    public void SetLocalRelation(
        Entity<EntityRelationsComponent?> owner,
        ref EntityRelation relation,
        Entity<EntityRelationsComponent?>? entity)
    {
        AssertLocalRelations(owner, entity);
        SetRelationInternal(owner, ref relation, entity, true);
    }

    public void SetLocalRelation(
        Entity<EntityRelationsComponent?> owner,
        ref EntityRelation? relation,
        Entity<EntityRelationsComponent?>? entity)
    {
        AssertLocalRelations(owner, entity);
        var copy = relation ?? EntityRelation.Null;
        SetRelationInternal(owner, ref copy, entity, true);
        relation = copy;
    }

    public void AddLocalRelation(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        Entity<EntityRelationsComponent?>? entity)
    {
        AssertLocalRelations(owner, entity);
        var copy = EntityRelation.Null;
        SetRelationInternal(owner, ref copy, entity, true);
        relations.Add(copy);
    }

    public void AddLocalRelation(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        Entity<EntityRelationsComponent?>? entity)
    {
        AssertLocalRelations(owner, entity);
        var copy = EntityRelation.Null;
        SetRelationInternal(owner, ref copy, entity, true);
        relations.Add(copy);
    }

    public void AddLocalRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Entity<EntityRelationsComponent?>? entity,
        T value)
    {
        AssertLocalRelations(owner, entity);
        var copy = EntityRelation.Null;
        SetRelationInternal(owner, ref copy, entity, true);
        relations.Add(copy, value);
    }

    public void AddLocalRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Entity<EntityRelationsComponent?>? entity,
        T key) where T : notnull
    {
        AssertLocalRelations(owner, entity);
        var copy = EntityRelation.Null;
        SetRelationInternal(owner, ref copy, entity, true);
        relations.Add(key, copy);
    }

    public void AddLocalRelations(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        List<EntityUid> entities)
    {
        AssertLocalRelations(owner, entities);
        ClearRelationsInternal(owner, relations, true);
        AddRelationsInternal(owner, relations, entities, true);
    }

    public void AddLocalRelations(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        HashSet<EntityUid> entities)
    {
        AssertLocalRelations(owner, entities);
        ClearRelationsInternal(owner, relations, true);
        AddRelationsInternal(owner, relations, entities, true);
    }

    public void AddLocalRelations(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        params Entity<EntityRelationsComponent?>?[] entities)
    {
        AssertLocalRelations(owner, entities);
        ClearRelationsInternal(owner, relations, true);
        AddRelationsInternal(owner, relations, true, entities);
    }

    public void AddLocalRelations(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        params Entity<EntityRelationsComponent?>?[] entities)
    {
        AssertLocalRelations(owner, entities);
        ClearRelationsInternal(owner, relations, true);
        AddRelationsInternal(owner, relations, true, entities);
    }

    public void AddLocalRelations<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Dictionary<EntityUid, T> entities)
    {
        AssertLocalRelations(owner, entities);
        ClearRelationsInternal(owner, relations, true);
        AddRelationsInternal(owner, relations, entities, true);
    }

    public void AddLocalRelations<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Dictionary<T, EntityUid> entities) where T : notnull
    {
        AssertLocalRelations(owner, entities);
        ClearRelationsInternal(owner, relations, true);
        AddRelationsInternal(owner, relations, entities, true);
    }

    public void SetLocalRelations(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        List<EntityUid> entities)
    {
        ClearRelationsInternal(owner, relations, true);
        AddRelationsInternal(owner, relations, entities, true);
    }

    public void SetLocalRelations(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        HashSet<EntityUid> entities)
    {
        ClearRelationsInternal(owner, relations, true);
        AddRelationsInternal(owner, relations, entities, true);
    }

    public void SetLocalRelations<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Dictionary<EntityUid, T> entities)
    {
        ClearRelationsInternal(owner, relations, true);
        AddRelationsInternal(owner, relations, entities, true);
    }

    public void SetLocalRelations<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Dictionary<T, EntityUid> entities) where T : notnull
    {
        ClearRelationsInternal(owner, relations, true);
        AddRelationsInternal(owner, relations, entities, true);
    }

    public void ClearLocalRelations(EntityRelation relation, EntityRelationsComponent? relations = null)
    {
        if (relation.Entity == null)
            return;

        if (!_relationsQuery.Resolve(relation.Entity.Value, ref relations))
            return;

        ClearRelationsInternal((relation.Entity.Value, relations), true);
    }

    public void ClearLocalRelations(Entity<EntityRelationsComponent?> ent)
    {
        ClearRelationsInternal(ent, true);
    }

    public void ClearLocalRelation(Entity<EntityRelationsComponent?> ent, ref EntityRelation relation)
    {
        ClearRelationInternal(ent, ref relation, true);
    }

    public void ClearLocalRelation(Entity<EntityRelationsComponent?> ent, ref EntityRelation? relation)
    {
        var copy = relation ?? EntityRelation.Null;
        ClearRelationInternal(ent, ref copy, true);
        relation = copy;
    }

    public void ClearLocalRelations(Entity<EntityRelationsComponent?> ent, List<EntityRelation> relations)
    {
        ClearRelationsInternal(ent, relations, true);
    }

    public void ClearLocalRelations(Entity<EntityRelationsComponent?> ent, HashSet<EntityRelation> relations)
    {
        ClearRelationsInternal(ent, relations, true);
    }

    public void ClearLocalRelations<T>(Entity<EntityRelationsComponent?> ent, Dictionary<EntityRelation, T> relations)
    {
        ClearRelationsInternal(ent, relations, true);
    }

    public void ClearLocalRelations<T>(Entity<EntityRelationsComponent?> ent, Dictionary<T, EntityRelation> relations) where T : notnull
    {
        ClearRelationsInternal(ent, relations, true);
    }

    public void ClearLocalRelation(
        Entity<EntityRelationsComponent?> owner,
        List<EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity)
    {
        ClearRelationInternal(owner, relations, entity, true);
    }

    public void ClearLocalRelation(
        Entity<EntityRelationsComponent?> owner,
        HashSet<EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity)
    {
        ClearRelationInternal(owner, relations, entity, true);
    }

    public void ClearLocalRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<EntityRelation, T> relations,
        Entity<EntityRelationsComponent?> entity)
    {
        ClearRelationInternal(owner, relations, entity, true);
    }

    public void ClearLocalRelation<T>(
        Entity<EntityRelationsComponent?> owner,
        Dictionary<T, EntityRelation> relations,
        Entity<EntityRelationsComponent?> entity,
        bool removeKey = false) where T : notnull
    {
        ClearRelationInternal(owner, relations, entity, removeKey, true);
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

    #region Helpers

    private void RemoveRelationCompIfEmpty(Entity<EntityRelationsComponent?> ent)
    {
        RemoveRelationCompIfEmpty(ref ent);
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

    [Conditional("DEBUG")]
    private void AssertLocalRelations(Entity<EntityRelationsComponent?> owner, Entity<EntityRelationsComponent?>? entity)
    {
        if (_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
        {
            DebugTools.Assert(!owner.Comp.RemoveOnEmpty,
                $"{{nameof(EntityRelationsComponent)}} Local relations aren't supported with {nameof(EntityRelationsComponent.RemoveOnEmpty)} enabled!");
        }

        if (entity == null)
            return;

        var ent = entity.Value;
        if (_relationsQuery.Resolve(ent.Owner, ref ent.Comp, false))
        {
            DebugTools.Assert(!ent.Comp.RemoveOnEmpty,
                $"{{nameof(EntityRelationsComponent)}} Local relations aren't supported with {nameof(EntityRelationsComponent.RemoveOnEmpty)} enabled!");
        }
    }

    [Conditional("DEBUG")]
    private void AssertLocalRelations(Entity<EntityRelationsComponent?> owner, List<EntityUid> entities)
    {
        if (_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
        {
            DebugTools.Assert(!owner.Comp.RemoveOnEmpty,
                $"{{nameof(EntityRelationsComponent)}} Local relations aren't supported with {nameof(EntityRelationsComponent.RemoveOnEmpty)} enabled!");
        }

        foreach (var ent in entities)
        {
            if (!_relationsQuery.TryComp(ent, out var comp))
                continue;

            DebugTools.Assert(!comp.RemoveOnEmpty,
                $"{{nameof(EntityRelationsComponent)}} Local relations aren't supported with {nameof(EntityRelationsComponent.RemoveOnEmpty)} enabled!");
        }
    }

    [Conditional("DEBUG")]
    private void AssertLocalRelations(Entity<EntityRelationsComponent?> owner, HashSet<EntityUid> entities)
    {
        if (_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
        {
            DebugTools.Assert(!owner.Comp.RemoveOnEmpty,
                $"{{nameof(EntityRelationsComponent)}} Local relations aren't supported with {nameof(EntityRelationsComponent.RemoveOnEmpty)} enabled!");
        }

        foreach (var ent in entities)
        {
            if (!_relationsQuery.TryComp(ent, out var comp))
                continue;

            DebugTools.Assert(!comp.RemoveOnEmpty,
                $"{{nameof(EntityRelationsComponent)}} Local relations aren't supported with {nameof(EntityRelationsComponent.RemoveOnEmpty)} enabled!");
        }
    }

    [Conditional("DEBUG")]
    private void AssertLocalRelations(Entity<EntityRelationsComponent?> owner, params Entity<EntityRelationsComponent?>?[] entities)
    {
        if (_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
        {
            DebugTools.Assert(!owner.Comp.RemoveOnEmpty,
                $"{{nameof(EntityRelationsComponent)}} Local relations aren't supported with {nameof(EntityRelationsComponent.RemoveOnEmpty)} enabled!");
        }

        foreach (var ent in entities)
        {
            if (ent == null)
                continue;

            var copy = ent.Value;
            if (!_relationsQuery.Resolve(copy.Owner, ref copy.Comp, false))
                continue;

            DebugTools.Assert(!copy.Comp.RemoveOnEmpty,
                $"{{nameof(EntityRelationsComponent)}} Local relations aren't supported with {nameof(EntityRelationsComponent.RemoveOnEmpty)} enabled!");
        }
    }

    [Conditional("DEBUG")]
    private void AssertLocalRelations<T>(Entity<EntityRelationsComponent?> owner, Dictionary<EntityUid, T> entities)
    {
        if (_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
        {
            DebugTools.Assert(!owner.Comp.RemoveOnEmpty,
                $"{{nameof(EntityRelationsComponent)}} Local relations aren't supported with {nameof(EntityRelationsComponent.RemoveOnEmpty)} enabled!");
        }

        foreach (var ent in entities.Keys)
        {
            if (!_relationsQuery.TryComp(ent, out var comp))
                continue;

            DebugTools.Assert(!comp.RemoveOnEmpty,
                $"{{nameof(EntityRelationsComponent)}} Local relations aren't supported with {nameof(EntityRelationsComponent.RemoveOnEmpty)} enabled!");
        }
    }

    [Conditional("DEBUG")]
    private void AssertLocalRelations<T>(Entity<EntityRelationsComponent?> owner, Dictionary<T, EntityUid> entities) where T : notnull
    {
        if (_relationsQuery.Resolve(owner.Owner, ref owner.Comp, false))
        {
            DebugTools.Assert(!owner.Comp.RemoveOnEmpty,
                $"{{nameof(EntityRelationsComponent)}} Local relations aren't supported with {nameof(EntityRelationsComponent.RemoveOnEmpty)} enabled!");
        }

        foreach (var ent in entities.Values)
        {
            if (!_relationsQuery.TryComp(ent, out var comp))
                continue;

            DebugTools.Assert(!comp.RemoveOnEmpty,
                $"{{nameof(EntityRelationsComponent)}} Local relations aren't supported with {nameof(EntityRelationsComponent.RemoveOnEmpty)} enabled!");
        }
    }

    #endregion
}
