using System;

namespace Robust.Shared.Animations
{
    /// <summary>
    ///     Specifies that a property can be animated, or that a method can be called by animations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class AnimatableAttribute : Attribute
    {
    }
}
