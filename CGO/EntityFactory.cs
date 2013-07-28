namespace CGO
{
    public class EntityFactory
    {
        private EntityTemplateDatabase m_entityTemplateDatabase;

        /// <summary>
        /// Constructor
        /// </summary>
        public EntityFactory(EntityTemplateDatabase entityTemplateDatabase)
        {
            m_entityTemplateDatabase = entityTemplateDatabase;
        }

        /// <summary>
        /// Creates an entity from a template pulled from the entitydb
        /// </summary>
        /// <param name="entityTemplateName">name of the template</param>
        /// <returns>Created Entity</returns>
        public Entity CreateEntity(string entityTemplateName)
        {
            var template = m_entityTemplateDatabase.GetTemplate(entityTemplateName);
            //TODO: Throw exception here
            return template == null ? null : template.CreateEntity();
        }

        /// <summary>
        /// Retrieves template with given name from db
        /// </summary>
        /// <param name="entityTemplateName">name of the template</param>
        /// <returns>Template</returns>
        public EntityTemplate GetTemplate(string entityTemplateName)
        {
            return m_entityTemplateDatabase.GetTemplate(entityTemplateName);
        }
    }
}
