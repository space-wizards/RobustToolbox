using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [BaseTypeRequired(typeof(Attribute))]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class MeansDataDefinitionAttribute : Attribute
    {
    }
}
