using System;
using System.ComponentModel;

namespace Robust.Shared.IoC;

/// <summary>
/// Declares that a type has dependencies that can be injected via <see cref="IDependencyCollection"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type should only be used by engine.
/// The API may change in the future so content should not implement or use it itself.
/// Automatic implementation via the source generator is the only supported method.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[RequiresExplicitImplementation]
[NotContentImplementable]
public interface IHasDependencies
{
    /// <summary>
    /// Get an array of types that this object wants to have resolved and injected.
    /// </summary>
    Type[] GetDependencyTypes();

    /// <summary>
    /// Inject services into this type.
    /// </summary>
    /// <param name="instances">
    /// The list of services to inject.
    /// This is the same length and order as the types returned by <see cref="GetDependencyTypes"/>.
    /// </param>
    void Inject(ReadOnlySpan<object> instances);
}

/// <summary>
/// Has the dependencies source generator ran on this type?
/// </summary>
/// <remarks>
/// <para>
/// If this attribute is lacking on a type that has <c>[Dependency]</c> fields,
/// it indicates that the type in question needs its code fixed to support the source generator.
/// </para>
/// <para>
/// This type is a TEMPORARY implementation detail for backwards compatibility.
/// It will be removed in the future along with support for non-source-generated dependencies.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class HasDependenciesGeneratedAttribute : Attribute;
