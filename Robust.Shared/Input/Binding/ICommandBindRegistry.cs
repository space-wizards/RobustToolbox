using System;
using System.Collections.Generic;

namespace Robust.Shared.Input.Binding
{
    /// <summary>
    /// Allows registering bindings so that they will receive and handle inputs. Each set of bindings
    /// is registered to a particular owner Type, which is typically a system or a manager.
    ///
    /// This association of bindings with owner types allows allows the bindings to declare
    /// dependencies on each other - for example to ensure that one system's handlers will always
    /// fire after another system's handlers. This also allows easy unregistering of all bindings
    /// for a given system / manager.
    /// </summary>
    public interface ICommandBindRegistry
    {
        /// <summary>CO
        /// Registers the indicated bindings, under the given owner type.
        /// The handlers in the bindings will receive input events.
        /// </summary>
        /// <param name="commandBinds">Bindings to register.</param>
        /// <typeparam name="TOwner">type that owns these bindings, typically a system / manager,
        /// should usually be typeof(this) - same type as the calling class.</typeparam>
        void Register<TOwner>(CommandBinds commandBinds);

        /// <summary>
        /// Registers the indicated bindings, under the given type.
        /// The handlers in the bindings will receive input events.
        /// </summary>
        /// <param name="commandBinds">Bindings to register.</param>
        /// <param name="owner">type that owns these bindings, typically a system / manager,
        /// should usually be typeof(this) - same type as the calling class.</param>
        void Register(CommandBinds commandBinds, Type owner);

        /// <summary>
        /// Gets the command handlers bound to the indicated function, in the order
        /// in which they should be fired based on the dependency graph. Empty enumerable
        /// if no handlers are bound.
        /// </summary>
        /// <param name="function">Key function to get the input handlers of.</param>
        IEnumerable<InputCmdHandler> GetHandlers(BoundKeyFunction function);

        /// <summary>
        /// Unregisters all bindings currently registered under indicated type so they will
        /// no longer receive / handle inputs.
        /// </summary>
        /// <param name="owner">owner type whose bindings should be unregistered, typically a system / manager,
        /// should usually be typeof(this) - same type as the calling class.</param>
        void Unregister(Type owner);

        /// <summary>
        /// Unregisters all bindings currently registered under indicated type so they will
        /// no longer receive / handle inputs.
        /// </summary>
        /// <typeparam name="TOwner">owner type whose bindings should be unregistered, typically a system / manager,
        /// should usually be typeof(this) - same type as the calling class.</typeparam>
        void Unregister<TOwner>();
    }
}
