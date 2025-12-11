using System;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    /// <summary>
    ///     When inheriting a field from a parent, always <b>merge</b> the two fields when such behavior exists.
    ///     This is unlike the normal behavior where the child's field will always <b>overwrite</b> the parent's.
    ///     <br/>
    ///     Merging is done at a YAML level by combining mapping and
    /// </summary>
    /// <example>
    ///     <code>
    ///     - id: Parent
    ///       myField: [Foo, Bar]
    ///     <br/>
    ///     - id: Child
    ///       parents: [Parent]
    ///       myField: [Baz, Qux]
    ///     </code>
    ///     Which, when deserialized, will result in data that looks like this:
    ///     <code>
    ///     - id: Child
    ///       myField: [Foo, Bar, Baz, Qux]
    ///     </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AlwaysPushInheritanceAttribute : Attribute
    {
    }
}
