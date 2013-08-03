using System.Collections.Generic;
using System.Linq;
using SS13_Shared;

namespace GameObject
{
    public interface IEntityManager
    {
        ComponentFactory ComponentFactory { get; }
        ComponentManager ComponentManager { get; }
        IEntityNetworkManager EntityNetworkManager { get; set; }
        EngineType EngineType { get; set; }

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="eid">entity id</param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        Entity GetEntity(int eid);

        List<Entity> GetEntities(EntityQuery query);

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        void DeleteEntity(Entity e);

        bool EntityExists(int eid);
    }

    public class EntityManager : IEntityManager
    {
        protected readonly Dictionary<int, Entity> _entities = new Dictionary<int, Entity>();
        private string _componentNamespace;
        public ComponentFactory ComponentFactory { get; private set; }
        public ComponentManager ComponentManager { get; private set; }
        public IEntityNetworkManager EntityNetworkManager { get; set; }

        //This is a crude method to tell if we're running on the server or on the client. Fuck me.
        public EngineType EngineType { get; set; }

        public EntityManager(string componentNamespace, IEntityNetworkManager entityNetworkManager)
        {
            _componentNamespace = componentNamespace;
            ComponentFactory = new ComponentFactory(this, _componentNamespace);
            EntityNetworkManager = entityNetworkManager;
            ComponentManager = new ComponentManager();
        }

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="eid">entity id</param>
        /// <returns>Entity or null if entity id doesn't exist</returns>
        public Entity GetEntity(int eid)
        {
            return _entities.ContainsKey(eid) ? _entities[eid] : null;
        }


        public List<Entity> GetEntities(EntityQuery query)
        {
            return _entities.Values.Where(e => e.Match(query)).ToList();
        }

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        public void DeleteEntity(Entity e)
        {
            e.Shutdown();
            _entities.Remove(e.Uid);
        }

        protected void DeleteEntity(int entityUid)
        {
            if (EntityExists(entityUid))
                DeleteEntity(GetEntity(entityUid));
        }
        
        public bool EntityExists(int eid)
        {
            return _entities.ContainsKey(eid);
        }
    }
}
