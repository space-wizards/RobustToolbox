using System;

namespace Robust.Shared.Prototypes.DataClasses.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class DataClassAttribute : Attribute
    {
        public readonly Type? ClassName;

        public DataClassAttribute(Type? className = null)
        {
            ClassName = className;
        }
    }
}
