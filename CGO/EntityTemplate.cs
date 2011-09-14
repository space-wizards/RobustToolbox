using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    /// <summary>
    /// This class holds a template for an entity -- the entity name, components, and parameters the entity will be instantiated with.
    /// </summary>
    public class EntityTemplate
    {
        /// <summary>
        /// This holds a list of the component types the entity will be instantiated with.
        /// </summary>
        private List<string> components;
        /// <summary>
        /// This holds a dictionary linking parameter objects to 
        /// </summary>
        private Dictionary<string, ComponentParameter> parameters;

        /// <summary>
        /// Name of the entity template eg. "HumanMob"
        /// </summary>
        private string m_name;
        public string Name
        {
            get
            { return m_name; }
            set
            { m_name = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public EntityTemplate()
        {

        }

        /// <summary>
        /// Creates an entity from this template
        /// </summary>
        /// <returns></returns>
        public Entity CreateEntity()
        {
            return null;
        }

        /// <summary>
        /// Adds a component type to the entity template
        /// </summary>
        public void AddComponent(string componentType)
        {
            
        }

        /// <summary>
        /// Sets a parameter for a component type
        /// </summary>
        /// <param name="t">The type of the component to set a parameter on</param>
        /// <param name="parameter">The parameter object</param>
        public void SetParameter(string componenttype, ComponentParameter parameter)
        {

        }
    }
}
