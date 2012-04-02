namespace SGO
{
    public class EntityFactory
    {
        private readonly EntityNetworkManager m_entityNetworkManager;
        private readonly EntityTemplateDatabase m_entityTemplateDatabase;

        /// <summary>
        /// Constructor
        /// </summary>
        public EntityFactory(EntityTemplateDatabase entityTemplateDatabase, EntityNetworkManager entityNetworkManager)
        {
            m_entityTemplateDatabase = entityTemplateDatabase;
            m_entityNetworkManager = entityNetworkManager;
        }

        /// <summary>
        /// Creates an entity from a template pulled from the entitydb
        /// </summary>
        /// <param name="entityTemplateName">name of the template</param>
        /// <returns>Created Entity</returns>
        public Entity CreateEntity(string entityTemplateName)
        {
            EntityTemplate template = m_entityTemplateDatabase.GetTemplate(entityTemplateName);
            //TODO: Throw exception here
            if (template == null)
                return null;
            Entity entity = template.CreateEntity(m_entityNetworkManager);
            entity.Initialize();
            return entity;
        }
    }
}