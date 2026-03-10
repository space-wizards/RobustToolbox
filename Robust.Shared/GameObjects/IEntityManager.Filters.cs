using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;

namespace Robust.Shared.GameObjects;

public partial interface IEntityManager
{
    /// <summary>
    ///     Tests whether an entity matches a filter, i.e. its components are a superset of the filter's.
    /// </summary>
    /// <param name="ent">The entity to test against.</param>
    /// <param name="filter">The filter to use.</param>
    /// <returns>True if match, false if not.</returns>
    public bool MatchesFilter(EntityUid ent, ComponentFilter filter);

    /// <summary>
    ///     Tests whether an entity matches a filter exactly, i.e. its components are identical to the filter's.
    /// </summary>
    /// <param name="ent">The entity to test against.</param>
    /// <param name="filter">The filter to use.</param>
    /// <returns>True if match, false if not.</returns>
    /// <remarks>
    ///     For performance and technical reasons, this matches the exact type, not any shared component.
    /// </remarks>
    public bool ExactlyMatchesFilter(EntityUid ent, ComponentFilter filter);

    /// <summary>
    ///     Enumerates all the components the filter has, but the entity does not.
    /// </summary>
    /// <param name="ent">The entity to test against.</param>
    /// <param name="filter">The filter to use.</param>
    /// <returns>A list of component types.</returns>
    public IEnumerable<Type> EnumerateFilterMisses(EntityUid ent, ComponentFilter filter);

    /// <summary>
    ///     Enumerates all the components the entity has, but the filter does not.
    /// </summary>
    /// <param name="ent">The entity to test against.</param>
    /// <param name="filter">The filter to use.</param>
    /// <returns>A list of component types.</returns>
    public IEnumerable<Type> EnumerateEntityMisses(EntityUid ent, ComponentFilter filter);

    /// <summary>
    ///     Enumerates all components the filter and the entity have in common.
    /// </summary>
    /// <param name="ent">The entity to test against.</param>
    /// <param name="filter">The filter to use.</param>
    /// <returns>A list of component types.</returns>
    public IEnumerable<Type> EnumerateFilterHits(EntityUid ent, ComponentFilter filter);

    /// <summary>
    ///     Computes the filter misses for the given entity, and then looks up those components from a registry to add them.
    /// </summary>
    /// <param name="ent">The entity to test against.</param>
    /// <param name="filter">The filter to use.</param>
    /// <param name="registry">The registry to introduce components from.</param>
    /// <remarks>
    ///     Like all filter methods, this is <i>dynamic</i> and is slower than using EnsureComp if you know the types in advance.
    /// </remarks>
    public void FillMissesFromRegistry(EntityUid ent, ComponentFilter filter, ComponentRegistry registry);

    /// <summary>
    ///     Computes the filter misses for the given entity, and then adds the missing components.
    /// </summary>
    /// <param name="ent">The entity to test against.</param>
    /// <param name="filter">The filter to use.</param>
    /// <remarks>
    ///     Like all filter methods, this is <i>dynamic</i> and is slower than using EnsureComp if you know the types in advance.
    /// </remarks>
    public void FillMissesWithNewComponents(EntityUid ent, ComponentFilter filter);
}
