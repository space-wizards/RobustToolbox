#nullable enable
using System;

namespace Robust.Shared.GameObjects
{

    /// <summary>
    /// Arguments for an event related to a component.
    /// </summary>
    public abstract class ComponentEventArgs : EventArgs
    {

        /// <summary>
        /// Component that this event relates to.
        /// </summary>
        public IComponent Component { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="ComponentEventArgs"/>.
        /// </summary>
        /// <param name="component">The relevant component</param>
        protected ComponentEventArgs(IComponent component) => Component = component;

    }

    /// <summary>
    /// Arguments for an event related to a component being added.
    /// </summary>
    public sealed class AddedComponentEventArgs : ComponentEventArgs
    {
        /// <summary>
        /// Constructs a new instance of <see cref="AddedComponentEventArgs"/>.
        /// </summary>
        /// <param name="component">The relevant component</param>
        public AddedComponentEventArgs(IComponent component) : base(component) { }
    }

    /// <summary>
    /// Arguments for an event related to a component being removed.
    /// </summary>
    public sealed class RemovedComponentEventArgs : ComponentEventArgs
    {
        /// <summary>
        /// Constructs a new instance of <see cref="RemovedComponentEventArgs"/>.
        /// </summary>
        /// <param name="component">The relevant component</param>
        public RemovedComponentEventArgs(IComponent component) : base(component) { }
    }

    /// <summary>
    /// Arguments for an event related to a component being deleted.
    /// </summary>
    public sealed class DeletedComponentEventArgs : ComponentEventArgs
    {
        /// <summary>
        /// Constructs a new instance of <see cref="DeletedComponentEventArgs"/>.
        /// </summary>
        /// <param name="component">The relevant component</param>
        public DeletedComponentEventArgs(IComponent component) : base(component) { }
    }

}
