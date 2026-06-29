using System;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    /// <summary>
    ///     Marks all classes or interfaces that inherit from the one with this attribute with
    ///     <see cref="DataDefinitionAttribute"/>, without requiring this be done manually.
    ///     Cannot be reversed by inheritors!
    /// </summary>
    /// <example>
    ///     <code>
    ///         [ImplicitDataDefinitionForInheritors]
    ///         public abstract class BaseClass
    ///         {
    ///             [DataField]
    ///             public bool Enabled;
    ///         }
    ///         <br/>
    ///         // Not only do we not need to mark this as a data definition,
    ///         // we inherit our fields from our parent class as normal and can add our own fields.
    ///         public sealed class MyClass : BaseClass
    ///         {
    ///             [DataField]
    ///             public int Counter;
    ///         }
    /// </code>
    /// </example>
    /// <seealso cref="ImplicitDataRecordAttribute"/>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class ImplicitDataDefinitionForInheritorsAttribute : Attribute
    {
    }
}
