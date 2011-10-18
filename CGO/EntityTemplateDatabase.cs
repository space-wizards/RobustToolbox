using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.Linq;

namespace CGO
{
    public class EntityTemplateDatabase
    {
        private Dictionary<string, EntityTemplate> m_templates;

        public EntityTemplateDatabase()
        {
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
            XDocument tmp = XDocument.Load(path);
            var templates = tmp.Elements("EntityTemplate");
            foreach (XElement e in templates)
            {
                EntityTemplate newTemplate = new EntityTemplate();
                newTemplate.LoadFromXML(e);
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

        public EntityTemplate GetTemplateByAtomName(string templatename)
        {
            var temps = from t in m_templates.Values
                        where t.AtomName == templatename
                        select t;
            if (temps.Count() > 0)
                return temps.First();
            return null;
        }
    }
}
