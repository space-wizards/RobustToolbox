using System;
using JetBrains.Annotations;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Marks a component as being automatically registered by <see cref="IComponentFactory.DoAutoRegistrations" />
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [BaseTypeRequired(typeof(IComponent))]
    [MeansImplicitUse]
    public sealed class RegisterComponentAttribute : Attribute
    {

    }
}
