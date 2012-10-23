using System;

namespace SS13_Shared.GO
{
    /// <summary>
    /// A parameter used for instantiation of a component in an entity from a template.
    /// </summary>
    [Serializable]
    public class ComponentParameter
    {
        public string MemberName { get; protected set; }

        public Type ParameterType { get { return Parameter.GetType(); } }

        public dynamic Parameter { get; set; }

        public ComponentParameter() {}
        
        public ComponentParameter(string memberName, object parameterValue)
        {
            MemberName = memberName;
            Parameter = parameterValue;
        }

        public T GetValue<T>()
        {
            //MAGIC WOOT
            if(!(Parameter is T))
                throw new ArgumentException("Parameter type specified does not match stored parameter's type.");

            return (T) Parameter;
        }

        public object GetValue()
        {
            return Parameter;
        }
    }
}
