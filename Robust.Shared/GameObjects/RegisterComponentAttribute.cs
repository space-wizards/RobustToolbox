using System;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

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
        /// <summary>
        ///     If true, this component will inherit component references from its parent recursively.
        /// </summary>
        public bool Recursive { get; set; } = true;
    }
}
