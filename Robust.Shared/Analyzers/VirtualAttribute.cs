using System;

namespace Robust.Shared.Analyzers;

/// <summary>
///     Specify that this class is allowed to be inherited.
/// </summary>
/// <remarks>
///     Robust uses analyzers to prevent accidental usage of non-sealed classes:
///     a class must be either marked [Virtual], abstract, or sealed.
/// </remarks>
/// <seealso cref="ObsoleteInheritanceAttribute"/>
/// <example>
/// <code>
///     // Warning RA0003: Class must be explicitly marked as [Virtual], abstract, static or sealed.
///     public class MyVirtualClass;
///     <br/>
///     // No warning.
///     [Virtual]
///     public class MyBetterVirtualClass;
///     <br/>
///     // Also no warnings:
///     public sealed class MyClass1;
///     public static class MyClass2;
///     public abstract class MyClass3;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class VirtualAttribute : Attribute
{
}
