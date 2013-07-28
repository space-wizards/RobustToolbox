using ServerInterfaces.GameObject;

namespace SGO
{
    public class EntityFactory
    {
        private readonly EntityTemplateDatabase m_entityTemplateDatabase;

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
        public IEntity CreateEntity(string entityTemplateName)
        {
            EntityTemplate template = m_entityTemplateDatabase.GetTemplate(entityTemplateName);
            //TODO: Throw exception here
            if (template == null)
                return null;
            IEntity entity = template.CreateEntity();
            return entity;
        }
    }
}