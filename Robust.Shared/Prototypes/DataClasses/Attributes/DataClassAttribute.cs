using System;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Shared.Prototypes.DataClasses.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [MeansImplicitDataClass]
    public class DataClassAttribute : Attribute
    {
        public readonly Type? ClassName;

        public DataClassAttribute(Type? className = null)
        {
            ClassName = className;
        }
    }
}
