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
        public Component Component { get; }

        /// <summary>
        /// EntityUid of the entity this component belongs to.
        /// </summary>
        public EntityUid Owner { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="ComponentEventArgs"/>.
        /// </summary>
        /// <param name="component">The relevant component</param>
        /// <param name="owner">EntityUid of the entity this component belongs to.</param>
        public ComponentEventArgs(Component component, EntityUid owner)
        {
            Component = component;
            Owner = owner;
        }
    }

    public readonly struct AddedComponentEventArgs
    {
        public readonly ComponentEventArgs BaseArgs;
        public readonly CompIdx ComponentType;

        public AddedComponentEventArgs(ComponentEventArgs baseArgs, CompIdx componentType)
        {
            BaseArgs = baseArgs;
            ComponentType = componentType;
        }
    }

    public readonly struct RemovedComponentEventArgs
    {
        public readonly ComponentEventArgs BaseArgs;

        public readonly bool Terminating;

        public RemovedComponentEventArgs(ComponentEventArgs baseArgs, bool terminating)
        {
            BaseArgs = baseArgs;
            Terminating = terminating;
        }
    }

    public readonly struct DeletedComponentEventArgs
    {
        public readonly ComponentEventArgs BaseArgs;

        public readonly bool Terminating;

        public DeletedComponentEventArgs(ComponentEventArgs baseArgs, bool terminating)
        {
            BaseArgs = baseArgs;
            Terminating = terminating;
        }
    }
}
