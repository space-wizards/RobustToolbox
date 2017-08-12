using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.Interfaces.GameObjects
{
    /// <summary>
    /// An entity query is a type that can filter entities based on certain criteria,
    /// for example based on the components that the entity has.
    /// </summary>
    public interface IEntityQuery
    {
        /// <summary>
        /// Match the entity and see if it passes the criteria.
        /// </summary>
        /// <param name="entity">The entity to test.</param>
        /// <returns>True if the entity is included in this query, false otherwise</returns>
        bool Match(IEntity entity);
    }
}
