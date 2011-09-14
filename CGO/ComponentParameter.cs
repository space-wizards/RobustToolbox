using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    public class ComponentParameter
    {
        private string m_memberName;
        public string MemberName
        {
            get
            { return m_memberName; }
            set { }
        }

        private string m_parameterType;
        public string ParameterType
        {
            get
            { return m_parameterType; }
            set { }
        }

        private object m_parameter;
        public object Parameter
        {
            get
            { return m_parameter; }
            set { }
        }

        public ComponentParameter(string memberName, string parameterType, object parameter)
        {
            m_memberName = memberName;
            m_parameterType = parameterType;
            m_parameter = parameter;
        }
    }
}
