using System;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    /// <summary>
    ///     When added to a <see cref="DataFieldAttribute">DataField</see>, this makes it so that the value of the field
    ///     is <b>never</b> given to a child when inheriting. For example with prototypes, this means the value of the
    ///     field will not be given to a child if it inherits from a parent with the field set. This is useful for
    ///     things like <see cref="AbstractDataFieldAttribute"/> where you do not want abstract-ness to pass on to the
    ///     child.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class NeverPushInheritanceAttribute : Attribute
    {
    }
}
