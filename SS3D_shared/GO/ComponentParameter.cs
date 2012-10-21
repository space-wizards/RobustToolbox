using System;
using System.Reflection;

namespace SS13_Shared.GO
{
    /// <summary>
    /// A parameter used for instantiation of a component in an entity from a template.
    /// </summary>
    public class ComponentParameter
    {
        public string MemberName { get; protected set; }

        public Type ParameterType { get; protected set; }

        public ComponentParameter() {}
        
        public ComponentParameter(string memberName, Type parameterType)
        {
            MemberName = memberName;
            ParameterType = parameterType;
        }

        public T GetValue<T>()
        {
            //MAGIC WOOT
            PropertyInfo parameterValue = GetType().GetProperty("Param");
            if(parameterValue == null) 
                throw new ArgumentException("Parameter has no type.");
            if(parameterValue.PropertyType != typeof(T))
                throw new ArgumentException("Parameter type specified does not match stored parameter's type.");
            var Parameter = parameterValue.GetValue(this, null);

            return (T) Parameter;
        }

        public static ComponentParameter<T> CreateParameter<T>(string memberName, Type parameterType, T parameter)
        {
            return new ComponentParameter<T>(memberName, parameterType, parameter);
        }
    }

    public class ComponentParameter<T> : ComponentParameter
    {
        public T Param { get; set; }

        public ComponentParameter() {}

        public ComponentParameter(string memberName, Type parameterType) 
            : base(memberName, parameterType) {}

        public ComponentParameter(string memberName, Type parameterType, T parameter)
            :base(memberName, parameterType)
        {
            Param = parameter;
        }
    }

    public class StringComponentParameter : ComponentParameter<string>
    {
        public string Parameter { get { return Param; } protected set { Param = value; } }

        public StringComponentParameter(string memberName, Type parameterType, string parameter)
            :base(memberName, parameterType)
        {
            Parameter = parameter;
        }
    }

    public class IntComponentParameter : ComponentParameter<int>
    {
        public int Parameter { get { return Param; } protected set { Param = value; } }

        public IntComponentParameter(string memberName, Type parameterType, int parameter)
            :base(memberName, parameterType)
        {
            Parameter = parameter;
        } 
    }

    public class FloatComponentParameter : ComponentParameter<float>
    {
        public float Parameter { get { return Param; } protected set { Param = value; } }

        public FloatComponentParameter(string memberName, Type parameterType, float parameter)
            : base(memberName, parameterType)
        {
            Parameter = parameter;
        }
    }

    public class BoolComponentParameter : ComponentParameter<bool>
    {
        public bool Parameter { get { return Param; } protected set { Param = value; } }
        
        public BoolComponentParameter(string memberName, Type parameterType, bool parameter)
            : base(memberName, parameterType)
        {
            Parameter = parameter;
        }
    }
}
