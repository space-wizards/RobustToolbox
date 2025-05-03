using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects;

public partial class EntitySystem
{
    #region Entity LifeStage

    /// <inheritdoc cref="IEntityManager.EntityExists(EntityUid)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool Exists(EntityUid uid)
    {
        return EntityManager.EntityExists(uid);
    }

    /// <inheritdoc cref="IEntityManager.EntityExists(EntityUid?)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool Exists([NotNullWhen(true)] EntityUid? uid)
    {
        return EntityManager.EntityExists(uid);
    }

    /// <summary>
    ///     Retrieves whether the entity is initializing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool Initializing(EntityUid uid, MetaDataComponent? metaData = null)
    {
        return LifeStage(uid, metaData) == EntityLifeStage.Initializing;
    }

    /// <summary>
    ///     Retrieves whether the entity has been initialized and has not yet been deleted.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool Initialized(EntityUid uid, MetaDataComponent? metaData = null)
    {
        return LifeStage(uid, metaData) is >= EntityLifeStage.Initialized and < EntityLifeStage.Terminating;
    }

    /// <summary>
    ///     Retrieves whether the entity is being terminated.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool Terminating(EntityUid uid, MetaDataComponent? metaData = null)
    {
        return LifeStage(uid, metaData) == EntityLifeStage.Terminating;
    }

    /// <summary>
    ///     Retrieves whether the entity is deleted or is nonexistent. Returns false if the entity is currently in the
    ///     process of being deleted.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool Deleted(EntityUid uid, MetaDataComponent? metaData = null)
    {
        return LifeStage(uid, metaData) >= EntityLifeStage.Deleted;
    }

    /// <summary>
    ///     Checks whether the entity is being or has been deleted (or never existed in the first place).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TerminatingOrDeleted(EntityUid uid, MetaDataComponent? metaData = null)
    {
        return LifeStage(uid, metaData) >= EntityLifeStage.Terminating;
    }

    /// <summary>
    ///     Checks whether the entity is being or has been deleted (or never existed in the first place).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TerminatingOrDeleted(EntityUid? uid, MetaDataComponent? metaData = null)
    {
        return !uid.HasValue || TerminatingOrDeleted(uid.Value, metaData);
    }

    [Obsolete("Use override without the EntityQuery")]
    protected bool Deleted(EntityUid uid, EntityQuery<MetaDataComponent> metaQuery) => Deleted(uid);

    /// <summary>
    ///     Retrieves whether the entity is deleted or is nonexistent.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool Deleted([NotNullWhen(false)] EntityUid? uid)
    {
        return !uid.HasValue || Deleted(uid.Value);
    }

    /// <inheritdoc cref="MetaDataComponent.EntityLifeStage" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityLifeStage LifeStage(EntityUid uid, MetaDataComponent? metaData = null)
    {
        if (!EntityManager.MetaQuery.Resolve(uid, ref metaData, false))
            return EntityLifeStage.Deleted;

        return metaData.EntityLifeStage;
    }

    [Obsolete("Use LifeStage()")]
    protected bool TryLifeStage(EntityUid uid, [NotNullWhen(true)] out EntityLifeStage? lifeStage, MetaDataComponent? metaData = null)
    {
        if (!EntityManager.MetaQuery.Resolve(uid, ref metaData, false))
        {
            lifeStage = null;
            return false;
        }

        lifeStage = metaData.EntityLifeStage;
        return true;
    }

    #endregion

    #region Entity Metadata

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool IsPaused(EntityUid? uid, MetaDataComponent? metadata = null)
    {
        return EntityManager.IsPaused(uid, metadata);
    }

    /// <summary>
    /// Marks this entity as dirty so that it will be updated over the network.
    /// </summary>
    /// <remarks>
    /// Calling Dirty on a component will call this directly.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void DirtyEntity(EntityUid uid, MetaDataComponent? meta = null)
    {
        EntityManager.DirtyEntity(uid, meta);
    }

    /// <inheritdoc cref="Dirty{T}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Dirty(EntityUid uid, IComponent component, MetaDataComponent? meta = null)
    {
        EntityManager.Dirty(uid, component, meta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void DirtyField(EntityUid uid, IComponentDelta delta, string fieldName, MetaDataComponent? meta = null)
    {
        EntityManager.DirtyField(uid, delta, fieldName, meta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void DirtyField<T>(Entity<T?> entity, string fieldName, MetaDataComponent? meta = null)
        where T : IComponentDelta
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        EntityManager.DirtyField(entity.Owner, entity.Comp, fieldName, meta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void DirtyField<T>(EntityUid uid, T component, string fieldName, MetaDataComponent? meta = null)
        where T : IComponentDelta
    {
        EntityManager.DirtyField(uid, component, fieldName, meta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void DirtyFields<T>(EntityUid uid, T comp, MetaDataComponent? meta, params string[] fields)
        where T : IComponentDelta
    {
        EntityManager.DirtyFields(uid, comp, meta, fields);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void DirtyFields<T>(Entity<T?> ent, MetaDataComponent? meta, params string[] fields)
        where T : IComponentDelta
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        EntityManager.DirtyFields(ent, ent.Comp, meta, fields);
    }

    /// <summary>
    ///     Marks a component as dirty. This also implicitly dirties the entity this component belongs to.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Dirty<T>(Entity<T> ent, MetaDataComponent? meta = null) where T : IComponent?
    {
        var comp = ent.Comp;
        if (comp == null && !EntityManager.TryGetComponent(ent.Owner, out comp))
            return;

        EntityManager.Dirty(ent, comp, meta);
    }

    /// <inheritdoc cref="Dirty{T}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Dirty<T1, T2>(Entity<T1, T2> ent, MetaDataComponent? meta = null)
        where T1 : IComponent
        where T2 : IComponent
    {
        EntityManager.Dirty(ent, meta);
    }

    /// <inheritdoc cref="Dirty{T}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Dirty<T1, T2, T3>(Entity<T1, T2, T3> ent, MetaDataComponent? meta = null)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        EntityManager.Dirty(ent, meta);
    }

    /// <inheritdoc cref="Dirty{T}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Dirty<T1, T2, T3, T4>(Entity<T1, T2, T3, T4> ent, MetaDataComponent? meta = null)
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        EntityManager.Dirty(ent, meta);
    }

    /// <summary>
    ///     Retrieves the name of an entity.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected string Name(EntityUid uid, MetaDataComponent? metaData = null)
    {
        if (!EntityManager.MetaQuery.Resolve(uid, ref metaData, false))
            throw CompNotFound<MetaDataComponent>(uid);

        return metaData.EntityName;
    }

    /// <summary>
    ///     Retrieves the description of an entity.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected string Description(EntityUid uid, MetaDataComponent? metaData = null)
    {
        if (!EntityManager.MetaQuery.Resolve(uid, ref metaData, false))
            throw CompNotFound<MetaDataComponent>(uid);

        return metaData.EntityDescription;
    }

    /// <summary>
    ///     Retrieves the prototype of an entity, if any.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityPrototype? Prototype(EntityUid uid, MetaDataComponent? metaData = null)
    {
        if (!EntityManager.MetaQuery.Resolve(uid, ref metaData, false))
            throw CompNotFound<MetaDataComponent>(uid);

        return metaData.EntityPrototype;
    }

    /// <summary>
    ///     Retrieves the last <see cref="GameTick"/> the entity was modified at.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    protected GameTick LastModifiedTick(EntityUid uid, MetaDataComponent? metaData = null)
    {
        if (!EntityManager.MetaQuery.Resolve(uid, ref metaData, false))
            throw CompNotFound<MetaDataComponent>(uid);

        return metaData.EntityLastModifiedTick;
    }

    /// <summary>
    ///     Retrieves whether the entity is paused or not.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool Paused(EntityUid uid, MetaDataComponent? metaData = null)
    {
        if (!EntityManager.MetaQuery.Resolve(uid, ref metaData, false))
            throw CompNotFound<MetaDataComponent>(uid);

        return metaData.EntityPaused;
    }

    /// <summary>
    ///     Sets the paused status on an entity.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void SetPaused(EntityUid uid, bool paused, MetaDataComponent? metaData = null)
    {
        if (!EntityManager.MetaQuery.Resolve(uid, ref metaData, false))
            throw CompNotFound<MetaDataComponent>(uid);

        EntityManager.EntitySysManager.GetEntitySystem<MetaDataSystem>().SetEntityPaused(uid, paused, metaData);
    }

    /// <summary>
    ///     Attempts to mark an entity as dirty.
    /// </summary>
    /// <returns>Whether the operation succeeded.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryDirty(EntityUid uid, MetaDataComponent? metaData = null)
    {
        if (!EntityManager.MetaQuery.Resolve(uid, ref metaData, false))
            return false;

        DirtyEntity(uid, metaData);
        return true;
    }

    /// <summary>
    ///     Attempts to retrieve the name of an entity.
    /// </summary>
    /// <returns>Whether the name could be retrieved.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryName(EntityUid uid, [NotNullWhen(true)] out string? name, MetaDataComponent? metaData = null)
    {
        if (!EntityManager.MetaQuery.Resolve(uid, ref metaData, false))
        {
            name = null;
            return false;
        }

        name = metaData.EntityName;
        return true;
    }

    /// <summary>
    ///     Attempts to retrieve the description of an entity.
    /// </summary>
    /// <returns>Whether the description could be retrieved.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryDescription(EntityUid uid, [NotNullWhen(true)] out string? description, MetaDataComponent? metaData = null)
    {
        if (!EntityManager.MetaQuery.Resolve(uid, ref metaData, false))
        {
            description = null;
            return false;
        }

        description = metaData.EntityDescription;
        return true;
    }

    /// <summary>
    ///     Attempts to retrieve the prototype of an entity.
    /// </summary>
    /// <returns>Whether the prototype could be retrieved and was not null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryPrototype(EntityUid uid, [NotNullWhen(true)] out EntityPrototype? prototype, MetaDataComponent? metaData = null)
    {
        if (!EntityManager.MetaQuery.Resolve(uid, ref metaData, false))
        {
            prototype = null;
            return false;
        }

        prototype = metaData.EntityPrototype;
        return prototype != null;
    }

    /// <summary>
    ///     Attempts to retrieve the last <see cref="GameTick"/> the entity was modified at.
    /// </summary>
    /// <returns>Whether the last modified tick could be retrieved.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryLastModifiedTick(EntityUid uid, [NotNullWhen(true)] out GameTick? lastModifiedTick, MetaDataComponent? metaData = null)
    {
        if (!EntityManager.MetaQuery.Resolve(uid, ref metaData, false))
        {
            lastModifiedTick = null;
            return false;
        }

        lastModifiedTick = metaData.EntityLastModifiedTick;
        return true;
    }

    /// <summary>
    ///     Attempts to retrieve the paused status on an entity.
    /// </summary>
    /// <returns>Whether the pause status could be retrieved.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryPaused(EntityUid uid, [NotNullWhen(true)] out bool? paused, MetaDataComponent? metaData = null)
    {
        if (!EntityManager.MetaQuery.Resolve(uid, ref metaData, false))
        {
            paused = null;
            return false;
        }

        paused = metaData.EntityPaused;
        return true;
    }

    /// <inheritdoc cref="IEntityManager.ToPrettyString(EntityUid, MetaDataComponent?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull("uid")]
    protected EntityStringRepresentation? ToPrettyString(EntityUid? uid, MetaDataComponent? metadata = null)
    {
        return EntityManager.ToPrettyString(uid, metadata);
    }

    /// <inheritdoc cref="IEntityManager.ToPrettyString(EntityUid, MetaDataComponent?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull("netEntity")]
    protected EntityStringRepresentation? ToPrettyString(NetEntity? netEntity)
    {
        return EntityManager.ToPrettyString(netEntity);
    }

    /// <inheritdoc cref="IEntityManager.ToPrettyString(EntityUid, MetaDataComponent?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityStringRepresentation ToPrettyString(EntityUid uid, MetaDataComponent? metadata)
        => EntityManager.ToPrettyString((uid, metadata));

    /// <inheritdoc cref="IEntityManager.ToPrettyString(EntityUid, MetaDataComponent?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityStringRepresentation ToPrettyString(Entity<MetaDataComponent?> entity)
        => EntityManager.ToPrettyString(entity);

    /// <inheritdoc cref="IEntityManager.ToPrettyString(EntityUid, MetaDataComponent?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityStringRepresentation ToPrettyString(NetEntity netEntity)
        => EntityManager.ToPrettyString(netEntity);

    #endregion

    #region Component Get

    /// <inheritdoc cref="IEntityManager.GetComponent&lt;T&gt;"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T Comp<T>(EntityUid uid) where T : IComponent
    {
        return EntityManager.GetComponent<T>(uid);
    }

    /// <summary>
    ///     Returns the component of a specific type, or null when it's missing or the entity does not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T? CompOrNull<T>(EntityUid uid) where T : IComponent
    {
        return EntityManager.GetComponentOrNull<T>(uid);
    }

    /// <summary>
    ///     Returns the component of a specific type, or null when it's missing or the entity does not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T? CompOrNull<T>(EntityUid? uid) where T : IComponent
    {
        return uid.HasValue ? EntityManager.GetComponentOrNull<T>(uid.Value) : default;
    }

    /// <inheritdoc cref="IEntityManager.TryGetComponent&lt;T&gt;(EntityUid, out T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [PreferNonGenericVariantFor(typeof(TransformComponent), typeof(MetaDataComponent))]
    protected bool TryComp<T>(EntityUid uid, [NotNullWhen(true)] out T? comp) where T : IComponent
    {
        return EntityManager.TryGetComponent(uid, out comp);
    }

    /// <inheritdoc cref="IEntityManager.TryGetComponent&lt;T&gt;(EntityUid, out T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryComp(EntityUid uid, [NotNullWhen(true)] out TransformComponent? comp)
    {
        return EntityManager.TransformQuery.TryGetComponent(uid, out comp);
    }

    /// <inheritdoc cref="IEntityManager.TryGetComponent&lt;T&gt;(EntityUid, out T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryComp(EntityUid uid, [NotNullWhen(true)] out MetaDataComponent? comp)
    {
        return EntityManager.MetaQuery.TryGetComponent(uid, out comp);
    }

    /// <inheritdoc cref="IEntityManager.TryGetComponent&lt;T&gt;(EntityUid?, out T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryComp<T>([NotNullWhen(true)] EntityUid? uid, [NotNullWhen(true)] out T? comp) where T : IComponent
    {
        if (!uid.HasValue)
        {
            comp = default;
            return false;
        }

        return EntityManager.TryGetComponent(uid.Value, out comp);
    }

    /// <inheritdoc cref="IEntityManager.TryGetComponent&lt;T&gt;(EntityUid?, out T)"/>
    protected bool TryComp([NotNullWhen(true)] EntityUid? uid, [NotNullWhen(true)] out TransformComponent? comp)
    {
        if (!uid.HasValue)
        {
            comp = default;
            return false;
        }

        return EntityManager.TransformQuery.TryGetComponent(uid.Value, out comp);
    }

    /// <inheritdoc cref="IEntityManager.TryGetComponent&lt;T&gt;(EntityUid?, out T)"/>
    protected bool TryComp([NotNullWhen(true)] EntityUid? uid, [NotNullWhen(true)] out MetaDataComponent? comp)
    {
        if (!uid.HasValue)
        {
            comp = default;
            return false;
        }

        return EntityManager.MetaQuery.TryGetComponent(uid.Value, out comp);
    }

    /// <inheritdoc cref="IEntityManager.GetComponents"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IEnumerable<IComponent> AllComps(EntityUid uid)
    {
        return EntityManager.GetComponents(uid);
    }

    /// <inheritdoc cref="IEntityManager.GetComponents&lt;T&gt;"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IEnumerable<T> AllComps<T>(EntityUid uid)
    {
        return EntityManager.GetComponents<T>(uid);
    }

    /// <summary>
    ///     Returns the <see cref="TransformComponent"/> on an entity.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected TransformComponent Transform(EntityUid uid)
    {
        return EntityManager.TransformQuery.GetComponent(uid);
    }

    /// <summary>
    ///     Returns the <see cref="MetaDataComponent"/> on an entity.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected MetaDataComponent MetaData(EntityUid uid)
    {
        return EntityManager.MetaQuery.GetComponent(uid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected (EntityUid, MetaDataComponent)  GetEntityData(NetEntity nuid)
    {
        return EntityManager.GetEntityData(nuid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryGetEntityData(NetEntity nuid, [NotNullWhen(true)] out EntityUid? uid,
        [NotNullWhen(true)] out MetaDataComponent? meta)
    {
        return EntityManager.TryGetEntityData(nuid, out uid, out meta);
    }

    #endregion

    #region Component Copy

    /// <inheritdoc cref="IEntityManager.TryCopyComponent"/>
    protected bool TryCopyComponent<T>(
        EntityUid source,
        EntityUid target,
        ref T? sourceComponent,
        [NotNullWhen(true)] out T? targetComp,
        MetaDataComponent? meta = null) where T : IComponent
    {
        return EntityManager.TryCopyComponent(source, target, ref sourceComponent, out targetComp, meta);
    }

    /// <inheritdoc cref="IEntityManager.TryCopyComponents"/>
    protected bool TryCopyComponents(
        EntityUid source,
        EntityUid target,
        MetaDataComponent? meta = null,
        params Type[] sourceComponents)
    {
        return EntityManager.TryCopyComponents(source, target, meta, sourceComponents);
    }

    /// <inheritdoc cref="IEntityManager.CopyComponent"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IComponent CopyComp(EntityUid source, EntityUid target, IComponent sourceComponent, MetaDataComponent? meta = null)
    {
        return EntityManager.CopyComponent(source, target, sourceComponent, meta);
    }

    /// <inheritdoc cref="IEntityManager.CopyComponent{T}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T CopyComp<T>(EntityUid source, EntityUid target, T sourceComponent, MetaDataComponent? meta = null) where T : IComponent
    {
        return EntityManager.CopyComponent(source, target, sourceComponent, meta);
    }

    /// <inheritdoc cref="IEntityManager.CopyComponents"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void CopyComps(EntityUid source, EntityUid target, MetaDataComponent? meta = null, params IComponent[] sourceComponents)
    {
        EntityManager.CopyComponents(source, target, meta, sourceComponents);
    }

    #endregion

    #region Component Has

    /// <summary>
    ///     Retrieves whether the entity has the specified component or not.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool HasComp<T>(EntityUid uid) where T : IComponent
    {
        return EntityManager.HasComponent<T>(uid);
    }

    /// <summary>
    ///     Retrieves whether the entity has the specified component or not.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool HasComp(EntityUid uid, Type type)
    {
        return EntityManager.HasComponent(uid, type);
    }

    /// <summary>
    ///     Retrieves whether the entity has the specified component or not.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool HasComp<T>([NotNullWhen(true)] EntityUid? uid) where T : IComponent
    {
        return EntityManager.HasComponent<T>(uid);
    }

    /// <summary>
    ///     Retrieves whether the entity has the specified component or not.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool HasComp([NotNullWhen(true)] EntityUid? uid, Type type)
    {
        return EntityManager.HasComponent(uid, type);
    }

    #endregion

    #region Component Add

    /// <inheritdoc cref="IEntityManager.AddComponent&lt;T&gt;(EntityUid)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T AddComp<T>(EntityUid uid) where T :  Component, new()
    {
        return EntityManager.AddComponent<T>(uid);
    }

    /// <inheritdoc cref="IEntityManager.AddComponent&lt;T&gt;(EntityUid, T, bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void AddComp<T>(EntityUid uid, T component, bool overwrite = false) where T : IComponent
    {
        EntityManager.AddComponent(uid, component, overwrite);
    }

    /// <inheritdoc cref="IEntityManager.EnsureComponent&lt;T&gt;(EntityUid)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T EnsureComp<T>(EntityUid uid) where T : IComponent, new()
    {
        return EntityManager.EnsureComponent<T>(uid);
    }

    /// <inheritdoc cref="IEntityManager.EnsureComponent&lt;T&gt;(EntityUid, out T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool EnsureComp<T>(EntityUid uid, out T comp) where T : IComponent, new()
    {
        return EntityManager.EnsureComponent(uid, out comp);
    }

    /// <inheritdoc cref="IEntityManager.EnsureComponent&lt;T&gt;(ref Entity&lt;T&gt;)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool EnsureComp<T>(ref Entity<T?> entity) where T : IComponent, new()
    {
        return EntityManager.EnsureComponent(ref entity);
    }

    #endregion

    #region Component Remove Deferred

    /// <inheritdoc cref="IEntityManager.RemoveComponentDeferred&lt;T&gt;(EntityUid)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool RemCompDeferred<T>(EntityUid uid) where T : IComponent
    {
        return EntityManager.RemoveComponentDeferred<T>(uid);
    }

    /// <inheritdoc cref="IEntityManager.RemoveComponentDeferred(EntityUid, Type)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool RemCompDeferred(EntityUid uid, Type type)
    {
        return EntityManager.RemoveComponentDeferred(uid, type);
    }

    /// <inheritdoc cref="IEntityManager.RemoveComponentDeferred(EntityUid, IComponent)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RemCompDeferred(EntityUid uid, IComponent component)
    {
        EntityManager.RemoveComponentDeferred(uid, component);
    }
    #endregion

    #region Component count

    /// <inheritdoc cref="IEntityManager.Count" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected int Count<T>() where T : IComponent
    {
        return EntityManager.Count<T>();
    }

    /// <inheritdoc cref="IEntityManager.Count" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected int Count(Type type)
    {
        return EntityManager.Count(type);
    }

    #endregion

    #region Component Remove

    /// <inheritdoc cref="IEntityManager.RemoveComponent&lt;T&gt;(EntityUid)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool RemComp<T>(EntityUid uid) where T : IComponent
    {
        return EntityManager.RemoveComponent<T>(uid);
    }

    /// <inheritdoc cref="IEntityManager.RemoveComponent(EntityUid, Type)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool RemComp(EntityUid uid, Type type)
    {
        return EntityManager.RemoveComponent(uid, type);
    }

    /// <inheritdoc cref="IEntityManager.RemoveComponent(EntityUid, IComponent)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RemComp(EntityUid uid, IComponent component)
    {
        EntityManager.RemoveComponent(uid, component);
    }
    #endregion

    #region Entity Delete

    /// <inheritdoc cref="IEntityManager.DeleteEntity(EntityUid)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Del(EntityUid? uid)
    {
        EntityManager.DeleteEntity(uid);
    }

    /// <inheritdoc cref="IEntityManager.QueueDeleteEntity(EntityUid)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void QueueDel(EntityUid? uid)
    {
        EntityManager.QueueDeleteEntity(uid);
    }

    /// <inheritdoc cref="IEntityManager.DeleteEntity(EntityUid?)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void PredictedDel(Entity<MetaDataComponent?, TransformComponent?> ent)
    {
        EntityManager.PredictedDeleteEntity(ent);
    }

    /// <inheritdoc cref="IEntityManager.DeleteEntity(EntityUid?)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void PredictedDel(Entity<MetaDataComponent?, TransformComponent?>? ent)
    {
        EntityManager.PredictedDeleteEntity(ent);
    }

    /// <inheritdoc cref="IEntityManager.QueueDeleteEntity(EntityUid)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void PredictedQueueDel(Entity<MetaDataComponent?, TransformComponent?> ent)
    {
        EntityManager.PredictedQueueDeleteEntity(ent);
    }

    /// <inheritdoc cref="IEntityManager.QueueDeleteEntity(EntityUid?)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void PredictedQueueDel(Entity<MetaDataComponent?, TransformComponent?>? ent)
    {
        EntityManager.PredictedQueueDeleteEntity(ent);
    }

    /// <inheritdoc cref="IEntityManager.TryQueueDeleteEntity(EntityUid?)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryQueueDel(EntityUid? uid)
    {
        return EntityManager.TryQueueDeleteEntity(uid);
    }

    #endregion

    #region Entity Spawning

    // This method will be obsoleted soon(TM).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid Spawn(string? prototype, EntityCoordinates coordinates)
    {
        return ((IEntityManager)EntityManager).SpawnEntity(prototype, coordinates);
    }

    /// <inheritdoc cref="IEntityManager.Spawn(string?, MapCoordinates, ComponentRegistry?, Angle)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid Spawn(string? prototype, MapCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default)
        => EntityManager.Spawn(prototype, coordinates, overrides, rotation);

    /// <inheritdoc cref="IEntityManager.Spawn(string?, ComponentRegistry?, bool)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid Spawn(string? prototype = null, ComponentRegistry? overrides = null, bool doMapInit = true)
        => EntityManager.Spawn(prototype, overrides, doMapInit);

    /// <inheritdoc cref="IEntityManager.SpawnAttachedTo" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid SpawnAttachedTo(string? prototype, EntityCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default)
        => EntityManager.SpawnAttachedTo(prototype, coordinates, overrides, rotation);

    /// <inheritdoc cref="IEntityManager.SpawnAtPosition" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid SpawnAtPosition(string? prototype, EntityCoordinates coordinates, ComponentRegistry? overrides = null)
        => EntityManager.SpawnAtPosition(prototype, coordinates, overrides);

    /// <inheritdoc cref="IEntityManager.TrySpawnInContainer" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TrySpawnInContainer(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        [NotNullWhen(true)] out EntityUid? uid,
        ContainerManagerComponent? containerComp = null,
        ComponentRegistry? overrides = null)
    {
        return EntityManager.TrySpawnInContainer(protoName, containerUid, containerId, out uid, containerComp, overrides);
    }

    /// <inheritdoc cref="IEntityManager.TrySpawnNextTo" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TrySpawnNextTo(
        string? protoName,
        EntityUid target,
        [NotNullWhen(true)] out EntityUid? uid,
        TransformComponent? xform = null,
        ComponentRegistry? overrides = null)
    {
        return EntityManager.TrySpawnNextTo(protoName, target, out uid, xform, overrides);
    }

    /// <inheritdoc cref="IEntityManager.SpawnNextToOrDrop" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid SpawnNextToOrDrop(
        string? protoName,
        EntityUid target,
        TransformComponent? xform = null,
        ComponentRegistry? overrides = null)
    {
        return EntityManager.SpawnNextToOrDrop(protoName, target, xform, overrides);
    }

    /// <inheritdoc cref="IEntityManager.SpawnInContainerOrDrop" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid SpawnInContainerOrDrop(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        TransformComponent? xform = null,
        ContainerManagerComponent? container = null,
        ComponentRegistry? overrides = null)
    {
        return EntityManager.SpawnInContainerOrDrop(protoName, containerUid, containerId, xform, container, overrides);
    }

    #endregion

    #region PredictedSpawning

    protected void FlagPredicted(Entity<MetaDataComponent?> ent)
    {
        EntityManager.FlagPredicted(ent);
    }

    /// <inheritdoc cref="IEntityManager.SpawnAttachedTo" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid PredictedSpawnAttachedTo(string? prototype, EntityCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default)
        => EntityManager.PredictedSpawnAttachedTo(prototype, coordinates, overrides, rotation);

    /// <inheritdoc cref="IEntityManager.SpawnAtPosition" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid PredictedSpawnAtPosition(string? prototype, EntityCoordinates coordinates, ComponentRegistry? overrides = null)
        => EntityManager.PredictedSpawnAtPosition(prototype, coordinates, overrides);

    /// <inheritdoc cref="IEntityManager.TrySpawnInContainer" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool PredictedTrySpawnInContainer(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        [NotNullWhen(true)] out EntityUid? uid,
        ContainerManagerComponent? containerComp = null,
        ComponentRegistry? overrides = null)
    {
        return EntityManager.PredictedTrySpawnInContainer(protoName, containerUid, containerId, out uid, containerComp, overrides);
    }

    /// <inheritdoc cref="IEntityManager.TrySpawnNextTo" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool PredictedTrySpawnNextTo(
        string? protoName,
        EntityUid target,
        [NotNullWhen(true)] out EntityUid? uid,
        TransformComponent? xform = null,
        ComponentRegistry? overrides = null)
    {
        return EntityManager.PredictedTrySpawnNextTo(protoName, target, out uid, xform, overrides);
    }

    /// <inheritdoc cref="IEntityManager.SpawnNextToOrDrop" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid PredictedSpawnNextToOrDrop(
        string? protoName,
        EntityUid target,
        TransformComponent? xform = null,
        ComponentRegistry? overrides = null)
    {
        return EntityManager.PredictedSpawnNextToOrDrop(protoName, target, xform, overrides);
    }

    /// <inheritdoc cref="IEntityManager.SpawnInContainerOrDrop" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid PredictedSpawnInContainerOrDrop(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        TransformComponent? xform = null,
        ContainerManagerComponent? container = null,
        ComponentRegistry? overrides = null)
    {
        return EntityManager.PredictedSpawnInContainerOrDrop(protoName, containerUid, containerId, xform, container, overrides);
    }

    #endregion

    #region Utils

    /// <summary>
    ///     Utility static method to create an exception to be thrown when an entity doesn't have a specific component.
    /// </summary>
    private static KeyNotFoundException CompNotFound<T>(EntityUid uid)
        => new($"Entity {uid} does not have a component of type {typeof(T)}");

    #endregion

    #region All Entity Query

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected AllEntityQueryEnumerator<TComp1> AllEntityQuery<TComp1>() where TComp1 : IComponent
    {
        return EntityManager.AllEntityQueryEnumerator<TComp1>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected AllEntityQueryEnumerator<TComp1, TComp2> AllEntityQuery<TComp1, TComp2>()
        where TComp1 : IComponent
        where TComp2 : IComponent
    {
        return EntityManager.AllEntityQueryEnumerator<TComp1, TComp2>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected AllEntityQueryEnumerator<TComp1, TComp2, TComp3> AllEntityQuery<TComp1, TComp2, TComp3>()
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
    {
        return EntityManager.AllEntityQueryEnumerator<TComp1, TComp2, TComp3>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected AllEntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4> AllEntityQuery<TComp1, TComp2, TComp3, TComp4>()
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent
    {
        return EntityManager.AllEntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>();
    }

    #endregion

    #region Get Entity Query

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityQueryEnumerator<TComp1> EntityQueryEnumerator<TComp1>() where TComp1 : IComponent
    {
        return EntityManager.EntityQueryEnumerator<TComp1>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityQueryEnumerator<TComp1, TComp2> EntityQueryEnumerator<TComp1, TComp2>()
        where TComp1 : IComponent
        where TComp2 : IComponent
    {
        return EntityManager.EntityQueryEnumerator<TComp1, TComp2>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityQueryEnumerator<TComp1, TComp2, TComp3> EntityQueryEnumerator<TComp1, TComp2, TComp3>()
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
    {
        return EntityManager.EntityQueryEnumerator<TComp1, TComp2, TComp3>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4> EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>()
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent
    {
        return EntityManager.EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>();
    }

    #endregion

    #region Entity Query

    /// <remarks>
    /// If you need the EntityUid, use <see cref="EntityQueryEnumerator{TComp1}"/>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    protected EntityQuery<T> GetEntityQuery<T>() where T : IComponent
    {
        return EntityManager.GetEntityQuery<T>();
    }

    /// <remarks>
    /// If you need the EntityUid, use <see cref="EntityQueryEnumerator{TComp1}"/>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IEnumerable<TComp1> EntityQuery<TComp1>(bool includePaused = false) where TComp1 : IComponent
    {
        return EntityManager.EntityQuery<TComp1>(includePaused);
    }

    /// <remarks>
    /// If you need the EntityUid, use <see cref="EntityQueryEnumerator{TComp1, TComp2}"/>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IEnumerable<(TComp1, TComp2)> EntityQuery<TComp1, TComp2>(bool includePaused = false)
        where TComp1 : IComponent
        where TComp2 : IComponent
    {
        return EntityManager.EntityQuery<TComp1, TComp2>(includePaused);
    }

    /// <remarks>
    /// If you need the EntityUid, use <see cref="EntityQueryEnumerator{TComp1, TComp2, TComp3}"/>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IEnumerable<(TComp1, TComp2, TComp3)> EntityQuery<TComp1, TComp2, TComp3>(bool includePaused = false)
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
    {
        return EntityManager.EntityQuery<TComp1, TComp2, TComp3>(includePaused);
    }

    /// <remarks>
    /// If you need the EntityUid, use <see cref="EntityQueryEnumerator{TComp1, TComp2, TComp3, TComp4}"/>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IEnumerable<(TComp1, TComp2, TComp3, TComp4)> EntityQuery<TComp1, TComp2, TComp3, TComp4>(bool includePaused = false)
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent
    {
        return EntityManager.EntityQuery<TComp1, TComp2, TComp3, TComp4>(includePaused);
    }

    #endregion

    #region Networked Events

    /// <summary>
    ///     Sends a networked message to the server, while also repeatedly raising it locally for every time this tick gets re-predicted.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RaisePredictiveEvent<T>(T msg) where T : EntityEventArgs
    {
        EntityManager.RaisePredictiveEvent(msg);
    }

    #endregion

    #region NetEntities

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool IsClientSide(EntityUid entity, MetaDataComponent? meta = null)
    {
        return EntityManager.IsClientSide(entity, meta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool IsClientSide(Entity<MetaDataComponent> entity)
    {
        return EntityManager.IsClientSide(entity, entity.Comp);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryGetEntity(NetEntity nEntity, [NotNullWhen(true)] out EntityUid? entity)
    {
        return EntityManager.TryGetEntity(nEntity, out entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryGetEntity(NetEntity? nEntity, [NotNullWhen(true)] out EntityUid? entity)
    {
        return EntityManager.TryGetEntity(nEntity, out entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetNetEntity(EntityUid uid, [NotNullWhen(true)] out NetEntity? netEntity, MetaDataComponent? metadata = null)
    {
        return EntityManager.TryGetNetEntity(uid, out netEntity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetNetEntity(EntityUid? uid, [NotNullWhen(true)] out NetEntity? netEntity, MetaDataComponent? metadata = null)
    {
        return EntityManager.TryGetNetEntity(uid, out netEntity);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> of an entity. Returns <see cref="NetEntity.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected NetEntity GetNetEntity(EntityUid uid, MetaDataComponent? metadata = null)
    {
        return EntityManager.GetNetEntity(uid, metadata);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> of an entity.  Logs an error if the entity does not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected NetEntity? GetNetEntity(EntityUid? uid, MetaDataComponent? metadata = null)
    {
        return EntityManager.GetNetEntity(uid, metadata);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> of an entity or creates a new entity if none exists.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid EnsureEntity<T>(NetEntity netEntity, EntityUid callerEntity)
    {
        return EntityManager.EnsureEntity<T>(netEntity, callerEntity);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> of an entity or creates a new one if not null.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid? EnsureEntity<T>(NetEntity? netEntity, EntityUid callerEntity)
    {
        return EntityManager.EnsureEntity<T>(netEntity, callerEntity);
    }

    /// <summary>
    ///     Returns the <see cref="NetCoordinates"/> of an entity or creates a new entity if none exists.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityCoordinates EnsureCoordinates<T>(NetCoordinates netCoordinates, EntityUid callerEntity)
    {
        return EntityManager.EnsureCoordinates<T>(netCoordinates, callerEntity);
    }

    /// <summary>
    ///     Returns the <see cref="NetCoordinates"/> of an entity or creates a new one if not null.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityCoordinates? EnsureCoordinates<T>(NetCoordinates? netCoordinates, EntityUid callerEntity)
    {
        return EntityManager.EnsureCoordinates<T>(netCoordinates, callerEntity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected HashSet<EntityUid> EnsureEntitySet<T>(HashSet<NetEntity> netEntities, EntityUid callerEntity)
    {
        return EntityManager.EnsureEntitySet<T>(netEntities, callerEntity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureEntitySet<T>(HashSet<NetEntity> netEntities, EntityUid callerEntity, HashSet<EntityUid> entities)
    {
        EntityManager.EnsureEntitySet<T>(netEntities, callerEntity, entities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<EntityUid> EnsureEntityList<T>(List<NetEntity> netEntities, EntityUid callerEntity)
    {
        return EntityManager.EnsureEntityList<T>(netEntities, callerEntity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureEntityList<T>(List<NetEntity> netEntities, EntityUid callerEntity, List<EntityUid> entities)
    {
        EntityManager.EnsureEntityList<T>(netEntities, callerEntity, entities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureEntityDictionary<TComp, TValue>(Dictionary<NetEntity, TValue> netEntities, EntityUid callerEntity, Dictionary<EntityUid, TValue> entities)
    {
        EntityManager.EnsureEntityDictionary<TComp, TValue>(netEntities, callerEntity, entities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureEntityDictionaryNullableValue<TComp, TValue>(Dictionary<NetEntity, TValue?> netEntities, EntityUid callerEntity, Dictionary<EntityUid, TValue?> entities)
    {
        EntityManager.EnsureEntityDictionaryNullableValue<TComp, TValue>(netEntities, callerEntity, entities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureEntityDictionary<TComp, TKey>(Dictionary<TKey, NetEntity> netEntities, EntityUid callerEntity, Dictionary<TKey, EntityUid> entities) where TKey : notnull
    {
        EntityManager.EnsureEntityDictionary<TComp, TKey>(netEntities, callerEntity, entities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureEntityDictionary<TComp, TKey>(Dictionary<TKey, NetEntity?> netEntities, EntityUid callerEntity, Dictionary<TKey, EntityUid?> entities) where TKey : notnull
    {
        EntityManager.EnsureEntityDictionary<TComp, TKey>(netEntities, callerEntity, entities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureEntityDictionary<TComp>(Dictionary<NetEntity, NetEntity> netEntities, EntityUid callerEntity, Dictionary<EntityUid, EntityUid> entities)
    {
        EntityManager.EnsureEntityDictionary<TComp>(netEntities, callerEntity, entities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureEntityDictionary<TComp>(Dictionary<NetEntity, NetEntity?> netEntities, EntityUid callerEntity, Dictionary<EntityUid, EntityUid?> entities)
    {
        EntityManager.EnsureEntityDictionary<TComp>(netEntities, callerEntity, entities);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> of a <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid GetEntity(NetEntity netEntity)
    {
        return EntityManager.GetEntity(netEntity);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> of a <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid? GetEntity(NetEntity? netEntity)
    {
        return EntityManager.GetEntity(netEntity);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> versions of the supplied entities. Logs an error if the entities do not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected HashSet<NetEntity> GetNetEntitySet(HashSet<EntityUid> uids)
    {
        return EntityManager.GetNetEntitySet(uids);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> versions of the supplied <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected HashSet<EntityUid> GetEntitySet(HashSet<NetEntity> netEntities)
    {
        return EntityManager.GetEntitySet(netEntities);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> versions of the supplied entities. Logs an error if the entities do not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<NetEntity> GetNetEntityList(ICollection<EntityUid> uids)
    {
        return EntityManager.GetNetEntityList(uids);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> versions of the supplied entities. Logs an error if the entities do not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<NetEntity> GetNetEntityList(IReadOnlyList<EntityUid> uids)
    {
        return EntityManager.GetNetEntityList(uids);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> versions of the supplied <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<EntityUid> GetEntityList(ICollection<NetEntity> netEntities)
    {
        return EntityManager.GetEntityList(netEntities);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> versions of the supplied entities. Logs an error if the entities do not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<NetEntity> GetNetEntityList(List<EntityUid> uids)
    {
        return EntityManager.GetNetEntityList(uids);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> versions of the supplied <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<EntityUid> GetEntityList(List<NetEntity> netEntities)
    {
        return EntityManager.GetEntityList(netEntities);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> versions of the supplied entities. Logs an error if the entities do not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<NetEntity?> GetNetEntityList(List<EntityUid?> uids)
    {
        return EntityManager.GetNetEntityList(uids);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> versions of the supplied <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<EntityUid?> GetEntityList(List<NetEntity?> netEntities)
    {
        return EntityManager.GetEntityList(netEntities);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> versions of the supplied entities. Logs an error if the entities do not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected NetEntity[] GetNetEntityArray(EntityUid[] uids)
    {
        return EntityManager.GetNetEntityArray(uids);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> versions of the supplied <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid[] GetEntityArray(NetEntity[] netEntities)
    {
        return EntityManager.GetEntityArray(netEntities);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> versions of the supplied entities.  Logs an error if the entities do not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected NetEntity?[] GetNetEntityArray(EntityUid?[] uids)
    {
        return EntityManager.GetNetEntityArray(uids);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> versions of the supplied <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid?[] GetEntityArray(NetEntity?[] netEntities)
    {
        return EntityManager.GetEntityArray(netEntities);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> versions of the supplied entities.  Logs an error if the entities do not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Dictionary<NetEntity, T> GetNetEntityDictionary<T>(Dictionary<EntityUid, T> uids)
    {
        return EntityManager.GetNetEntityDictionary(uids);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> versions of the supplied entities.  Logs an error if the entities do not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Dictionary<T, NetEntity> GetNetEntityDictionary<T>(Dictionary<T, EntityUid> uids) where T : notnull
    {
        return EntityManager.GetNetEntityDictionary(uids);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> versions of the supplied entities.  Logs an error if the entities do not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Dictionary<T, NetEntity?> GetNetEntityDictionary<T>(Dictionary<T, EntityUid?> uids) where T : notnull
    {
        return EntityManager.GetNetEntityDictionary(uids);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> versions of the supplied entities.  Logs an error if the entities do not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Dictionary<NetEntity, NetEntity> GetNetEntityDictionary(Dictionary<EntityUid, EntityUid> uids)
    {
        return EntityManager.GetNetEntityDictionary(uids);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> versions of the supplied entities.  Logs an error if the entities do not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Dictionary<NetEntity, NetEntity?> GetNetEntityDictionary(Dictionary<EntityUid, EntityUid?> uids)
    {
        return EntityManager.GetNetEntityDictionary(uids);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> versions of the supplied <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Dictionary<EntityUid, T> GetEntityDictionary<T>(Dictionary<NetEntity, T> uids)
    {
        return EntityManager.GetEntityDictionary(uids);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> versions of the supplied <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Dictionary<T, EntityUid> GetEntityDictionary<T>(Dictionary<T, NetEntity> uids) where T : notnull
    {
        return EntityManager.GetEntityDictionary(uids);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> versions of the supplied <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Dictionary<T, EntityUid?> GetEntityDictionary<T>(Dictionary<T, NetEntity?> uids) where T : notnull
    {
        return EntityManager.GetEntityDictionary(uids);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> versions of the supplied <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Dictionary<EntityUid, EntityUid> GetEntityDictionary(Dictionary<NetEntity, NetEntity> uids)
    {
        return EntityManager.GetEntityDictionary(uids);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> versions of the supplied <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Dictionary<EntityUid, EntityUid?> GetEntityDictionary(Dictionary<NetEntity, NetEntity?> uids)
    {
        return EntityManager.GetEntityDictionary(uids);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected NetCoordinates GetNetCoordinates(EntityCoordinates coordinates, MetaDataComponent? metadata = null)
    {
        return EntityManager.GetNetCoordinates(coordinates, metadata);
    }

    /// <summary>
    ///     Returns the <see cref="NetEntity"/> of an entity. Returns <see cref="NetEntity.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected NetCoordinates? GetNetCoordinates(EntityCoordinates? coordinates, MetaDataComponent? metadata = null)
    {
        return EntityManager.GetNetCoordinates(coordinates, metadata);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> of a <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityCoordinates GetCoordinates(NetCoordinates netEntity)
    {
        return EntityManager.GetCoordinates(netEntity);
    }

    /// <summary>
    ///     Returns the <see cref="EntityUid"/> of a <see cref="NetEntity"/>. Returns <see cref="EntityUid.Invalid"/> if it doesn't exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityCoordinates? GetCoordinates(NetCoordinates? netEntity)
    {
        return EntityManager.GetCoordinates(netEntity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected HashSet<EntityCoordinates> GetEntitySet(HashSet<NetCoordinates> netEntities)
    {
        return EntityManager.GetEntitySet(netEntities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<EntityCoordinates> GetEntityList(List<NetCoordinates> netEntities)
    {
        return EntityManager.GetEntityList(netEntities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<EntityCoordinates> GetEntityList(ICollection<NetCoordinates> netEntities)
    {
        return EntityManager.GetEntityList(netEntities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<EntityCoordinates?> GetEntityList(List<NetCoordinates?> netEntities)
    {
        return EntityManager.GetEntityList(netEntities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityCoordinates[] GetEntityArray(NetCoordinates[] netEntities)
    {
        return EntityManager.GetEntityArray(netEntities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityCoordinates?[] GetEntityArray(NetCoordinates?[] netEntities)
    {
        return EntityManager.GetEntityArray(netEntities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected HashSet<NetCoordinates> GetNetCoordinatesSet(HashSet<EntityCoordinates> entities)
    {
        return EntityManager.GetNetCoordinatesSet(entities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<NetCoordinates> GetNetCoordinatesList(List<EntityCoordinates> entities)
    {
        return EntityManager.GetNetCoordinatesList(entities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<NetCoordinates> GetNetCoordinatesList(ICollection<EntityCoordinates> entities)
    {
        return EntityManager.GetNetCoordinatesList(entities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected List<NetCoordinates?> GetNetCoordinatesList(List<EntityCoordinates?> entities)
    {
        return EntityManager.GetNetCoordinatesList(entities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected NetCoordinates[] GetNetCoordinatesArray(EntityCoordinates[] entities)
    {
        return EntityManager.GetNetCoordinatesArray(entities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected NetCoordinates?[] GetNetCoordinatesArray(EntityCoordinates?[] entities)
    {
        return EntityManager.GetNetCoordinatesArray(entities);
    }

    #endregion
}
