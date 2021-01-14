using System;
using JetBrains.Annotations;

namespace Robust.Shared.Interfaces.Serialization
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [BaseTypeRequired(typeof(DeepCloneExtension))]
    public class DeepCloneExtensionAttribute : Attribute
    {
        public readonly Type ForType;

        public DeepCloneExtensionAttribute(Type forType)
        {
            ForType = forType;
        }
    }

    public abstract class DeepCloneExtension
    {
        public abstract object DeepClone(object value);
    }
}
