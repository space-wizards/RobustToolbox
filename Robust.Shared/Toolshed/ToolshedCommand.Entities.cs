using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Toolshed;

public abstract partial class ToolshedCommand
{
    [PublicAPI, IoC.Dependency]
    protected readonly IEntityManager EntityManager = default!;

    [PublicAPI, IoC.Dependency]
    protected readonly IEntitySystemManager EntitySystemManager = default!;

    /// <summary>
    ///     A shorthand for retrieving <see cref="MetaDataComponent"/> for an entity.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected MetaDataComponent MetaData(EntityUid entity)
        => EntityManager.GetComponent<MetaDataComponent>(entity);

    /// <summary>
    ///     A shorthand for retrieving <see cref="TransformComponent"/> for an entity.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected TransformComponent Transform(EntityUid entity)
        => EntityManager.GetComponent<TransformComponent>(entity);

    /// <summary>
    ///     A shorthand for retrieving an entity's name.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected string EntName(EntityUid entity)
        => EntityManager.GetComponent<MetaDataComponent>(entity).EntityName;

    /// <summary>
    ///     A shorthand for deleting an entity.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Del(EntityUid entityUid)
        => EntityManager.DeleteEntity(entityUid);

    /// <summary>
    ///     A shorthand for checking if an entity is deleted or otherwise non-existant.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool Deleted(EntityUid entity)
        => EntityManager.Deleted(entity);

    /// <summary>
    ///     A shorthand for retrieving the given component for an entity.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T Comp<T>(EntityUid entity)
        where T: IComponent
        => EntityManager.GetComponent<T>(entity);

    /// <summary>
    ///     A shorthand for checking if an entity has the given component.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool HasComp<T>(EntityUid entityUid)
        where T: IComponent
        => EntityManager.HasComponent<T>(entityUid);

    /// <summary>
    ///     A shorthand for attempting to retrieve the given component for an entity.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryComp<T>(EntityUid? entity, [NotNullWhen(true)] out T? component)
        where T: IComponent
        => EntityManager.TryGetComponent(entity, out component);

    /// <inheritdoc cref="TryComp{T}(System.Nullable{Robust.Shared.GameObjects.EntityUid},out T?)"/>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryComp<T>(EntityUid entity, [NotNullWhen(true)] out T? component)
        where T: IComponent
        => EntityManager.TryGetComponent(entity, out component);

    /// <summary>
    ///     A shorthand for adding a component to the given entity.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T AddComp<T>(EntityUid entity)
        where T : Component, new()
        => EntityManager.AddComponent<T>(entity);

    /// <summary>
    ///     A shorthand for ensuring an entity has the given component.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T EnsureComp<T>(EntityUid entity)
        where T: Component, new()
        => EntityManager.EnsureComponent<T>(entity);

    /// <summary>
    ///     A shorthand for retrieving an entity system.
    /// </summary>
    /// <remarks>This may be replaced with some form of dependency attribute in the future.</remarks>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T GetSys<T>()
        where T: EntitySystem
        => EntitySystemManager.GetEntitySystem<T>();

    /// <summary>
    ///     A shorthand for retrieving an entity query.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityQuery<T> GetEntityQuery<T>()
        where T : Component
        => EntityManager.GetEntityQuery<T>();
}
