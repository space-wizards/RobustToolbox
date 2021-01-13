using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

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
        public static BindingsBuilder Builder => new();

        /// <summary>
        /// Unregisters from the current InputSystem's BindRegistry all bindings currently registered under
        /// indicated owner type so they will no longer receive / handle inputs. No effect if input system
        /// no longer exists.
        /// </summary>
        /// <typeparam name="TOwner">owner type whose bindings should be unregistered, typically a system / manager,
        /// should usually be typeof(this) - same type as the calling class.</typeparam>
        public static void Unregister<TOwner>()
        {
            if (EntitySystem.TryGet<SharedInputSystem>(out var inputSystem))
            {
                Unregister<TOwner>(inputSystem.BindRegistry);
            }
        }

        /// <summary>
        /// Unregisters from the given BindRegistry all bindings currently registered under
        /// indicated owner type so they will no longer receive / handle inputs. No effect if input system
        /// no longer exists.
        /// </summary>
        /// <typeparam name="TOwner">owner type whose bindings should be unregistered, typically a system / manager,
        /// should usually be typeof(this) - same type as the calling class.</typeparam>
        public static void Unregister<TOwner>(ICommandBindRegistry bindRegistry)
        {
            bindRegistry.Unregister<TOwner>();
        }

        /// <summary>
        /// For creating Bindings.
        /// </summary>
        public class BindingsBuilder
        {
            private readonly List<CommandBind> _bindings = new();

            public static BindingsBuilder Create()
            {
                return new();
            }

            /// <summary>
            /// Bind the indicated handler to the indicated function, with no
            /// particular dependency on bindings from other owner types. If multiple
            /// handlers in this builder are registered to the same key function,
            /// the handlers will fire in the order in which they were added to this builder.
            /// </summary>
            public BindingsBuilder Bind(BoundKeyFunction function, InputCmdHandler command)
            {
                return Bind(new CommandBind(function, command));
            }

            /// <summary>
            /// Bind the indicated handlers to the indicated function, with no
            /// particular dependency on bindings from other owner types.
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
            /// Bind the indicated handler to the indicated function. If other owner types register bindings for this key
            /// function, this handler will always fire after them if they appear in the "after" list.
            ///
            /// If multiple handlers in this builder are registered to the same key function,
            /// the handlers will fire in the order in which they were added to this builder.
            /// </summary>
            /// <param name="after">If other owner types register bindings for this key
            /// function, this handler will always fire after them if they appear in this list</param>
            public BindingsBuilder BindAfter(BoundKeyFunction function, InputCmdHandler command, params Type[] after)
            {
                return Bind(new CommandBind(function, command, after: after));
            }

            /// <summary>
            /// Bind the indicated handler to the indicated function. If other owner types register bindings for this key
            /// function, this handler will always fire before them if they appear in the "before" list.
            ///
            /// If multiple handlers in this builder are registered to the same key function,
            /// the handlers will fire in the order in which they were added to this builder.
            /// </summary>
            /// <param name="before">If other owner types register bindings for this key
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
                return new(_bindings);
            }


            /// <summary>
            /// Create the Bindings based on the current configuration and register
            /// with the indicated mappings so they will be allowed to handle inputs.
            /// </summary>
            /// <typeparam name="TOwner">type that owns these bindings, typically a system / manager,
            /// should usually be typeof(this) - same type as the calling class.</typeparam>
            /// <param name="registry">mappings to register these bindings with</param>
            public CommandBinds Register<TOwner>(ICommandBindRegistry registry)
            {
                var bindings = Build();
                registry.Register<TOwner>(bindings);
                return bindings;
            }

            /// <summary>
            /// Create the Bindings based on the current configuration and register
            /// with the indicated mappings to the active InputSystem's BindRegistry
            /// so they will be allowed to handle inputs.
            /// </summary>
            /// <typeparam name="TOwner">type that owns these bindings, typically a system / manager,
            /// should usually be typeof(this) - same type as the calling class.</typeparam>
            public CommandBinds Register<TOwner>()
            {
                return Register<TOwner>(EntitySystem.Get<SharedInputSystem>().BindRegistry);
            }
        }
    }
}
