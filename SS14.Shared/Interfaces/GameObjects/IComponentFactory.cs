using System;
using System.Collections.Generic;

namespace SS14.Shared.Interfaces.GameObjects
{
    /// <summary>
    /// Used by <see cref="Shared.GameObjects.EntityPrototype" /> to determine whether a component is available.
    /// This distinction is important because prototypes are shared across client and server, but the two might have different components.
    /// </summary>
    /// <seealso cref="IComponentFactory" />
    public enum ComponentAvailability
    {
        /// <summary>
        /// The component is available and can be insantiated.
        /// </summary>
        Available,

        /// <summary>
        /// The component is not available, but should be ignored (prevent warnings for missing components).
        /// </summary>
        Ignore,

        /// <summary>
        /// The component is unknown entirely. This may warrant a warning or error.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Handles the registration and spawning of components.
    /// </summary>
    /// <remarks>
    /// <p>
    /// When referring to component names, this is the name the component has been registered as,
    /// and what's used in prototypes. However, most commonly the type is referred through by an interface.
    /// </p>
    /// <p>
    /// Before a component can be spawned, it must be registered so things such as name, networking ID, type, etc...
    /// are known to the factory.
    /// Components are registered into a registry.
    /// The relevant methods for writing to this registry are <see cref="Register" /> and <see cref="RegisterReference" />.
    /// The data is exposed for reading through <see cref="GetRegistration" /> and its overloads.
    /// This data is returned in the form of a <see cref="IComponentRegistration" />, which represents one component's registration.
    /// </p>
    /// </remarks>
    /// <seealso cref="IComponentRegistration" />
    /// <seealso cref="IComponent" />
    public interface IComponentFactory
    {
        /// <summary>
        ///     All IComponent types that are currently registered to this factory.
        /// </summary>
        IEnumerable<Type> AllRegisteredTypes { get; }

            /// <summary>
        /// Get whether a component is available right now.
        /// </summary>
        /// <param name="componentName">The name of the component to check.</param>
        /// <returns>The availability of the component.</returns>
        ComponentAvailability GetComponentAvailability(string componentName);

        /// <summary>
        /// Registers a prototype to be available for spawning.
        /// </summary>
        /// <remarks>
        /// This implicitly calls <see cref="RegisterReference{TTarget, TInterface}"/>
        /// with a <c>TTarget</c> and <c>TInterface</c> of <typeparamref name="T"/>.
        /// </remarks>
        void Register<T>(bool overwrite = false) where T : IComponent, new();

        /// <summary>
        /// Registers a component name as being ignored.
        /// </summary>
        /// <param name="name">The name to be ignored.</param>
        /// <param name="overwrite">Whether to overrde existing settings instead of throwing an exception in the case of duplicates.</param>
        void RegisterIgnore(string name, bool overwrite = false);

        // NOTE: no overwrite here, it'd overcomplicate RegisterReference a LOT.
        // If you need to overwrite references for some sick reason overwrite the component too.
        /// <summary>
        /// Registers <typeparamref name="TTarget" /> to be referenced when
        /// <typeparamref name="TInterface"/> is used in methods like <see cref="IEntity.GetComponent{T}"/>
        /// </summary>
        void RegisterReference<TTarget, TInterface>() where TTarget : TInterface, IComponent, new();

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <param name="componentType">type of component to make</param>
        /// <returns>A Component</returns>
        IComponent GetComponent(Type componentType);

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <returns>A Component</returns>
        T GetComponent<T>() where T : IComponent, new();

        /// <summary>
        /// Gets a new component instantiated of the specified <see cref="IComponent.Name"/>.
        /// </summary>
        /// <param name="componentName">name of component to make</param>
        /// <returns>A Component</returns>
        IComponent GetComponent(string componentName);

        /// <summary>
        /// Gets the registration belonging to a component.
        /// </summary>
        /// <param name="componentName">The name of the component.</param>
        IComponentRegistration GetRegistration(string componentName);

        /// <summary>
        /// Gets the registration belonging to a component.
        /// </summary>
        /// <param name="reference">A reference corresponding to the component to look up.</param>
        IComponentRegistration GetRegistration(Type reference);

        /// <summary>
        /// Gets the registration belonging to a component.
        /// </summary>
        /// <typeparam name="T">A type referencing the component.</typeparam>
        IComponentRegistration GetRegistration<T>() where T : IComponent, new();

        /// <summary>
        /// Gets the registration belonging to a component.
        /// </summary>
        /// <param name="netID">The network ID corresponding to the component.</param>
        /// <returns></returns>
        IComponentRegistration GetRegistration(uint netID);

        /// <summary>
        /// Get the registration of a component.
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        IComponentRegistration GetRegistration(IComponent component);
    }

    /// <summary>
    /// Represents a component registered into a <see cref="IComponentFactory" />.
    /// </summary>
    /// <seealso cref="IComponentFactory" />
    /// <seealso cref="IComponent" />
    public interface IComponentRegistration
    {
        /// <summary>
        /// The name of the component.
        /// This is used as the <c>type</c> field in the component declarations if entity prototypes.
        /// </summary>
        /// <seealso cref="IComponent.Name" />
        string Name { get; }

        /// <summary>
        /// ID used to reference the component type across the network.
        /// If null, no network synchronization will be available for this component.
        /// </summary>
        /// <seealso cref="IComponent.NetID" />
        uint? NetID { get; }

        /// <summary>
        /// True if the addition and removal of the component will be synchronized to clients.
        /// This means that if the server adds or removes the component outside of prototype-based creation,
        /// the client will update accordingly.
        /// If false the client will ignore missing components even when the net ID checks out and could be instantiated.
        /// and the client won't delete the component if no state was sent for it.
        /// </summary>
        /// <seealso cref="IComponent.NetworkSynchronizeExistence" />
        bool NetworkSynchronizeExistence { get; }

        /// <summary>
        /// The type that will be instantiated if this component is created.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// A list of type references that can be used to get a reference to an instance of this component,
        /// for methods like <see cref="IEntity.GetComponent{T}" />.
        /// These are not unique and can overlap with other components.
        /// Unlike the other properties, this data is not gotten from a component instance,
        /// instead this data is set with <see cref="IComponentFactory.RegisterReference{TTarget, TInterface}" />
        /// </summary>
        IReadOnlyList<Type> References { get; }
    }
}
