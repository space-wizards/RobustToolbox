using System.Collections.Generic;

namespace SS14.Shared.Input
{
    /// <summary>
    ///     Contains a mapping of <see cref="BoundKeyFunction"/> to <see cref="InputCmdHandler"/>.
    /// </summary>
    public interface ICommandBindMapping
    {
        /// <summary>
        ///     Binds an input command handler to a key function.
        /// </summary>
        /// <param name="function">Key function being bound.</param>
        /// <param name="command">Input command handler to bind.</param>
        void BindFunction(BoundKeyFunction function, InputCmdHandler command);

        /// <summary>
        ///     Tries to get the command handler of a key function.
        /// </summary>
        /// <param name="function">Key function to get the input handler of.</param>
        /// <param name="handler">command handler that was bound to the key function (if any).</param>
        /// <returns>True if the key function had a handler to return.</returns>
        bool TryGetHandler(BoundKeyFunction function, out InputCmdHandler handler);

        /// <summary>
        ///     Unbinds the command handler from a key function.
        /// </summary>
        /// <param name="function">Key function being unbound.</param>
        void UnbindFunction(BoundKeyFunction function);
    }

    /// <inheritdoc />
    public class CommandBindMapping : ICommandBindMapping
    {
        private readonly Dictionary<BoundKeyFunction, InputCmdHandler> _commandBinds = new Dictionary<BoundKeyFunction, InputCmdHandler>();

        /// <inheritdoc />
        public void BindFunction(BoundKeyFunction function, InputCmdHandler command)
        {
            _commandBinds.Add(function, command);
        }

        /// <inheritdoc />
        public bool TryGetHandler(BoundKeyFunction function, out InputCmdHandler handler)
        {
            return _commandBinds.TryGetValue(function, out handler);
        }

        /// <inheritdoc />
        public void UnbindFunction(BoundKeyFunction function)
        {
            _commandBinds.Remove(function);
        }
    }
}
