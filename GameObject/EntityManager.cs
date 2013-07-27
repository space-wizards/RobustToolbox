using System.Collections.Generic;
using System.Linq;

namespace GameObject
{
    public class EntityManager
    {
        protected readonly Dictionary<int, Entity> _entities = new Dictionary<int, Entity>();
        private string _componentNamespace;
        public ComponentFactory ComponentFactory { get; private set; }

        public EntityManager(string componentNamespace)
        {
            _componentNamespace = componentNamespace;
            ComponentFactory = new ComponentFactory(this, _componentNamespace);
        }

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
