using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    /// <summary>
    ///     Marks an attribute class as implying <see cref="DataDefinitionAttribute"/>.
    /// </summary>
    /// <seealso cref="DataFieldAttribute"/>
    /// <seealso cref="DataRecordAttribute"/>
    [BaseTypeRequired(typeof(Attribute))]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class MeansDataDefinitionAttribute : Attribute
    {
    }
}
