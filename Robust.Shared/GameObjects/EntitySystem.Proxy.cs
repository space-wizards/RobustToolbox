using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects;

public partial class EntitySystem
{
    #region Entity LifeStage

    /// <inheritdoc cref="IEntityManager.EntityExists" />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Exists(EntityUid uid)
    {
        return EntityManager.EntityExists(uid);
    }

    /// <summary>
    ///     Retrieves whether the entity is initializing. Throws if the entity does not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Initializing(EntityUid uid, MetaDataComponent? metaData = null)
    {
        return LifeStage(uid, metaData) == EntityLifeStage.Initializing;
    }

    /// <summary>
    ///     Retrieves whether the entity is initialized. Throws if the entity does not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Initialized(EntityUid uid, MetaDataComponent? metaData = null)
    {
        return LifeStage(uid, metaData) >= EntityLifeStage.Initialized;
    }

    /// <summary>
    ///     Retrieves whether the entity is being terminated. Throws if the entity does not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Terminating(EntityUid uid, MetaDataComponent? metaData = null)
    {
        return LifeStage(uid, metaData) == EntityLifeStage.Terminating;
    }

    /// <summary>
    ///     Retrieves whether the entity is deleted. Throws if the entity does not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Deleted(EntityUid uid, MetaDataComponent? metaData = null)
    {
        return LifeStage(uid, metaData) >= EntityLifeStage.Deleted;
    }

    /// <inheritdoc cref="MetaDataComponent.EntityLifeStage" />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public EntityLifeStage LifeStage(EntityUid uid, MetaDataComponent? metaData = null)
    {
        if (!Resolve(uid, ref metaData, false))
            throw CompNotFound<MetaDataComponent>(uid);

        return metaData.EntityLifeStage;
    }

    /// <summary>
    ///     Attempts to retrieve whether the entity is initializing.
    /// </summary>
    /// <returns>Whether it could be retrieved.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryInitializing(EntityUid uid, [NotNullWhen(true)] out bool? initializing, MetaDataComponent? metaData = null)
    {
        if (!TryLifeStage(uid, out var lifeStage, metaData))
        {
            initializing = null;
            return false;
        }

        initializing = lifeStage == EntityLifeStage.Initializing;
        return true;
    }

    /// <summary>
    ///     Attempts to retrieve whether the entity is initialized.
    /// </summary>
    /// <returns>Whether it could be retrieved.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryInitialized(EntityUid uid, [NotNullWhen(true)] out bool? initialized, MetaDataComponent? metaData = null)
    {
        if (!TryLifeStage(uid, out var lifeStage, metaData))
        {
            initialized = null;
            return false;
        }

        initialized = lifeStage >= EntityLifeStage.Initialized;
        return true;
    }

    /// <summary>
    ///     Attempts to retrieve whether the entity is terminating.
    /// </summary>
    /// <returns>Whether it could be retrieved.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryTerminating(EntityUid uid, [NotNullWhen(true)] out bool? terminating, MetaDataComponent? metaData = null)
    {
        if (!TryLifeStage(uid, out var lifeStage, metaData))
        {
            terminating = null;
            return false;
        }

        terminating = lifeStage == EntityLifeStage.Terminating;
        return true;
    }

    /// <summary>
    ///     Attempts to retrieve whether the entity is deleted.
    /// </summary>
    /// <returns>Whether it could be retrieved.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryDeleted(EntityUid uid, [NotNullWhen(true)] out bool? deleted, MetaDataComponent? metaData = null)
    {
        if (!TryLifeStage(uid, out var lifeStage, metaData))
        {
            deleted = null;
            return false;
        }

        deleted = lifeStage >= EntityLifeStage.Deleted;
        return true;
    }

    /// <summary>
    ///     Attempts to retrieve the life-stage of the entity.
    ///     <seealso cref="MetaDataComponent.EntityLifeStage"/>
    /// </summary>
    /// <returns>Whether it could be retrieved.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryLifeStage(EntityUid uid, [NotNullWhen(true)] out EntityLifeStage? lifeStage, MetaDataComponent? metaData = null)
    {
        if (!Resolve(uid, ref metaData))
        {
            lifeStage = null;
            return false;
        }

        lifeStage = metaData.EntityLifeStage;
        return true;
    }

    #endregion

    #region Entity Metadata

    /// <summary>
    ///     Marks an entity as dirty.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Dirty(EntityUid uid)
    {
        EntityManager.DirtyEntity(uid);
    }

    /// <summary>
    ///     Retrieves the name of an entity.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public string Name(EntityUid uid, MetaDataComponent? metaData = null)
    {
        if(!Resolve(uid, ref metaData, false))
            throw CompNotFound<MetaDataComponent>(uid);

        return metaData.EntityName;
    }

    /// <summary>
    ///     Retrieves the description of an entity.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public string Description(EntityUid uid, MetaDataComponent? metaData = null)
    {
        if(!Resolve(uid, ref metaData, false))
            throw CompNotFound<MetaDataComponent>(uid);

        return metaData.EntityName;
    }

    /// <summary>
    ///     Retrieves the prototype of an entity, if any.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public EntityPrototype? Prototype(EntityUid uid, MetaDataComponent? metaData = null)
    {
        if (!Resolve(uid, ref metaData, false))
            throw CompNotFound<MetaDataComponent>(uid);

        return metaData.EntityPrototype;
    }

    /// <summary>
    ///     Retrieves the last <see cref="GameTick"/> the entity was modified at.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    public GameTick LastModifiedTick(EntityUid uid, MetaDataComponent? metaData = null)
    {
        if (!Resolve(uid, ref metaData, false))
            throw CompNotFound<MetaDataComponent>(uid);

        return metaData.EntityLastModifiedTick;
    }

    /// <summary>
    ///     Retrieves whether the entity is paused or not.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool Paused(EntityUid uid, MetaDataComponent? metaData = null)
    {
        if (!Resolve(uid, ref metaData, false))
            throw CompNotFound<MetaDataComponent>(uid);

        return metaData.EntityPaused;
    }

    /// <summary>
    ///     Sets the paused status on an entity.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SetPaused(EntityUid uid, bool paused, MetaDataComponent? metaData = null)
    {
        if (!Resolve(uid, ref metaData, false))
            throw CompNotFound<MetaDataComponent>(uid);

        metaData.EntityPaused = paused;
    }

    /// <summary>
    ///     Attempts to mark an entity as dirty.
    /// </summary>
    /// <returns>Whether the operation succeeded.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryDirty(EntityUid uid)
    {
        if (!Exists(uid))
            return false;

        Dirty(uid);
        return true;
    }

    /// <summary>
    ///     Attempts to retrieve the name of an entity.
    /// </summary>
    /// <returns>Whether the name could be retrieved.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryName(EntityUid uid, [NotNullWhen(true)] out string? name, MetaDataComponent? metaData = null)
    {
        if (!Resolve(uid, ref metaData, false))
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
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryDescription(EntityUid uid, [NotNullWhen(true)] out string? description, MetaDataComponent? metaData = null)
    {
        if (!Resolve(uid, ref metaData, false))
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
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryPrototype(EntityUid uid, [NotNullWhen(true)] out EntityPrototype? prototype, MetaDataComponent? metaData = null)
    {
        if (!Resolve(uid, ref metaData, false))
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
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryLastModifiedTick(EntityUid uid, [NotNullWhen(true)] out GameTick? lastModifiedTick, MetaDataComponent? metaData = null)
    {
        if (!Resolve(uid, ref metaData, false))
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
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryPaused(EntityUid uid, [NotNullWhen(true)] out bool? paused, MetaDataComponent? metaData = null)
    {
        if (!Resolve(uid, ref metaData, false))
        {
            paused = null;
            return false;
        }

        paused = metaData.EntityPaused;
        return true;
    }

    /// <summary>
    ///     Attempts to set the paused status on an entity.
    /// </summary>
    /// <returns>Whether the paused status could be set.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TrySetPaused(EntityUid uid, bool paused, MetaDataComponent? metaData = null)
    {
        if (!Resolve(uid, ref metaData, false))
            return false;

        metaData.EntityPaused = paused;
        return true;
    }

    /// <inheritdoc cref="IEntityManager.ToPrettyString"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public EntityStringRepresentation ToPrettyString(EntityUid uid)
    {
        return EntityManager.ToPrettyString(uid);
    }

    #endregion

    #region Component Get

    /// <inheritdoc cref="IEntityManager.GetComponent&lt;T&gt;"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public T Comp<T>(EntityUid uid) where T : class, IComponent
    {
        return EntityManager.GetComponent<T>(uid);
    }

    /// <summary>
    ///     Returns the component of a specific type, or null when it's missing or the entity does not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public T? CompOrNull<T>(EntityUid uid) where T : class, IComponent
    {
        return EntityManager.GetComponentOrNull<T>(uid);
    }

    /// <inheritdoc cref="IEntityManager.TryGetComponent&lt;T&gt;"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryComp<T>(EntityUid uid, [NotNullWhen(true)] out T? comp)
    {
        return EntityManager.TryGetComponent(uid, out comp);
    }

    /// <inheritdoc cref="IEntityManager.GetComponents"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public IEnumerable<IComponent> AllComps(EntityUid uid)
    {
        return EntityManager.GetComponents(uid);
    }

    /// <inheritdoc cref="IEntityManager.GetComponents&lt;T&gt;"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public IEnumerable<T> AllComps<T>(EntityUid uid)
    {
        return EntityManager.GetComponents<T>(uid);
    }

    /// <summary>
    ///     Returns the <see cref="TransformComponent"/> on an entity.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TransformComponent Transform(EntityUid uid)
    {
        return EntityManager.GetComponent<TransformComponent>(uid);
    }

    /// <summary>
    ///     Returns the <see cref="MetaDataComponent"/> on an entity.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the entity doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public MetaDataComponent MetaData(EntityUid uid)
    {
        return EntityManager.GetComponent<MetaDataComponent>(uid);
    }

    #endregion

    #region Component Has

    /// <summary>
    ///     Retrieves whether the entity has the specified component or not.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool HasComp<T>(EntityUid uid)
    {
        return EntityManager.HasComponent<T>(uid);
    }

    /// <summary>
    ///     Retrieves whether the entity has the specified component or not.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool HasComp(EntityUid uid, Type type)
    {
        return EntityManager.HasComponent(uid, type);
    }

    #endregion

    #region Component Add

    /// <inheritdoc cref="IEntityManager.AddComponent&lt;T&gt;(EntityUid)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void AddComp<T>(EntityUid uid) where T :  Component, new()
    {
        EntityManager.AddComponent<T>(uid);
    }

    /// <inheritdoc cref="IEntityManager.EnsureComponent&lt;T&gt;(EntityUid)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public T EnsureComp<T>(EntityUid uid) where T : Component, new()
    {
        return EntityManager.EnsureComponent<T>(uid);
    }

    #endregion

    #region Component Remove

    /// <inheritdoc cref="IEntityManager.RemoveComponent&lt;T&gt;(EntityUid)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void RemComp<T>(EntityUid uid) where T : class, IComponent
    {
        EntityManager.RemoveComponent<T>(uid);
    }

    #endregion

    #region Entity Delete

    /// <inheritdoc cref="IEntityManager.DeleteEntity(EntityUid)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Del(EntityUid uid)
    {
        EntityManager.DeleteEntity(uid);
    }

    /// <inheritdoc cref="IEntityManager.QueueDeleteEntity(EntityUid)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void QueueDel(EntityUid uid)
    {
        EntityManager.QueueDeleteEntity(uid);
    }

    #endregion

    #region Entity Spawning

    /// <inheritdoc cref="IEntityManager.SpawnEntity(string?, EntityCoordinates)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public EntityUid Spawn(string? prototype, EntityCoordinates coordinates)
    {
        return EntityManager.SpawnEntity(prototype, coordinates).Uid;
    }

    /// <inheritdoc cref="IEntityManager.SpawnEntity(string?, MapCoordinates)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public EntityUid Spawn(string? prototype, MapCoordinates coordinates)
    {
        return EntityManager.SpawnEntity(prototype, coordinates).Uid;
    }

    #endregion

    #region Utils

    /// <summary>
    ///     Utility static method to create an exception to be thrown when an entity doesn't have a specific component.
    /// </summary>
    private static KeyNotFoundException CompNotFound<T>(EntityUid uid)
        => throw new KeyNotFoundException($"Entity {uid} does not have a component of type {typeof(T)}");

    #endregion
}
