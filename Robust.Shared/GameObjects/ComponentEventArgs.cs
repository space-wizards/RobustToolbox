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
        /// EntityUid of the entity this component belongs to.
        /// </summary>
        public EntityUid OwnerUid { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="ComponentEventArgs"/>.
        /// </summary>
        /// <param name="component">The relevant component</param>
        /// <param name="ownerUid">EntityUid of the entity this component belongs to.</param>
        protected ComponentEventArgs(IComponent component, EntityUid ownerUid)
        {
            Component = component;
            OwnerUid = ownerUid;
        }
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
        /// <param name="uid">EntityUid of the entity this component belongs to.</param>
        public AddedComponentEventArgs(IComponent component, EntityUid uid) : base(component, uid) { }
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
        /// <param name="uid">EntityUid of the entity this component belongs to.</param>
        public RemovedComponentEventArgs(IComponent component, EntityUid uid) : base(component, uid) { }
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
        /// <param name="uid">EntityUid of the entity this component belongs to.</param>
        public DeletedComponentEventArgs(IComponent component, EntityUid uid) : base(component, uid) { }
    }
}
