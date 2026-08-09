using System;

namespace Robust.Shared.Analyzers;

/// <summary>
///     Indicates that a given interface must always be implemented using explicit interface implementation syntax.
///     This allows the programmer to indicate that the interface's implementation is not itself ever public API,
///     and is useful for things like initialization methods.
/// </summary>
/// <example>
/// <code>
///     [RequiresExplicitImplementation]
///     public interface MyInterface
///     {
///         public void DoThing();
///     }
///     <br/>
///     public sealed class MyClass : MyInterface
///     {
///         // Warning RA0000: No explicit interface specified.
///         public void DoThing() { /* ... */ }
///     }
///     <br/>
///     public sealed class MyBetterClass : MyInterface
///     {
///         // No warning.
///         void MyInterface.DoThing() { /* ... */ }
///     }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class RequiresExplicitImplementationAttribute : Attribute;
