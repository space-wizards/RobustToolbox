using System.Collections.Generic;
using System.Linq;

namespace GameObject
{
    public class EntityManager
    {
        private readonly Dictionary<int, Entity> _entities;
        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="eid">entity id</param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        public Entity GetEntity(int eid)
        {
            if (_entities.ContainsKey(eid))
                return _entities[eid];
            return null;
        }


        public List<Entity> GetEntities(EntityQuery query)
        {
            return _entities.Values.Where(e => e.Match(query)).ToList();
        } 
    }
}
