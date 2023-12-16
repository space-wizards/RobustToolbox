using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Variant of <see cref="EntitySystem"/> that has some convenience features for working with a specific component.
/// </summary>
public abstract partial class EntitySystem<TComp> : EntitySystem where TComp : IComponent, new()
{
    protected EntityQuery<TComp> Query;

    public override void Initialize()
    {
        base.Initialize();
        Query = GetEntityQuery<TComp>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool HasComp(EntityUid uid)
    {
        return Query.HasComponent(uid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected TComp Comp(EntityUid uid)
    {
        return Query.GetComponent(uid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryComp(EntityUid uid, [NotNullWhen(true)] out TComp? comp)
    {
        return Query.TryGetComponent(uid, out comp);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool Resolve(EntityUid uid, ref TComp? comp)
    {
        return Query.Resolve(uid, ref comp);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected TComp EnsureComp(EntityUid uid)
    {
        Query.TryGetComponent(uid, out var comp);
        return comp ?? EntityManager.AddComponent<TComp>(uid);
    }
}
