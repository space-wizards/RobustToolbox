using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ClientInterfaces.GOC;
using GameObject;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    /// <summary>
    /// This class holds a template for an entity -- the entity name, components, and parameters the entity will be instantiated with.
    /// </summary>
    public class EntityTemplate : GameObject.EntityTemplate, IEntityTemplate
    {
        /// <summary>
        /// Offset that is added to the position when placing. (if any). Client only.
        /// </summary>
        public KeyValuePair<int, int> PlacementOffset { get; private set; }

        /// <summary>
        /// The different mounting points on walls. (If any).
        /// </summary>
        public List<int> MountingPoints { get; private set; }
        
        /// <summary>
        /// Attempts to retrieve and return the name of the basesprite of this Template.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ComponentParameter> GetBaseSpriteParamaters() 
        {
            var spriteLists = from para in _parameters.Values
                              let spriteArgs = para.Where(arg => arg.MemberName == "basename" || arg.MemberName == "addsprite")
                              select spriteArgs;
            return spriteLists.SelectMany(x => x);
        }

        /// <summary>
        /// Creates an entity from this template
        /// </summary>
        /// <returns></returns>
        public Entity CreateEntity(EntityNetworkManager entityNetworkManager)
        {
            var e = new Entity(entityNetworkManager);

            foreach (var componentname in _components)
            {
                var component = ComponentFactory.Singleton.GetComponent(componentname);
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
            XElement offsetElement = placementPropertiesElement.Element("PlacementOffset");
            XElement mNodesElement = placementPropertiesElement.Element("PlacementNodes");

            if (modeElement != null)
            {
                string modeName = modeElement.Attribute("type").Value;
                PlacementMode = modeName;
            }
            else
                PlacementMode = "AlignNone";

            if (offsetElement != null)
            {
                int xOffset = int.Parse(offsetElement.Attribute("offsetX").Value);
                int yOffset = int.Parse(offsetElement.Attribute("offsetY").Value);
                this.PlacementOffset = new KeyValuePair<int, int>(xOffset, yOffset);
            }

            if (mNodesElement != null)
            {
                MountingPoints = new List<int>();
                foreach (var eNode in mNodesElement.Elements("PlacementNode"))
                {
                    var nodeHeight = int.Parse(eNode.Attribute("nodeHeight").Value);
                    MountingPoints.Add(nodeHeight);
                }
            }
        }
    }
}
