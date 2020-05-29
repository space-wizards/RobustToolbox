using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Robust.Shared.Input.Binding
{
    /// <summary>
    /// Represents a set of bindings from BoundKeyFunctions to InputCmdHandlers
    ///
    /// Immutable. Use Bindings.Builder() to create.
    /// </summary>
    public class CommandBinds
    {
        private readonly List<CommandBind> _bindings;

        public IEnumerable<CommandBind> Bindings => _bindings;

        private CommandBinds(List<CommandBind> bindings)
        {
            _bindings = bindings;
        }

        /// <summary>
        /// Builder to build a new set of Bindings
        /// </summary>
        /// <returns></returns>
        public static BindingsBuilder Builder => new BindingsBuilder();

        /// <summary>
        /// For creating Bindings.
        /// </summary>
        public class BindingsBuilder
        {
            private readonly List<CommandBind> _bindings = new List<CommandBind>();

            /// <summary>
            /// Bind the indicated handler to the indicated function, with no
            /// particular dependency on bindings from other types. If multiple
            /// handlers in this builder are registered to the same key function,
            /// the handlers will fire in the order in which they were added to this builder.
            /// </summary>
            public BindingsBuilder Bind(BoundKeyFunction function, InputCmdHandler command)
            {
                return Bind(new CommandBind(function, command));
            }

            /// <summary>
            /// Bind the indicated handlers to the indicated function, with no
            /// particular dependency on bindings from other types.
            ///
            /// If multiple
            /// handlers in this builder are registered to the same key function,
            /// the handlers will fire in the order in which they were added to this builder.
            /// </summary>
            public BindingsBuilder Bind(BoundKeyFunction function, IEnumerable<InputCmdHandler> commands)
            {
                foreach (var command in commands)
                {
                    Bind(new CommandBind(function, command));
                }

                return this;
            }

            /// <summary>
            /// Bind the indicated handler to the indicated function. If other types register bindings for this key
            /// function, this handler will always fire after them if they appear in the "after" list.
            ///
            /// If multiple handlers in this builder are registered to the same key function,
            /// the handlers will fire in the order in which they were added to this builder.
            /// </summary>
            /// <param name="after">If other types register bindings for this key
            /// function, this handler will always fire after them if they appear in this list</param>
            public BindingsBuilder BindAfter(BoundKeyFunction function, InputCmdHandler command, params Type[] after)
            {
                return Bind(new CommandBind(function, command, after: after));
            }

            /// <summary>
            /// Bind the indicated handler to the indicated function. If other types register bindings for this key
            /// function, this handler will always fire before them if they appear in the "before" list.
            ///
            /// If multiple handlers in this builder are registered to the same key function,
            /// the handlers will fire in the order in which they were added to this builder.
            /// </summary>
            /// <param name="before">If other types register bindings for this key
            /// function, this handler will always fire before them if they appear in this list</param>
            public BindingsBuilder BindBefore(BoundKeyFunction function, InputCmdHandler command, params Type[] before)
            {
                return Bind(new CommandBind(function, command, before));
            }

            /// <summary>
            /// Add the binding to this set of bindings. If other bindings in this set
            /// are bound to the same key function, they will be resolved in the order they were added
            /// to this builder.
            /// </summary>
            public BindingsBuilder Bind(CommandBind commandBind)
            {
                _bindings.Add(commandBind);
                return this;
            }

            /// <summary>
            /// Create the Bindings based on the current configuration.
            /// </summary>
            public CommandBinds Build()
            {
                return new CommandBinds(_bindings);
            }


            /// <summary>
            /// Create the Bindings based on the current configuration and register
            /// with the indicated mappings so they will be allowed to handle inputs.
            /// </summary>
            /// <param name="registry">mappings to register these bindings with</param>
            public CommandBinds Register<T>(ICommandBindRegistry registry)
            {
                var bindings = Build();
                registry.Register<T>(bindings);
                return bindings;
            }
        }
    }
}
