using System;
using System.Collections.Generic;

namespace Robust.Shared.Input.Binding
{
    /// <summary>
    /// Allows registering bindings so that they will receive and handle inputs.
    /// </summary>
    public interface ICommandBindRegistry
    {
        /// <summary>
        /// Registers the indicated bindings so they can receive and handle inputs.
        /// </summary>
        /// <param name="typeBindings">Bindings to register.</param>
        void Register(TypeBindings typeBindings);

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
