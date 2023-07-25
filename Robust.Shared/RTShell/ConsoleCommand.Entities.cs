using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.RTShell;

public abstract partial class RtShellCommand
{
    [PublicAPI, IoC.Dependency]
    protected readonly IEntityManager EntityManager = default!;

    [PublicAPI, IoC.Dependency]
    protected readonly IEntitySystemManager EntitySystemManager = default!;

    protected MetaDataComponent MetaData(EntityUid entity)
        => EntityManager.GetComponent<MetaDataComponent>(entity);

    protected TransformComponent Transform(EntityUid entity)
        => EntityManager.GetComponent<TransformComponent>(entity);

    protected string EntName(EntityUid entity)
        => EntityManager.GetComponent<MetaDataComponent>(entity).EntityName;

    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Del(EntityUid entityUid)
        => EntityManager.DeleteEntity(entityUid);

    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool Deleted(EntityUid entity)
        => EntityManager.Deleted(entity);

    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T Comp<T>(EntityUid entity)
        where T: IComponent
        => EntityManager.GetComponent<T>(entity);

    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool HasComp<T>(EntityUid entityUid)
        where T: IComponent
        => EntityManager.HasComponent<T>(entityUid);

    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryComp<T>(EntityUid? entity, [NotNullWhen(true)] out T? component)
        where T: IComponent
        => EntityManager.TryGetComponent(entity, out component);

    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryComp<T>(EntityUid entity, [NotNullWhen(true)] out T? component)
        where T: IComponent
        => EntityManager.TryGetComponent(entity, out component);

    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T AddComp<T>(EntityUid entity)
        where T : Component, new()
        => EntityManager.AddComponent<T>(entity);

    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T EnsureComp<T>(EntityUid entity)
        where T: Component, new()
        => EntityManager.EnsureComponent<T>(entity);

    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T GetSys<T>()
        where T: EntitySystem
        => EntitySystemManager.GetEntitySystem<T>();

    [PublicAPI, MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected EntityQuery<T> GetEntityQuery<T>()
        where T : Component
        => EntityManager.GetEntityQuery<T>();
}
