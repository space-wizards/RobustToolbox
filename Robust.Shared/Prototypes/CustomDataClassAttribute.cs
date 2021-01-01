using System;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Robust.Shared.Prototypes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [BaseTypeRequired(typeof(IComponent))]
    public class CustomDataClassAttribute : Attribute
    {
        public readonly Type ClassName;

        public CustomDataClassAttribute(Type className)
        {
            ClassName = className;
        }
    }
}
