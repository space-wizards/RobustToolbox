using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using GameObject;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces.GameObject;
using IEntity = ServerInterfaces.GameObject.IEntity;

namespace SGO
{
    /// <summary>
    /// This class holds a template for an entity -- the entity name, components, and parameters the entity will be instantiated with.
    /// </summary>
    public class EntityTemplate : GameObject.EntityTemplate, IEntityTemplate
    {

        public EntityTemplate(GameObject.EntityManager entityManager)
            : base(entityManager) { }

        #region IEntityTemplate Members
        
        /// <summary>
        /// The Range this entity can be placed from. This is only used serverside since the server handles normal gameplay. The client uses unlimited range since it handles things like admin spawning and editing.
        /// </summary>
        public int PlacementRange { get; private set; }
        
        /// <summary>
        /// Creates an entity from this template
        /// </summary>
        /// <returns></returns>
        public IEntity CreateEntity()
        {
            var e = new Entity((EntityManager)EntityManager);

            foreach (var componentname in _components)
            {
                var component = EntityManager.ComponentFactory.GetComponent(componentname);
                if (component == null)
                    continue; //TODO THROW ERROR

                // Get all the params in the template that apply to this component
                var cparameters = _parameters[componentname];
                foreach (var p in cparameters)
                {
                    // Set the component's parameters
                    component.SetParameter(p);
                }
                // Add the component to the entity
                e.AddComponent(component.Family, component);
            }

            e.Name = Name;
            e.Template = this;
            return e;
        }
        
        protected override void LoadPlacementProperties(XElement placementPropertiesElement)
        {
            XElement modeElement = placementPropertiesElement.Element("PlacementMode");
            XElement rangeElement = placementPropertiesElement.Element("PlacementRange");

            if (modeElement != null)
            {
                string modeName = modeElement.Attribute("type").Value;
                PlacementMode = modeName;
            }
            else
                PlacementMode = "AlignNone";

            if (rangeElement != null)
            {
                int range = int.Parse(rangeElement.Attribute("value").Value);
                PlacementRange = range;
            }
            else
                PlacementRange = 200;
        }

        #endregion

    }
}
