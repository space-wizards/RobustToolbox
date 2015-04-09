using SS14.Shared.GO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using SS14.Shared.Maths;

namespace SS14.Shared.GameObjects
{
    public class EntityTemplate
    {
        /// <summary>
        /// This holds a list of the component types the entity will be instantiated with.
        /// </summary>
        protected readonly List<string> _components = new List<string>();

        /// <summary>
        /// This holds a dictionary linking parameter objects to components
        /// </summary>
        protected readonly Dictionary<string, List<ComponentParameter>> _parameters =
            new Dictionary<string, List<ComponentParameter>>();

        /// <summary>
        /// Default constructor
        /// </summary>
        public EntityTemplate(EntityManager entityManager)
        {
            EntityManager = entityManager;
            Description = "There is nothing special about this object.";
        }

        /// <summary>
        /// The Placement mode used for client-initiated placement. This is used for admin and editor placement. The serverside version controls what type the server assigns in normal gameplay.
        /// </summary>
        public string PlacementMode { get; protected set; }

        /// <summary>
        /// The Range this entity can be placed from. This is only used serverside since the server handles normal gameplay. The client uses unlimited range since it handles things like admin spawning and editing.
        /// </summary>
        public int PlacementRange { get; protected set; }

        /// <summary>
        /// Offset that is added to the position when placing. (if any). Client only.
        /// </summary>
        public KeyValuePair<int, int> PlacementOffset { get; protected set; }

        /// <summary>
        /// The different mounting points on walls. (If any).
        /// </summary>
        public List<int> MountingPoints { get; protected set; }

        /// <summary>
        /// Name of the entity template eg. "HumanMob"
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description for the entity. Used by default examine handlers.
        /// </summary>
        public string Description { get; protected set; }

        public EntityManager EntityManager { get; protected set; }

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
            if (_parameters.ContainsKey(componentType))
                _parameters[componentType].Add(parameter);
        }

        public static Type TranslateType(string typeName)
        {
            switch (typeName.ToLowerInvariant())
            {
                case "string":
                    return typeof (string);
                case "int":
                    return typeof (int);
                case "float":
                    return typeof (float);
                case "boolean":
                case "bool":
                    return typeof (bool);
                case "vector2":
                    return typeof(Vector2);
                case "vector3":
                    return typeof(Vector3);
                case "vector4":
                    return typeof(Vector4);
                default:
                    return null;
            }
        }

        public void LoadFromXml(XElement templateElement)
        {
            Name = templateElement.Attribute("name").Value;

            IEnumerable<XElement> tComponents = templateElement.Element("Components").Elements();
            //Parse components
            foreach (XElement tComponent in tComponents)
            {
                string componentname = tComponent.Attribute("name").Value;
                _components.Add(componentname);
                _parameters.Add(componentname, new List<ComponentParameter>());
                IEnumerable<XElement> tComponentParameters = from tParam in tComponent.Descendants("Parameter")
                                                             select tParam;
                //Parse component parameters
                foreach (XElement tComponentParameter in tComponentParameters)
                {
                    if (tComponentParameter.Attribute("type").Value == "" ||
                        tComponentParameter.Attribute("name").Value == "")
                        throw new ArgumentException("Component Parameter name or type not set.");

                    //Get the specified type
                    Type paramType = TranslateType(tComponentParameter.Attribute("type").Value);

                    //Get the raw value
                    string paramRawValue = tComponentParameter.Attribute("value").Value;

                    //Validate
                    string paramName = tComponentParameter.Attribute("name").Value;
                    if (paramType == null)
                        throw new TemplateLoadException("Invalid parameter type specified.");
                    if (paramName == "")
                        throw new TemplateLoadException("Invalid parameter name specified.");

                    //Convert the raw value to the proper type
                    object paramValue; // = Convert.ChangeType(tComponentParameter.Attribute("value").Value, paramType);
                    if (paramType == typeof (int))
                    {
                        int pval;
                        if (!int.TryParse(paramRawValue, out pval))
                            throw new ArgumentException("Could not parse parameter " + paramName + " as int. Value: " +
                                                        paramRawValue);
                        paramValue = pval;
                    }
                    else if (paramType == typeof (float))
                    {
                        float pval;
                        if (!float.TryParse(paramRawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out pval))
                            throw new ArgumentException("Could not parse parameter " + paramName + " as float. Value: " +
                                                        paramRawValue);
                        paramValue = pval;
                    }
                    else if (paramType == typeof (bool))
                    {
                        bool pval;
                        if (!bool.TryParse(paramRawValue, out pval))
                            throw new ArgumentException("Could not parse parameter " + paramName + " as bool. Value: " +
                                                        paramRawValue);
                        paramValue = pval;
                    }
                    else if (paramType == typeof (string))
                    {
                        paramValue = paramRawValue;
                    }
                    else if (paramType == typeof (Vector2))
                    {
                        var args = paramRawValue.Split(',');
                        if (args.Length != 2)
                            throw new ArgumentException("Could not parse parameter " + paramName +
                                                        " as Vector2. Value: " + paramRawValue);
                        paramValue = new Vector2(float.Parse(args[0]), float.Parse(args[1]));
                    }
                    else if (paramType == typeof(Vector3))
                    {
                        var args = paramRawValue.Split(',');
                        if (args.Length != 3)
                            throw new ArgumentException("Could not parse parameter " + paramName +
                                                        " as Vector3. Value: " + paramRawValue);
                        paramValue = new Vector3(float.Parse(args[0]), float.Parse(args[1]), float.Parse(args[2]));
                    }
                    else if (paramType == typeof(Vector4))
                    {
                        var args = paramRawValue.Split(',');
                        if (args.Length != 4)
                            throw new ArgumentException("Could not parse parameter " + paramName +
                                                        " as Vector4. Value: " + paramRawValue);
                        paramValue = new Vector4(float.Parse(args[0]), float.Parse(args[1]), float.Parse(args[2]), float.Parse(args[3]));
                    }
                    else
                    {
                        throw new ArgumentException("Could not parse parameter " + paramName +
                                                    ". Type not recognized. Value: " + paramRawValue);
                    }

                    var cparam = new ComponentParameter(paramName, paramValue);
                    _parameters[componentname].Add(cparam);
                }

                if (tComponent.Element("ExtendedParameters") != null)
                {
                    _parameters[componentname].Add(new ComponentParameter("ExtendedParameters",
                                                                          tComponent.Element("ExtendedParameters")));
                }
            }

            XElement t_placementprops = templateElement.Element("PlacementProperties");
            //Load Placement properties.
            if (t_placementprops != null)
            {
                LoadPlacementProperties(t_placementprops);
            }
            else
            {
                PlacementMode = "AlignNone";
            }

            XElement tDescription = templateElement.Element("Description");
            if (tDescription != null) Description = tDescription.Attribute("string").Value;
        }

        protected virtual void LoadPlacementProperties(XElement placementPropertiesElement)
        {
            XElement modeElement = placementPropertiesElement.Element("PlacementMode");
            XElement offsetElement = placementPropertiesElement.Element("PlacementOffset");
            XElement mNodesElement = placementPropertiesElement.Element("PlacementNodes");
            XElement rangeElement = placementPropertiesElement.Element("PlacementRange");

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
                PlacementOffset = new KeyValuePair<int, int>(xOffset, yOffset);
            }

            if (mNodesElement != null)
            {
                MountingPoints = new List<int>();
                foreach (XElement eNode in mNodesElement.Elements("PlacementNode"))
                {
                    int nodeHeight = int.Parse(eNode.Attribute("nodeHeight").Value);
                    MountingPoints.Add(nodeHeight);
                }
            }

            if (rangeElement != null)
            {
                int range = int.Parse(rangeElement.Attribute("value").Value);
                PlacementRange = range;
            }
            else
                PlacementRange = 200;
        }

        /// <summary>
        /// Attempts to retrieve and return the name of the basesprite of this Template.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ComponentParameter> GetBaseSpriteParamaters()
        {
            // TODO unfuck this, it is painfully stupid.
            if(_parameters.ContainsKey("IconComponent"))
            {
                return _parameters["IconComponent"].Where(arg => arg.MemberName == "icon");
            }
            return new List<ComponentParameter>();
        }


        /// <summary>
        /// Creates an entity from this template
        /// </summary>
        /// <returns></returns>
        public Entity CreateEntity()
        {
            var e = new Entity(EntityManager);

            foreach (string componentname in _components)
            {
                IComponent component = EntityManager.ComponentFactory.GetComponent(componentname);
                if (component == null)
                    continue; //TODO THROW ERROR

                // Get all the params in the template that apply to this component
                List<ComponentParameter> cparameters = _parameters[componentname];
                foreach (ComponentParameter p in cparameters)
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
    }

    public class TemplateLoadException : Exception
    {
        public TemplateLoadException(string message)
            : base(message)
        {
        }
    }
}