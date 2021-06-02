using System.Collections;
using System.Collections.Generic;

namespace Robust.Shared.Input
{
    /// <summary>
    ///     An Input Context to determine which key binds are currently available to the player.
    /// </summary>
    public interface IInputCmdContext : IEnumerable<BoundKeyFunction>
    {
        /// <summary>
        ///     Adds a key function to the set of available functions.
        /// </summary>
        /// <param name="function"></param>
        void AddFunction(BoundKeyFunction function);

        /// <summary>
        ///     Checks if a key function is available in THIS context (DOES NOT CHECK PARENTS).
        /// </summary>
        /// <param name="function">Function to look for.</param>
        /// <returns>If the function is available.</returns>
        bool FunctionExists(BoundKeyFunction function);

        /// <summary>
        ///     Checks if a key function is available in this and ALL parent contexts.
        /// </summary>
        /// <param name="function">Function to look for.</param>
        /// <returns>If the function is available.</returns>
        bool FunctionExistsHierarchy(BoundKeyFunction function);

        /// <summary>
        ///     Removes a function from THIS context.
        /// </summary>
        /// <param name="function">Function to remove.</param>
        void RemoveFunction(BoundKeyFunction function);

        string Name { get; }
    }

    /// <inheritdoc />
    internal class InputCmdContext : IInputCmdContext
    {
        private readonly List<BoundKeyFunction> _commands = new();
        private readonly IInputCmdContext? _parent;
        public string Name { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="InputCmdContext"/>.
        /// </summary>
        /// <param name="parent">Parent context.</param>
        internal InputCmdContext(IInputCmdContext? parent, string name)
        {
            _parent = parent;
            Name = name;
        }

        /// <summary>
        ///     Creates a instance of <see cref="InputCmdContext"/> with no parent.
        /// </summary>
        internal InputCmdContext(string name)
        {
            Name = name;
        }

        /// <inheritdoc />
        public void AddFunction(BoundKeyFunction function)
        {
            _commands.Add(function);
        }

        /// <inheritdoc />
        public bool FunctionExists(BoundKeyFunction function)
        {
            return _commands.Contains(function);
        }

        /// <inheritdoc />
        public bool FunctionExistsHierarchy(BoundKeyFunction function)
        {
            if (_commands.Contains(function))
                return true;

            if (_parent != null)
                return _parent.FunctionExistsHierarchy(function);

            return false;
        }

        /// <inheritdoc />
        public void RemoveFunction(BoundKeyFunction function)
        {
            _commands.Remove(function);
        }

        /// <inheritdoc />
        public IEnumerator<BoundKeyFunction> GetEnumerator()
        {
            return _commands.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
