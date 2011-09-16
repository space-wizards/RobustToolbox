using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            EntityTemplate template = m_entityTemplateDatabase.GetTemplate(entityTemplateName);
            //TODO: Throw exception here
            if (template == null)
                return null;
            return template.CreateEntity();
        }
    }
}
