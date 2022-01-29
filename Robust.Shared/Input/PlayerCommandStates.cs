using System.Collections.Generic;

namespace Robust.Shared.Input
{
    /// <summary>
    ///     Contains a mapping of <see cref="BoundKeyFunction"/> to their current <see cref="BoundKeyState"/>.
    /// </summary>
    public interface IPlayerCommandStates
    {
        /// <summary>
        ///     Indexer access to the Get/Set functions.
        /// </summary>
        BoundKeyState this[BoundKeyFunction function] { get; set; }

        /// <summary>
        ///     Gets the current state of a function.
        /// </summary>
        /// <param name="function">Function to get the state of.</param>
        /// <returns>Current state of the function.</returns>
        BoundKeyState GetState(BoundKeyFunction function);

        /// <summary>
        ///     Sets the current state of a function.
        /// </summary>
        /// <param name="function">Function to change.</param>
        /// <param name="state">State value to set.</param>
        void SetState(BoundKeyFunction function, BoundKeyState state);
    }

    /// <inheritdoc />
    public sealed class PlayerCommandStates : IPlayerCommandStates
    {
        private readonly Dictionary<BoundKeyFunction, BoundKeyState> _functionStates = new();

        /// <inheritdoc />
        public BoundKeyState this[BoundKeyFunction function]
        {
            get => GetState(function);
            set => SetState(function, value);
        }

        /// <inheritdoc />
        public BoundKeyState GetState(BoundKeyFunction function)
        {
            return _functionStates.TryGetValue(function, out var state) ? state : BoundKeyState.Up;
        }

        /// <inheritdoc />
        public void SetState(BoundKeyFunction function, BoundKeyState state)
        {
            _functionStates[function] = state;
        }
    }
}
