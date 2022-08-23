using System;

namespace Robust.Shared.IoC
{
    /// <summary>
    /// Specifies that the field this is applied to is a dependency,
    /// which will be resolved by <see cref="IoCManager" /> when the containing class is instantiated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The dependency is resolved as if <see cref="IoCManager.Resolve{T}" /> were to be called,
    /// but it avoids circular references and init order issues due to internal code in the <see cref="IoCManager" />.
    /// </para>
    /// <para>
    /// The dependency can be injected into read only fields without issues,
    /// and as a matter of fact it is recommended to use read only fields.
    /// </para>
    /// <para>
    /// If you would like to run code after the dependencies have been injected, use <see cref="IPostInjectInit" />
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DependencyAttribute : Attribute
    {
    }
}
