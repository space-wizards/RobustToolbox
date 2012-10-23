using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces.GameObject;

namespace SGO
{
    /// <summary>
    /// This class holds a template for an entity -- the entity name, components, and parameters the entity will be instantiated with.
    /// </summary>
    public class EntityTemplate : IEntityTemplate
    {
        /// <summary>
        /// This holds a list of the component types the entity will be instantiated with.
        /// </summary>
        private readonly List<string> components = new List<string>();

        /// <summary>
        /// This holds a dictionary linking parameter objects to 
        /// </summary>
        private readonly Dictionary<string, List<ComponentParameter>> parameters =
            new Dictionary<string, List<ComponentParameter>>();

        #region IEntityTemplate Members

        /// <summary>
        /// The Placement mode used for server-initiated placement. This is used for placement during normal gameplay. The clientside version controls the placement type for editor and admin spawning.
        /// </summary>
        public PlacementOption PlacementMode { get; private set; }

        /// <summary>
        /// The Range this entity can be placed from.
        /// </summary>
        public int PlacementRange { get; private set; }

        public string Name { get; set; }

        /// <summary>
        /// Creates an entity from this template
        /// </summary>
        /// <returns></returns>
        public IEntity CreateEntity(IEntityNetworkManager entityNetworkManager)
        {
            var e = new Entity(entityNetworkManager);

            foreach (string componentname in components)
            {
                IGameObjectComponent component = ComponentFactory.Singleton.GetComponent(componentname);
                if (component == null)
                    continue; //TODO THROW ERROR

                ///Get all the params in the template that apply to this component
                List<ComponentParameter> cparameters = parameters[componentname];
                foreach (ComponentParameter p in cparameters)
                {
                    ///Set the component's parameters
                    component.SetParameter(p);
                }
                ///Add the component to the entity
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
            components.Add(componentType);
        }

        /// <summary>
        /// Sets a parameter for a component type for this template
        /// </summary>
        /// <param name="t">The type of the component to set a parameter on</param>
        /// <param name="parameter">The parameter object</param>
        public void SetParameter(string componenttype, ComponentParameter parameter)
        {
            if (parameters.ContainsKey(componenttype))
                parameters[componenttype].Add(parameter);
        }

        public void LoadFromXML(XElement templateElement)
        {
            Name = templateElement.Attribute("name").Value;

            IEnumerable<XElement> tComponents = templateElement.Element("Components").Elements();
            //Parse components
            foreach (XElement tComponent in tComponents)
            {
                string componentname = tComponent.Attribute("name").Value;
                components.Add(componentname);
                parameters.Add(componentname, new List<ComponentParameter>());
                IEnumerable<XElement> tComponentParameters = from t_param in tComponent.Descendants("Parameter")
                                                              select t_param;
                //Parse component parameters
                foreach (XElement tComponentParameter in tComponentParameters)
                {
                    Type paramType = translateType(tComponentParameter.Attribute("type").Value);

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
                        if (!float.TryParse(paramRawValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pval))
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
                    parameters[componentname].Add(cparam);
                }

                if (tComponent.Element("ExtendedParameters") != null)
                {
                    parameters[componentname].Add(new ComponentParameter("ExtendedParameters",
                                                                         tComponent.Element("ExtendedParameters")));
                }
            }

            XElement t_placementprops = templateElement.Element("PlacementProperties");
            //Load Placement properties.
            if (t_placementprops != null)
            {
                XElement modeElement = t_placementprops.Element("PlacementMode");
                XElement rangeElement = t_placementprops.Element("PlacementRange");

                if (modeElement != null)
                {
                    string modeName = modeElement.Attribute("type").Value;
                    PlacementMode = (PlacementOption) Enum.Parse(typeof (PlacementOption), modeName);
                }
                else
                    PlacementMode = PlacementOption.AlignNone;

                if (rangeElement != null)
                {
                    int range = int.Parse(rangeElement.Attribute("value").Value);
                    PlacementRange = range;
                }
                else
                    PlacementRange = 200;
            }
        }

        #endregion

        public static Type translateType(string typeName)
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
                default:
                    return null;
            }
        }
    }

    public class TemplateLoadException: Exception
    {
        public TemplateLoadException(string message) :base(message)
        {}
    }
}
