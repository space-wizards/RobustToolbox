using System;

namespace Robust.Shared.Analyzers
{
    /// <summary>
    ///     Indicates that inheriting a given type doesn't work unless you specify <see cref="SerializableAttribute"/>
    ///     on the child.
    /// </summary>
    /// <example>
    /// <code>
    ///     [RequiresSerializable]
    ///     public abstract MyParent;
    ///     <br/>
    ///     // Warning RA0001: Class not marked as (Net)Serializable.
    ///     public sealed class MyChild1 : MyParent;
    ///     <br/>
    ///     // No warning.
    ///     [NetSerializable, Serializable]
    ///     public sealed class MyChild2 : MyParent;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RequiresSerializableAttribute : Attribute;
}
