using SS14.Client.Input;

namespace SS14.Client
{
    public sealed partial class GameController
    {
        /// <summary>
        ///     Invoked when a key on the keyboard is pressed down.
        /// </summary>
        private void KeyDown(KeyEventArgs keyEvent)
        {
            _inputManager.KeyDown(keyEvent);
        }

        /// <summary>
        ///     Invoked when a key on the keyboard is released.
        /// </summary>
        private void KeyUp(KeyEventArgs keyEvent)
        {
            _inputManager.KeyUp(keyEvent);
        }

        /// <summary>
        ///     Invoked when a button on the mouse is pressed down.
        /// </summary>
        private void MouseDown(MouseButtonEventArgs mouseEvent)
        {
            _userInterfaceManager.UnhandledMouseDown(mouseEvent);
            _stateManager.MouseDown(mouseEvent);
        }

        /// <summary>
        ///     Invoked when a button on the mouse is released.
        /// </summary>
        private void MouseUp(MouseButtonEventArgs mouseButtonEventArgs)
        {
            _userInterfaceManager.UnhandledMouseUp(mouseButtonEventArgs);
            _stateManager.MouseUp(mouseButtonEventArgs);
        }

        /// <summary>
        ///     Invoked when the mouse is moved inside the game window.
        /// </summary>
        private void MouseMove(MouseMoveEventArgs mouseMoveEventArgs)
        {
            _stateManager.MouseMove(mouseMoveEventArgs);
        }

        /// <summary>
        ///     Invoked when the mouse wheel is moved.
        /// </summary>
        private void MouseWheel(MouseWheelEventArgs mouseWheelEventArgs)
        {
            _stateManager.MouseWheelMove(mouseWheelEventArgs);
        }
    }
}
