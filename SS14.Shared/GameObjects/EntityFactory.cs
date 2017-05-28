using SS14.Shared.Prototypes;

namespace SS14.Shared.GameObjects
{
    public class EntityFactory
    {
        private readonly IPrototypeManager PrototypeManager;
        private readonly EntityManager Manager;

        /// <summary>
        /// Constructor
        /// </summary>
        public EntityFactory(IPrototypeManager prototypeManager, EntityManager manager)
        {
            PrototypeManager = prototypeManager;
            Manager = manager;
        }

        /// <summary>
        /// Creates an entity from a template pulled from the entitydb
        /// </summary>
        /// <param name="prototypeName">name of the template</param>
        /// <returns>Created Entity</returns>
        public Entity CreateEntity(string prototypeName)
        {
            EntityPrototype prototype = PrototypeManager.Index<EntityPrototype>(prototypeName);
            //TODO: Throw exception here
            return prototype.CreateEntity(Manager);
        }

        /// <summary>
        /// Retrieves template with given name from db
        /// </summary>
        /// <param name="prototypeName">name of the template</param>
        /// <returns>Template</returns>
        public EntityPrototype GetTemplate(string prototypeName) => PrototypeManager.Index<EntityPrototype>(prototypeName);
    }
}
