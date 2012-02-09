using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ClientInterfaces.GOC;
using SS13_Shared;

namespace CGO
{
    /// <summary>
    /// This class holds a template for an entity -- the entity name, components, and parameters the entity will be instantiated with.
    /// </summary>
    public class EntityTemplate : IEntityTemplate
    {
        /// <summary>
        /// This holds a list of the component types the entity will be instantiated with.
        /// </summary>
        private readonly List<string> _components = new List<string>();

        /// <summary>
        /// This holds a dictionary linking parameter objects to components
        /// </summary>
        private readonly Dictionary<string, List<ComponentParameter>> _parameters = new Dictionary<string,List<ComponentParameter>>();

        /// <summary>
        /// The Placement mode used for client-initiated placement. This is used for admin and editor placement. The serverside version controls what type the server assigns in normal gameplay.
        /// </summary>
        public PlacementOption PlacementMode { get; private set; }

        /// <summary>
        /// Offset that is added to the position when placing. (if any). Client only.
        /// </summary>
        public KeyValuePair<int, int> PlacementOffset { get; private set; }

        /// <summary>
        /// The different mounting points on walls. (If any).
        /// </summary>
        public List<int> MountingPoints { get; private set; }

        /// <summary>
        /// Description for the entity. Used by default examine handlers.
        /// </summary>
        public string Description { get ; private set; }

        /// <summary>
        /// Name of the entity template eg. "HumanMob"
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public EntityTemplate()
        {
            Description = "There is nothing special about this object.";
        }

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
        public Entity CreateEntity()
        {
            var e = new Entity();

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

        /// <summary>
        /// Adds a component type to the entity template
        /// </summary>
        public void AddComponent(string componentType)
        {
            _components.Add(componentType);
        }

        /// <summary>
        /// Sets a parameter for a component type for this template
        /// </summary>
        /// <param name="componentType">The type of the component to set a parameter on</param>
        /// <param name="parameter">The parameter object</param>
        public void SetParameter(string componentType, ComponentParameter parameter)
        {
            if(_parameters.ContainsKey(componentType))
            _parameters[componentType].Add(parameter);
        }

        public void LoadFromXml(XElement templateElement)
        {
            Name = templateElement.Attribute("name").Value;

            var tComponents = templateElement.Element("Components").Elements();
            //Parse components
            foreach (var tComponent in tComponents)
            {
                string componentname = tComponent.Attribute("name").Value;
                _components.Add(componentname);
                _parameters.Add(componentname, new List<ComponentParameter>());
                var tComponentParameters = from tParam in tComponent.Descendants("Parameter")
                                            select tParam;
                //Parse component parameters
                foreach (var tComponentParameter in tComponentParameters)
                {
                    Type paramtype = TranslateType(tComponentParameter.Attribute("type").Value);

                    if (paramtype == null)
                        break; //TODO THROW ERROR

                    _parameters[componentname].Add(new ComponentParameter(tComponentParameter.Attribute("name").Value, 
                                                                         paramtype, 
                                                                         tComponentParameter.Attribute("value").Value)
                                                 );
                }

                if (tComponent.Element("ExtendedParameters") != null)
                {
                    _parameters[componentname].Add(new ComponentParameter("ExtendedParameters", typeof(XElement), tComponent.Element("ExtendedParameters")));
                }
            }

            var t_placementprops = templateElement.Element("PlacementProperties");
            //Load Placement properties.
            if (t_placementprops != null)
            {
                XElement modeElement = t_placementprops.Element("PlacementMode");
                XElement offsetElement = t_placementprops.Element("PlacementOffset");
                XElement mNodesElement = t_placementprops.Element("PlacementNodes");

                if (modeElement != null)
                {
                    string modeName = modeElement.Attribute("type").Value;
                    this.PlacementMode = (PlacementOption)Enum.Parse(typeof(PlacementOption), modeName);
                }
                else
                    this.PlacementMode = PlacementOption.AlignNoneFree;

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

            var tDescription = templateElement.Element("Description");
            if (tDescription != null) Description = tDescription.Attribute("string").Value;
        }

        private static Type TranslateType(string typeName)
        {
            switch(typeName.ToLowerInvariant())
            {
                case "string":
                    return typeof(string);
                case "int":
                    return typeof(int);
                case "float":
                    return typeof(float);
                default:
                    return null;
            }
        }
    }
}
