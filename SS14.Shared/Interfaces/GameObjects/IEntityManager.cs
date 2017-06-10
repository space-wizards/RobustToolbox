using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.IoC;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.System;

namespace SS14.Shared.Interfaces.GameObjects
{
    public interface IEntityManager : IIoCInterface
    {
        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="eid">entity id</param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        IEntity GetEntity(int eid);

        IEnumerable<IEntity> GetEntities(IEntityQuery query);

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        void DeleteEntity(IEntity e);

        /// <summary>
        /// Checks whether an entity with the specified ID exists.
        /// </summary>
        bool EntityExists(int eid);
    }
}
