using System;

namespace Robust.Shared.Analyzers;

/// <summary>
///     Indicates that overriders of this method must always call the base function.
/// </summary>
/// <param name="onlyOverrides">
///     If true, only base calls to <b>overrides</b> are necessary.
///     This is intended for base classes where the base function is always empty,
///     so a base call from the first override may be ommitted.
/// </param>
/// <example>
/// <code>
///     public abstract class MyBaseClass
///     {
///         [MustCallBase]
///         public virtual void Initialize() { /* ... */ }
///     }
///     <br/>
///     public sealed class MyBadClass : MyBaseClass
///     {
///         // Error RA0028: Overriders of this function must always call the base function.
///         public override void Initialize()
///         {
///             // We don't do anything in here, not even call base.Initialize().
///         }
///     }
///     <br/>
///     public sealed class MyGoodClass : MyBaseClass
///     {
///         // No error.
///         public override void Initialize()
///         {
///             base.Initialize();
///         }
///     }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MustCallBaseAttribute(bool onlyOverrides = false) : Attribute
{
    public bool OnlyOverrides { get; } = onlyOverrides;
}
