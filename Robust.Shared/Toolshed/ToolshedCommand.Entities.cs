using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Toolshed.Errors;

namespace Robust.Shared.Toolshed;

public abstract partial class ToolshedCommand
{
    [PublicAPI, IoC.Dependency]
    protected readonly IEntityManager EntityManager = default!;

    [PublicAPI, IoC.Dependency]
    protected readonly IEntitySystemManager EntitySystemManager = default!;

    /// <summary>
    ///     Returns the entity that's executing this command, if any.
    /// </summary>
    [PublicAPI]
    protected EntityUid? ExecutingEntity(IInvocationContext ctx)
    {
        if (ctx.Session is null)
        {
            ctx.ReportError(new NotForServerConsoleError());
            return null;
        }

        if (ctx.Session.AttachedEntity is { } ent)
            return ent;

        ctx.ReportError(new SessionHasNoEntityError(ctx.Session));
        return null;
    }

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
    ///     A shorthand for spawning an entity.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid Spawn(string? proto, EntityCoordinates coords)
        => EntityManager.SpawnEntity(proto, coords);

    /// <summary>
    ///     A shorthand for spawning an entity.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityUid Spawn(string? proto, MapCoordinates coords)
        => EntityManager.SpawnEntity(proto, coords);

    /// <summary>
    ///     A shorthand for deleting an entity.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Del(EntityUid entityUid)
        => EntityManager.DeleteEntity(entityUid);


    /// <summary>
    ///     A shorthand for queueing the deletion of an entity.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void QDel(EntityUid entityUid)
        => EntityManager.QueueDeleteEntity(entityUid);

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
    protected bool TryComp<T>([NotNullWhen(true)] EntityUid? entity, [NotNullWhen(true)] out T? component)
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
        where T : IComponent, new()
        => EntityManager.AddComponent<T>(entity);

    /// <summary>
    ///     A shorthand for removing a component from the given entity.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RemComp<T>(EntityUid entity)
        where T : IComponent
        => EntityManager.RemoveComponent<T>(entity);

    /// <summary>
    ///     A shorthand for ensuring an entity has the given component.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T EnsureComp<T>(EntityUid entity)
        where T: IComponent, new()
        => EntityManager.EnsureComponent<T>(entity);

    /// <summary>
    ///     A shorthand for retrieving an entity system.
    /// </summary>
    /// <remarks>This may be replaced with some form of dependency attribute in the future.</remarks>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T GetSys<T>()
        where T: EntitySystem
        => EntitySystemManager.GetEntitySystem<T>();

    // GetSys is just too many letters to type
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T Sys<T>() where T: EntitySystem => EntitySystemManager.GetEntitySystem<T>();

    /// <summary>
    ///     A shorthand for retrieving an entity query.
    /// </summary>
    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityQuery<T> GetEntityQuery<T>()
        where T : IComponent
        => EntityManager.GetEntityQuery<T>();
}
