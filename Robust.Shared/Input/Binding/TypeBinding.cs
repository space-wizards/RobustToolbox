using System;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Input.Binding
{
    /// <summary>
    /// An individual binding of a given handler to a given key function for a particular type, with associated
    /// dependency information to resolve handlers bound to the same key function from different types.
    /// </summary>
    public class TypeBinding
    {
        private readonly BoundKeyFunction _boundKeyFunction;
        private readonly IEnumerable<Type> _after;
        private readonly IEnumerable<Type> _before;
        private readonly InputCmdHandler _handler;
        private readonly Type _type;

        /// <summary>
        /// Type which defines this binding.
        /// </summary>
        public Type ForType => _type;

        /// <summary>
        /// Key function the handler should be triggered on
        /// </summary>
        public BoundKeyFunction BoundKeyFunction => _boundKeyFunction;

        /// <summary>
        /// If other types register bindings for this key function, this handler will always fire
        /// after them if they appear in this list.
        /// </summary>
        public IEnumerable<Type> After => _after;
        /// <summary>
        /// If other types register bindings for this key function, this handler will always fire
        /// before them if they appear in this list.
        /// </summary>
        public IEnumerable<Type> Before => _before;

        /// <summary>
        /// Handler which should handle inputs for the key function
        /// </summary>
        public InputCmdHandler Handler => _handler;

        /// <summary>
        /// A binding of a handler to the indicated key function, with the indicated dependencies.
        /// </summary>
        /// <param name="type">type which is defining this binding</param>
        /// <param name="boundKeyFunction">key function this handler should handle</param>
        /// <param name="handler">handler to handle the input</param>
        /// <param name="before">If other types register bindings for this key function, this handler will always fire
        /// before them if they appear in this list.</param>
        /// <param name="after">If other types register bindings for this key function, this handler will always fire
        /// after them if they appear in this list.</param>
        public TypeBinding(Type type, BoundKeyFunction boundKeyFunction, InputCmdHandler handler, IEnumerable<Type> before = null,
            IEnumerable<Type> after = null)
        {
            _type = type;
            _boundKeyFunction = boundKeyFunction;
            _after = after ?? Enumerable.Empty<Type>();
            _before = before ?? Enumerable.Empty<Type>();
            _handler = handler;
        }
    }
}
