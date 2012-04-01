using System;

namespace SS13_Shared.GO
{
    /// <summary>
    /// A parameter used for instantiation of a component in an entity from a template.
    /// </summary>
    public class ComponentParameter
    {
        public string MemberName { get; private set; }

        public Type ParameterType { get; private set; }

        public object Parameter { get; private set; }

        public ComponentParameter(string memberName, Type parameterType, object parameter)
        {
            MemberName = memberName;
            ParameterType = parameterType;
            Parameter = parameter;
        }
    }
}
