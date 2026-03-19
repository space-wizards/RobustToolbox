using System;

namespace Robust.Shared.GameObjects;

public partial interface IEntityManager
{
    /// <summary>
    ///     Returns a dynamic query over the given component list and properties.
    /// </summary>
    /// <param name="entries">The query entries to build into a cached query.</param>
    /// <returns>A constructed, optimized query for the given entries.</returns>
    /// <remarks>
    /// <para>
    ///     Currently, this is the only API that exposes the Optional and Without component query configurations.
    ///     A typed API to expose them is unfortunately TBD.
    /// </para>
    /// <para>
    ///     Having an Optional or Without entry as the first entry prevents you from enumerating the query.
    /// </para>
    /// </remarks>
    DynamicEntityQuery GetDynamicQuery(params (Type, DynamicEntityQuery.QueryFlags)[] entries);

    /// <summary>
    ///     Returns a query for entities with the given component.
    /// </summary>
    /// <seealso cref="T:Robust.Shared.GameObjects.EntityQuery`1"/>
    EntityQuery<TComp1> GetEntityQuery<TComp1>() where TComp1 : IComponent;

    /// <summary>
    ///     Returns a generic query for entities with the given component.
    /// </summary>
    /// <seealso cref="T:Robust.Shared.GameObjects.EntityQuery`1"/>
    /// <seealso cref="GetDynamicQuery"/>
    EntityQuery<IComponent> GetEntityQuery(Type type);

    /// <summary>
    ///     Returns a query for entities with the given components.
    /// </summary>
    /// <seealso cref="T:Robust.Shared.GameObjects.EntityQuery`2"/>
    EntityQuery<TComp1, TComp2> GetEntityQuery<TComp1, TComp2>()
        where TComp1 : IComponent
        where TComp2 : IComponent;

    /// <summary>
    ///     Returns a query for entities with the given components.
    /// </summary>
    /// <seealso cref="T:Robust.Shared.GameObjects.EntityQuery`3"/>
    EntityQuery<TComp1, TComp2, TComp3> GetEntityQuery<TComp1, TComp2, TComp3>()
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent;

    /// <summary>
    ///     Returns a query for entities with the given components.
    /// </summary>
    /// <seealso cref="T:Robust.Shared.GameObjects.EntityQuery`4"/>
    EntityQuery<TComp1, TComp2, TComp3, TComp4> GetEntityQuery<TComp1, TComp2, TComp3, TComp4>()
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent;
}
