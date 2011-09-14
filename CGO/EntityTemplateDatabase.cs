using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    public class EntityTemplateDatabase
    {
        private Dictionary<string, EntityTemplate> m_templates;

        public EntityTemplateDatabase()
        {
            m_templates = new Dictionary<string, EntityTemplate>();
        }

        /// <summary>
        /// Load entity templates from an xml file
        /// </summary>
        /// <param name="path">path to xml file</param>
        public void LoadTemplatesFromXML(string path)
        {

        }

        /// <summary>
        /// Add a template directly to the database -- used for creating a template in code instead of xml
        /// </summary>
        /// <param name="template">the template to add</param>
        public void AddTemplate(EntityTemplate template)
        {

        }

        /// <summary>
        /// Gets a tempate from the db and returns it
        /// </summary>
        /// <param name="templatename"></param>
        /// <returns></returns>
        public EntityTemplate GetTemplate(string templatename)
        {
            return null;
        }
    }
}
