using System;
using System.Collections.Generic;

namespace Robust.Shared.Input.Binding
{
    /// <summary>
    /// Allows registering bindings so that they will receive and handle inputs. Each set of bindings
    /// is registered to a particular Type, which is typically a system or a manager.
    ///
    /// This association of bindings with types allows allows the bindings to declare
    /// dependencies on each other - for example to ensure that one system's handlers will always
    /// fire after another system's handlers. This also allows easy unregistering of all bindings
    /// for a given system / manager.
    /// </summary>
    public interface ICommandBindRegistry
    {
        /// <summary>
        /// Registers the indicated bindings, under the given type.
        /// The handlers in the bindings will receive input events.
        /// </summary>
        /// <param name="commandBinds">Bindings to register.</param>
        /// <typeparam name="T">type to register the bindings under, typically a system / manager,
        /// should usually be typeof(this) - same type as the calling class.</typeparam>
        void Register<T>(CommandBinds commandBinds);

        /// <summary>
        /// Registers the indicated bindings, under the given type.
        /// The handlers in the bindings will receive input events.
        /// </summary>
        /// <param name="commandBinds">Bindings to register.</param>
        /// <param name="forType">type to register the bindings under, typically a system / manager,
        /// should usually be typeof(this) - same type as the calling class.</param>
        void Register(CommandBinds commandBinds, Type forType);

        /// <summary>
        /// Gets the command handlers bound to the indicated function, in the order
        /// in which they should be fired based on the dependency graph. Empty enumerable
        /// if no handlers are bound.
        /// </summary>
        /// <param name="function">Key function to get the input handlers of.</param>
        IEnumerable<InputCmdHandler> GetHandlers(BoundKeyFunction function);

        /// <summary>
        /// Unregisters all bindings currently defined by the indicated type so they will
        /// no longer receive / handle inputs. Should usually be typeof(this)  - same type
        /// as the calling class.
        /// </summary>
        void Unregister(Type forType);

        /// <summary>
        /// Unregisters all bindings currently defined by the indicated type so they will
        /// no longer receive / handle inputs. Should usually be typeof(this) - same type
        /// as the calling class.
        /// </summary>
        void Unregister<T>();
    }
}
