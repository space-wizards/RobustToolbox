using System;
using SS14.Shared.Map;

namespace SS14.Shared.Input
{
    /// <summary>
    ///     Event data values for a bound key state change.
    /// </summary>
    public class BoundKeyEventArgs : EventArgs
    {
        /// <summary>
        ///     Bound key that that is changing.
        /// </summary>
        public BoundKeyFunction Function { get; }

        /// <summary>
        ///     New state of the <see cref="BoundKeyFunction"/>.
        /// </summary>
        public BoundKeyState State { get; }

        /// <summary>
        ///     Current Pointer location in screen coordinates.
        /// </summary>
        public ScreenCoordinates PointerLocation { get; }

        /// <summary>
        ///     Constructs a new instance of <see cref="BoundKeyEventArgs"/>.
        /// </summary>
        /// <param name="function">Bound key that that is changing.</param>
        /// <param name="state">New state of the function.</param>
        /// <param name="pointerLocation">Current Pointer location in screen coordinates.</param>
        public BoundKeyEventArgs(BoundKeyFunction function, BoundKeyState state, ScreenCoordinates pointerLocation)
        {
            Function = function;
            State = state;
            PointerLocation = pointerLocation;
        }
    }
}
