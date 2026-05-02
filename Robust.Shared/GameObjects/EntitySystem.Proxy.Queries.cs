using System;
using JetBrains.Annotations;

namespace Robust.Shared.GameObjects;

public partial class EntitySystem
{
    /// <inheritdoc cref="M:Robust.Shared.GameObjects.EntityManager.GetDynamicQuery"/>
    [ProxyFor(typeof(EntityManager))]
    [Pure]
    protected DynamicEntityQuery GetDynamicQuery(params (Type, DynamicEntityQuery.QueryFlags)[] userEntries)
    {
        return EntityManager.GetDynamicQuery(userEntries);
    }

    /// <inheritdoc cref="M:Robust.Shared.GameObjects.EntityManager.GetEntityQuery``1"/>
    [ProxyFor(typeof(EntityManager))]
    [Pure]
    protected EntityQuery<TComp1> GetEntityQuery<TComp1>()
        where TComp1 : IComponent
    {
        return EntityManager.GetEntityQuery<TComp1>();
    }

    /// <inheritdoc cref="M:Robust.Shared.GameObjects.EntityManager.GetEntityQuery``2"/>
    [ProxyFor(typeof(EntityManager))]
    [Pure]
    protected EntityQuery<TComp1, TComp2> GetEntityQuery<TComp1, TComp2>()
        where TComp1 : IComponent
        where TComp2 : IComponent
    {
        return EntityManager.GetEntityQuery<TComp1, TComp2>();
    }

    /// <inheritdoc cref="M:Robust.Shared.GameObjects.EntityManager.GetEntityQuery``3"/>
    [ProxyFor(typeof(EntityManager))]
    [Pure]
    protected EntityQuery<TComp1, TComp2, TComp3> GetEntityQuery<TComp1, TComp2, TComp3>()
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
    {
        return EntityManager.GetEntityQuery<TComp1, TComp2, TComp3>();
    }

    /// <inheritdoc cref="M:Robust.Shared.GameObjects.EntityManager.GetEntityQuery``4"/>
    [ProxyFor(typeof(EntityManager))]
    [Pure]
    protected EntityQuery<TComp1, TComp2, TComp3, TComp4> GetEntityQuery<TComp1, TComp2, TComp3, TComp4>()
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent
    {
        return EntityManager.GetEntityQuery<TComp1, TComp2, TComp3, TComp4>();
    }
}
