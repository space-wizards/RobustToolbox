namespace Robust.Shared.GameObjects
{

    /// <summary>
    /// Arguments for an event related to a component.
    /// </summary>
    public readonly struct ComponentEventArgs
    {
        /// <summary>
        /// Component that this event relates to.
        /// </summary>
        public IComponent Component { get; }

        /// <summary>
        /// EntityUid of the entity this component belongs to.
        /// </summary>
        public EntityUid Owner { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="ComponentEventArgs"/>.
        /// </summary>
        /// <param name="component">The relevant component</param>
        /// <param name="owner">EntityUid of the entity this component belongs to.</param>
        public ComponentEventArgs(IComponent component, EntityUid owner)
        {
            Component = component;
            Owner = owner;
        }
    }

    public readonly struct AddedComponentEventArgs
    {
        public readonly ComponentEventArgs BaseArgs;
        public readonly ComponentRegistration ComponentType;

        internal AddedComponentEventArgs(ComponentEventArgs baseArgs, ComponentRegistration componentType)
        {
            BaseArgs = baseArgs;
            ComponentType = componentType;
        }
    }

    public readonly struct RemovedComponentEventArgs
    {
        public readonly ComponentEventArgs BaseArgs;

        public readonly bool Terminating;

        public readonly MetaDataComponent Meta;

        public readonly CompIdx Idx;

        internal RemovedComponentEventArgs(ComponentEventArgs baseArgs, bool terminating, MetaDataComponent meta, CompIdx idx)
        {
            BaseArgs = baseArgs;
            Terminating = terminating;
            Meta = meta;
            Idx = idx;
        }
    }
}
