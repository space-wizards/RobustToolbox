using System;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.GameObjects;

public partial interface IEntityManager
{
    #region HasComp

    public bool HasComponent<TComp1, TComp2>(EntityUid? uid);

    public bool HasComponent<TComp1, TComp2, TComp3>(EntityUid? uid);

    public bool HasComponent<TComp1, TComp2, TComp3, TComp4>(EntityUid? uid);

    public bool HasComponent<TComp1, TComp2>(EntityUid uid);

    public bool HasComponent<TComp1, TComp2, TComp3>(EntityUid uid);

    public bool HasComponent<TComp1, TComp2, TComp3, TComp4>(EntityUid uid);

    public bool HasComponent(EntityUid? uid, Type comp1, Type comp2);

    public bool HasComponent(EntityUid? uid, Type comp1, Type comp2, Type comp3);

    public bool HasComponent(EntityUid? uid, Type comp1, Type comp2, Type comp3, Type comp4);

    public bool HasComponent(EntityUid uid, Type comp1, Type comp2);

    public bool HasComponent(EntityUid uid, Type comp1, Type comp2, Type comp3);

    public bool HasComponent(EntityUid uid, Type comp1, Type comp2, Type comp3, Type comp4);

    #endregion

    #region TryComp

    public bool TryGetComponent<TComp1, TComp2>(EntityUid? uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2);

    public bool TryGetComponent<TComp1, TComp2, TComp3>(EntityUid? uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2,
        [NotNullWhen(true)] out TComp3? comp3);

    public bool TryGetComponent<TComp1, TComp2, TComp3, TComp4>(EntityUid? uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2,
        [NotNullWhen(true)] out TComp3? comp3,
        [NotNullWhen(true)] out TComp4? comp4);

    public bool TryGetComponent<TComp1, TComp2>(EntityUid uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2);

    public bool TryGetComponent<TComp1, TComp2, TComp3>(EntityUid uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2,
        [NotNullWhen(true)] out TComp3? comp3);

    public bool TryGetComponent<TComp1, TComp2, TComp3, TComp4>(EntityUid uid,
        [NotNullWhen(true)] out TComp1? comp1,
        [NotNullWhen(true)] out TComp2? comp2,
        [NotNullWhen(true)] out TComp3? comp3,
        [NotNullWhen(true)] out TComp4? comp4);

    public bool TryGetComponent(EntityUid? uid,
        Type type1,
        Type type2,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2);

    public bool TryGetComponent(EntityUid? uid,
        Type type1,
        Type type2,
        Type type3,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2,
        [NotNullWhen(true)] out IComponent? comp3);

    public bool TryGetComponent(EntityUid? uid,
        Type type1,
        Type type2,
        Type type3,
        Type type4,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2,
        [NotNullWhen(true)] out IComponent? comp3,
        [NotNullWhen(true)] out IComponent? comp4);

    public bool TryGetComponent(EntityUid uid,
        Type type1,
        Type type2,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2);

    public bool TryGetComponent(EntityUid uid,
        Type type1,
        Type type2,
        Type type3,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2,
        [NotNullWhen(true)] out IComponent? comp3);

    public bool TryGetComponent(EntityUid uid,
        Type type1,
        Type type2,
        Type type3,
        Type type4,
        [NotNullWhen(true)] out IComponent? comp1,
        [NotNullWhen(true)] out IComponent? comp2,
        [NotNullWhen(true)] out IComponent? comp3,
        [NotNullWhen(true)] out IComponent? comp4);

    #endregion
}
