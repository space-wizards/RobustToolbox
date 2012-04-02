using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.Linq;

namespace SGO
{
    public class EntityTemplateDatabase
    {
        private Dictionary<string, EntityTemplate> m_templates;

        public EntityTemplateDatabase()
        {
            m_templates = new Dictionary<string, EntityTemplate>();
            LoadAllTemplates();
        }

        private void LoadAllTemplates()
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
                EntityTemplate newTemplate = new EntityTemplate();
                newTemplate.LoadFromXML(e);
                AddTemplate(newTemplate);
            }
        }

        /// <summary>
        /// Add a template directly to the database -- used for creating a template in code instead of xml
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
