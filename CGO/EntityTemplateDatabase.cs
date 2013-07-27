using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using SS13_Shared;

namespace CGO
{
    public class EntityTemplateDatabase
    {
        private Dictionary<string, EntityTemplate> m_templates;

        public Dictionary<string, EntityTemplate> Templates { get { return m_templates; } }

        public EntityManager EntityManager { get; private set; }
        public EntityTemplateDatabase(EntityManager entityManager)
        {
            EntityManager = entityManager;
            m_templates = new Dictionary<string, EntityTemplate>();
            LoadAllTemplates();
        }

        public void LoadAllTemplates()
        {
            string[] templatePaths = Directory.GetFiles(@"EntityTemplates");
            foreach (string path in templatePaths)
                LoadTemplateFromXML(path);
        }

        /// <summary>
        /// Load entity templates from an xml file
        /// </summary>
        /// <param name="path">path to xml file</param>
        public void LoadTemplateFromXML(string path)
        {
            XElement tmp = XDocument.Load(path).Element("EntityTemplates");
            var templates = tmp.Elements("EntityTemplate");
            foreach (XElement e in templates)
            {
                EntityTemplate newTemplate = new EntityTemplate(EntityManager);
                newTemplate.LoadFromXml(e);
                AddTemplate(newTemplate);
            }
        }

        /// <summary>
        /// Add a template directly to the database
        /// </summary>
        /// <param name="template">the template to add</param>
        public void AddTemplate(EntityTemplate template)
        {
            m_templates.Add(template.Name, template);
        }

        /// <summary>
        /// Gets a tempate from the db and returns it
        /// </summary>
        /// <param name="templatename"></param>
        /// <returns></returns>
        public EntityTemplate GetTemplate(string templatename)
        {
            if (m_templates.ContainsKey(templatename))
                return m_templates[templatename];
            return null;
        }
    }
}
