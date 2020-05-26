using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Robust.Shared.Input.Binding
{
    /// <summary>
    /// Represents a set of bindings from BoundKeyFunctions to InputCmdHandlers for a particular
    /// type (typically a particular System or Manager).
    ///
    /// Binding sets have an associated type so that you
    /// can control the order in which handlers are fired when multiple types define bindings for the same
    /// key function. They are resolved by building a DAG based on the after / before types for each registered binding
    /// so that the indicated order of resolution is satisfied.
    ///
    /// Immutable. Use Bindings.Builder() to create.
    /// </summary>
    public class TypeBindings
    {
        private readonly List<TypeBinding> _bindings;
        private readonly Type _type;

        /// <summary>
        /// All bindings for this type.
        /// </summary>
        public IEnumerable<TypeBinding> Bindings => _bindings;

        private TypeBindings(List<TypeBinding> bindings, Type type)
        {
            _bindings = bindings;
            _type = type;
        }

        /// <summary>
        /// Builder to build a new set of Bindings
        /// </summary>
        /// <param name="type">type that is defining these bindings, should almost always be typeof(this) - the same type
        /// as the calling class</param>
        /// <returns></returns>
        public static BindingsBuilder Builder(Type type)
        {
            return new BindingsBuilder(type);
        }

        /// <summary>
        /// Builder to build a new set of Bindings
        /// </summary>
        /// <typeparam name="T">type that is defining these bindings, should almost always be typeof(this) - the
        /// same type as the calling class</typeparam>
        /// <returns></returns>
        public static BindingsBuilder Builder<T>()
        {
            return new BindingsBuilder(typeof(T));
        }

        /// <summary>
        /// For creating Bindings.
        /// </summary>
        public class BindingsBuilder
        {
            private readonly List<TypeBinding> _bindings = new List<TypeBinding>();
            private readonly Type _type;

            /// <summary>
            /// New builder for creating a new set of bindings defined by the indicated type.
            /// </summary>
            /// <param name="type">type which is defining these bindings, should almost always be typeof(this)</param>
            public BindingsBuilder(Type type)
            {
                _type = type;
            }

            /// <summary>
            /// Bind the indicated handler to the indicated function, with no
            /// particular dependency on bindings from other types. If multiple
            /// handlers in this builder are registered to the same key function,
            /// the handlers will fire in the order in which they were added to this builder.
            /// </summary>
            public BindingsBuilder Bind(BoundKeyFunction function, InputCmdHandler command)
            {
                return Bind(new TypeBinding(_type, function, command));
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
                    Bind(new TypeBinding(_type, function, command));
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
                return Bind(new TypeBinding(_type, function, command, after: after));
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
                return Bind(new TypeBinding(_type, function, command, before));
            }

            /// <summary>
            /// Add the binding to this set of bindings. If other bindings in this set
            /// are bound to the same key function, they will be resolved in the order they were added
            /// to this builder.
            /// </summary>
            public BindingsBuilder Bind(TypeBinding typeBinding)
            {
                _bindings.Add(typeBinding);
                return this;
            }

            /// <summary>
            /// Create the Bindings based on the current configuration.
            /// </summary>
            public TypeBindings Build()
            {
                return new TypeBindings(_bindings, _type);
            }


            /// <summary>
            /// Create the Bindings based on the current configuration and register
            /// with the indicated mappings so they will be allowed to handle inputs.
            /// </summary>
            /// <param name="registry">mappings to register these bindings with</param>
            public TypeBindings Register(ICommandBindRegistry registry)
            {
                var bindings = Build();
                registry.Register(bindings);
                return bindings;
            }
        }
    }
}
