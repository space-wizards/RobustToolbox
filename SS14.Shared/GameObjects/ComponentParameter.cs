using System;

namespace SS14.Shared.GameObjects
{
    /// <summary>
    /// A parameter used for instantiation of a component in an entity from a template.
    /// </summary>
    [Serializable]
    public class ComponentParameter
    {
        public ComponentParameter()
        {
        }

        public ComponentParameter(string memberName, object parameterValue)
        {
            MemberName = memberName;
            Parameter = parameterValue;
        }

        public string MemberName { get; protected set; }

        public Type ParameterType
        {
            get { return Parameter.GetType(); }
        }

        public dynamic Parameter { get; set; }

        public T GetValue<T>()
        {
            //MAGIC WOOT
            if (!(Parameter is T))
                throw new ArgumentException("Parameter type specified does not match stored parameter's type.");

            return (T) Parameter;
        }

        public object GetValue()
        {
            return Parameter;
        }
    }
}
