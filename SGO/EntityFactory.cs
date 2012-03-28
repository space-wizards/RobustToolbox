using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO
{
    public class EntityFactory
    {
        private EntityTemplateDatabase m_entityTemplateDatabase;
        private EntityNetworkManager m_entityNetworkManager;

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
            var entity = template.CreateEntity(m_entityNetworkManager);
            entity.Initialize();
            return entity;
        }
    }
}
