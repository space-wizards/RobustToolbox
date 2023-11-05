using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    /*
     * Ideally this would be sourcegenned but my PC seems physically incapable of calling Debugger.Launcher
     * without dotnet shitting itself so we get this so content at least has some API to use until archetypes.
     */

    #region HasComp

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<TComp1, TComp2>(EntityUid? uid)
    {
        if (uid == null)
            return false;

        return HasComponent<TComp1, TComp2>(uid.Value);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<TComp1, TComp2, TComp3>(EntityUid? uid)
    {
        if (uid == null)
            return false;

        return HasComponent<TComp1, TComp2, TComp3>(uid.Value);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<TComp1, TComp2, TComp3, TComp4>(EntityUid? uid)
    {
        if (uid == null)
            return false;

        return HasComponent<TComp1, TComp2, TComp3, TComp4>(uid.Value);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<TComp1, TComp2>(EntityUid uid)
    {
        return HasComponent<TComp1>(uid) &&
               HasComponent<TComp2>(uid);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<TComp1, TComp2, TComp3>(EntityUid uid)
    {
        return HasComponent<TComp1>(uid) &&
               HasComponent<TComp2>(uid) &&
               HasComponent<TComp3>(uid);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<TComp1, TComp2, TComp3, TComp4>(EntityUid uid)
    {
        return HasComponent<TComp1>(uid) &&
               HasComponent<TComp2>(uid) &&
               HasComponent<TComp3>(uid) &&
               HasComponent<TComp4>(uid);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(EntityUid? uid, Type comp1, Type comp2)
    {
        if (uid == null)
            return false;

        return HasComponent(uid.Value, comp1, comp2);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(EntityUid? uid, Type comp1, Type comp2, Type comp3)
    {
        if (uid == null)
            return false;

        return HasComponent(uid.Value, comp1, comp2, comp3);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(EntityUid? uid, Type comp1, Type comp2, Type comp3, Type comp4)
    {
        if (uid == null)
            return false;

        return HasComponent(uid.Value, comp1, comp2, comp3, comp4);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(EntityUid uid, Type comp1, Type comp2)
    {
        return HasComponent(uid, comp1) &&
               HasComponent(uid, comp2);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(EntityUid uid, Type comp1, Type comp2, Type comp3)
    {
        return HasComponent(uid, comp1) &&
               HasComponent(uid, comp2) &&
               HasComponent(uid, comp3);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(EntityUid uid, Type comp1, Type comp2, Type comp3, Type comp4)
    {
        return HasComponent(uid, comp1) &&
               HasComponent(uid, comp2) &&
               HasComponent(uid, comp3) &&
               HasComponent(uid, comp4);
    }

    #endregion

    #region TryComp

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent<TComp1, TComp2>(EntityUid? uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2)
    {
        comp1 = default;
        comp2 = default;

        if (uid == null)
            return false;

        return TryGetComponent(uid.Value, out comp1, out comp2);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent<TComp1, TComp2, TComp3>(EntityUid? uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2,
        [NotNullWhen(true)] out TComp3? comp3)
    {
        comp1 = default;
        comp2 = default;
        comp3 = default;

        if (uid == null)
            return false;

        return TryGetComponent(uid.Value, out comp1, out comp2, out comp3);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent<TComp1, TComp2, TComp3, TComp4>(EntityUid? uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2,
        [NotNullWhen(true)] out TComp3? comp3,
        [NotNullWhen(true)] out TComp4? comp4)
    {
        comp1 = default;
        comp2 = default;
        comp3 = default;
        comp4 = default;

        if (uid == null)
            return false;

        return TryGetComponent(uid.Value, out comp1, out comp2, out comp3, out comp4);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent<TComp1, TComp2>(EntityUid uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2)
    {
        comp1 = default;
        comp2 = default;

        return TryGetComponent(uid, out comp1) &&
               TryGetComponent(uid, out comp2);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent<TComp1, TComp2, TComp3>(EntityUid uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2,
        [NotNullWhen(true)] out TComp3? comp3)
    {
        comp1 = default;
        comp2 = default;
        comp3 = default;

        return TryGetComponent(uid, out comp1) &&
               TryGetComponent(uid, out comp2) &&
               TryGetComponent(uid, out comp3);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent<TComp1, TComp2, TComp3, TComp4>(EntityUid uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2,
        [NotNullWhen(true)] out TComp3? comp3,
        [NotNullWhen(true)] out TComp4? comp4)
    {
        comp1 = default;
        comp2 = default;
        comp3 = default;
        comp4 = default;

        return TryGetComponent(uid, out comp1) &&
               TryGetComponent(uid, out comp2) &&
               TryGetComponent(uid, out comp3) &&
               TryGetComponent(uid, out comp4);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent(EntityUid? uid,
        Type type1,
        Type type2,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2)
    {
        comp1 = default;
        comp2 = default;

        return TryGetComponent(uid, type1, out comp1) &&
               TryGetComponent(uid, type2, out comp2);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent(EntityUid? uid,
        Type type1,
        Type type2,
        Type type3,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2,
        [NotNullWhen(true)] out IComponent? comp3)
    {
        comp1 = default;
        comp2 = default;
        comp3 = default;

        return TryGetComponent(uid, type1, out comp1) &&
               TryGetComponent(uid, type2, out comp2) &&
               TryGetComponent(uid, type3, out comp3);
    }

    /// <inheritdoc />
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
        comp1 = default;
        comp2 = default;
        comp3 = default;
        comp4 = default;

        return TryGetComponent(uid, type1, out comp1) &&
               TryGetComponent(uid, type2, out comp2) &&
               TryGetComponent(uid, type3, out comp3) &&
               TryGetComponent(uid, type3, out comp4);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent(EntityUid uid,
        Type type1,
        Type type2,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2)
    {
        comp1 = default;
        comp2 = default;

        return TryGetComponent(uid, type1, out comp1) &&
               TryGetComponent(uid, type2, out comp2);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent(EntityUid uid,
        Type type1,
        Type type2,
        Type type3,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2,
        [NotNullWhen(true)] out IComponent? comp3)
    {
        comp1 = default;
        comp2 = default;
        comp3 = default;

        return TryGetComponent(uid, type1, out comp1) &&
               TryGetComponent(uid, type2, out comp2) &&
               TryGetComponent(uid, type3, out comp3);
    }

    /// <inheritdoc />
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
        comp1 = default;
        comp2 = default;
        comp3 = default;
        comp4 = default;

        return TryGetComponent(uid, type1, out comp1) &&
               TryGetComponent(uid, type2, out comp2) &&
               TryGetComponent(uid, type3, out comp3) &&
               TryGetComponent(uid, type3, out comp4);
    }

    #endregion
}
