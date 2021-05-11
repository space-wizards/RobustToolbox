using System;
using System.Collections.Generic;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// An entity query is a type that can filter entities based on certain criteria,
    /// for example based on the components that the entity has.
    /// </summary>
    [Obsolete("Use any of the other entity query methods instead.")]
    public interface IEntityQuery
    {
        /// <summary>
        /// Match the entity and see if it passes the criteria.
        /// </summary>
        /// <param name="entity">The entity to test.</param>
        /// <returns>True if the entity is included in this query, false otherwise</returns>
        bool Match(IEntity entity);

        /// <summary>
        /// Matches every entity in an EntityManager to see if it passes the criteria.
        /// </summary>
        /// <param name="entityMan">An EntityManager containing a set of entities.</param>
        /// <returns>Enumeration of all entities that successfully matched the criteria.</returns>
        IEnumerable<IEntity> Match(IEntityManager entityMan);
    }
}
