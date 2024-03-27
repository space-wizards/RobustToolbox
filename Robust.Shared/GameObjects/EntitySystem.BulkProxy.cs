using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Robust.Shared.GameObjects;

public partial class EntitySystem
{
    #region HasComp

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComp<TComp1, TComp2>(EntityUid? uid)
    {
        if (uid == null)
            return false;

        return EntityManager.HasComponent<TComp1, TComp2>(uid.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComp<TComp1, TComp2, TComp3>(EntityUid? uid)
    {
        return EntityManager.HasComponent<TComp1, TComp2, TComp3>(uid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<TComp1, TComp2, TComp3, TComp4>(EntityUid? uid)
    {
        return EntityManager.HasComponent<TComp1, TComp2, TComp3, TComp4>(uid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<TComp1, TComp2>(EntityUid uid)
    {
        return EntityManager.HasComponent<TComp1, TComp2>(uid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<TComp1, TComp2, TComp3>(EntityUid uid)
    {
        return EntityManager.HasComponent<TComp1, TComp2, TComp3>(uid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<TComp1, TComp2, TComp3, TComp4>(EntityUid uid)
    {
        return EntityManager.HasComponent<TComp1, TComp2, TComp3, TComp4>(uid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(EntityUid? uid, Type comp1, Type comp2)
    {
        return EntityManager.HasComponent(uid, comp1, comp2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(EntityUid? uid, Type comp1, Type comp2, Type comp3)
    {
        return EntityManager.HasComponent(uid, comp1, comp2, comp3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(EntityUid? uid, Type comp1, Type comp2, Type comp3, Type comp4)
    {
        return EntityManager.HasComponent(uid, comp1, comp2, comp3, comp4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(EntityUid uid, Type comp1, Type comp2)
    {
        return EntityManager.HasComponent(uid, comp1, comp2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(EntityUid uid, Type comp1, Type comp2, Type comp3)
    {
        return EntityManager.HasComponent(uid, comp1, comp2, comp3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(EntityUid uid, Type comp1, Type comp2, Type comp3, Type comp4)
    {
        return EntityManager.HasComponent(uid, comp1, comp2, comp3, comp4);
    }

    #endregion

    #region TryComp

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent<TComp1, TComp2>(EntityUid? uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2)
    {
        return EntityManager.TryGetComponent(uid, out comp1, out comp2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent<TComp1, TComp2, TComp3>(EntityUid? uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2,
        [NotNullWhen(true)] out TComp3? comp3)
    {
        return EntityManager.TryGetComponent(uid, out comp1, out comp2, out comp3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent<TComp1, TComp2, TComp3, TComp4>(EntityUid? uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2,
        [NotNullWhen(true)] out TComp3? comp3,
        [NotNullWhen(true)] out TComp4? comp4)
    {
        return EntityManager.TryGetComponent(uid, out comp1, out comp2, out comp3, out comp4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent<TComp1, TComp2>(EntityUid uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2)
    {
        return EntityManager.TryGetComponent(uid, out comp1, out comp2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent<TComp1, TComp2, TComp3>(EntityUid uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2,
        [NotNullWhen(true)] out TComp3? comp3)
    {
        return EntityManager.TryGetComponent(uid, out comp1, out comp2, out comp3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent<TComp1, TComp2, TComp3, TComp4>(EntityUid uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2,
        [NotNullWhen(true)] out TComp3? comp3,
        [NotNullWhen(true)] out TComp4? comp4)
    {
        return EntityManager.TryGetComponent(uid, out comp1, out comp2, out comp3, out comp4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent(EntityUid? uid,
        Type type1,
        Type type2,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2)
    {
        return EntityManager.TryGetComponent(uid, out comp1, out comp2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent(EntityUid? uid,
        Type type1,
        Type type2,
        Type type3,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2,
        [NotNullWhen(true)] out IComponent? comp3)
    {
        return EntityManager.TryGetComponent(uid, out comp1, out comp2, out comp3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent(EntityUid? uid,
        Type type1,
        Type type2,
        Type type3,
        Type type4,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2,
        [NotNullWhen(true)] out IComponent? comp3,
        [NotNullWhen(true)] out IComponent? comp4)
    {
        return EntityManager.TryGetComponent(uid, out comp1, out comp2, out comp3, out comp4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent(EntityUid uid,
        Type type1,
        Type type2,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2)
    {
        return EntityManager.TryGetComponent(uid, type1, type2, out comp1, out comp2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent(EntityUid uid,
        Type type1,
        Type type2,
        Type type3,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2,
        [NotNullWhen(true)] out IComponent? comp3)
    {
        return EntityManager.TryGetComponent(uid, type1, type2, type3, out comp1, out comp2, out comp3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent(EntityUid uid,
        Type type1,
        Type type2,
        Type type3,
        Type type4,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2,
        [NotNullWhen(true)] out IComponent? comp3,
        [NotNullWhen(true)] out IComponent? comp4)
    {
        return EntityManager.TryGetComponent(uid, type1, type2, type3, type4, out comp1, out comp2, out comp3, out comp4);
    }

    #endregion
}
