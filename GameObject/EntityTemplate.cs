using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Xml.Linq;
using SS13_Shared.GO;

namespace GameObject
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
        protected readonly Dictionary<string, List<ComponentParameter>> _parameters = new Dictionary<string, List<ComponentParameter>>();
        
        /// <summary>
        /// The Placement mode used for client-initiated placement. This is used for admin and editor placement. The serverside version controls what type the server assigns in normal gameplay.
        /// </summary>
        public string PlacementMode { get; protected set; }

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
        /// Default constructor
        /// </summary>
        public EntityTemplate(EntityManager entityManager)
        {
            EntityManager = entityManager;
            Description = "There is nothing special about this object.";
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
            if (_parameters.ContainsKey(componentType))
                _parameters[componentType].Add(parameter);
        }

        public static Type TranslateType(string typeName)
        {
            switch (typeName.ToLowerInvariant())
            {
                case "string":
                    return typeof(string);
                case "int":
                    return typeof(int);
                case "float":
                    return typeof(float);
                case "boolean":
                case "bool":
                    return typeof(bool);
                default:
                    return null;
            }
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
                    if (tComponentParameter.Attribute("type").Value == "" || tComponentParameter.Attribute("name").Value == "")
                        throw new ArgumentException("Component Parameter name or type not set.");

                    //Get the specified type
                    Type paramType = TranslateType(tComponentParameter.Attribute("type").Value);

                    //Get the raw value
                    string paramRawValue = tComponentParameter.Attribute("value").Value;

                    //Validate
                    var paramName = tComponentParameter.Attribute("name").Value;
                    if (paramType == null)
                        throw new TemplateLoadException("Invalid parameter type specified.");
                    if (paramName == "")
                        throw new TemplateLoadException("Invalid parameter name specified.");

                    //Convert the raw value to the proper type
                    object paramValue;// = Convert.ChangeType(tComponentParameter.Attribute("value").Value, paramType);
                    if (paramType == typeof(int))
                    {
                        int pval;
                        if (!int.TryParse(paramRawValue, out pval))
                            throw new ArgumentException("Could not parse parameter " + paramName + " as int. Value: " + paramRawValue);
                        paramValue = pval;
                    }
                    else if (paramType == typeof(float))
                    {
                        float pval;
                        if (!float.TryParse(paramRawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out pval))
                            throw new ArgumentException("Could not parse parameter " + paramName + " as float. Value: " + paramRawValue);
                        paramValue = pval;
                    }
                    else if (paramType == typeof(bool))
                    {
                        bool pval;
                        if (!bool.TryParse(paramRawValue, out pval))
                            throw new ArgumentException("Could not parse parameter " + paramName + " as bool. Value: " + paramRawValue);
                        paramValue = pval;
                    }
                    else if (paramType == typeof(string))
                    {
                        paramValue = paramRawValue;
                    }
                    else
                    {
                        throw new ArgumentException("Could not parse parameter " + paramName + ". Type not recognized. Value: " + paramRawValue);
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

            var t_placementprops = templateElement.Element("PlacementProperties");
            //Load Placement properties.
            if (t_placementprops != null)
            {
                LoadPlacementProperties(t_placementprops);
            }
            else
            {
                PlacementMode = "AlignNone";
            }

            var tDescription = templateElement.Element("Description");
            if (tDescription != null) Description = tDescription.Attribute("string").Value;
        }

        protected virtual void LoadPlacementProperties(XElement placementPropertiesElement)
        {}
    }

    public class TemplateLoadException : Exception
    {
        public TemplateLoadException(string message)
            : base(message)
        { }
    }
}
