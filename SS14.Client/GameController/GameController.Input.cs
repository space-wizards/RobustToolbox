using SS14.Client.Input;

namespace SS14.Client
{
    internal sealed partial class GameController
    {
        /// <summary>
        ///     Invoked when a key on the keyboard is pressed down.
        /// </summary>
        public void KeyDown(KeyEventArgs keyEvent)
        {
            _inputManager.KeyDown(keyEvent);
        }

        /// <summary>
        ///     Invoked when a key on the keyboard is released.
        /// </summary>
        public void KeyUp(KeyEventArgs keyEvent)
        {
            _inputManager.KeyUp(keyEvent);
        }

        /// <summary>
        ///     Invoked when a button on the mouse is pressed down.
        /// </summary>
        public void MouseDown(MouseButtonEventArgs mouseEvent)
        {
            _userInterfaceManager.GDUnhandledMouseDown(mouseEvent);
            _stateManager.MouseDown(mouseEvent);
        }

        /// <summary>
        ///     Invoked when a button on the mouse is released.
        /// </summary>
        public void MouseUp(MouseButtonEventArgs mouseButtonEventArgs)
        {
            _userInterfaceManager.GDUnhandledMouseUp(mouseButtonEventArgs);
            _stateManager.MouseUp(mouseButtonEventArgs);
        }

        /// <summary>
        ///     Invoked when the mouse is moved inside the game window.
        /// </summary>
        public void MouseMove(MouseMoveEventArgs mouseMoveEventArgs)
        {
            _stateManager.MouseMove(mouseMoveEventArgs);
        }

        /// <summary>
        ///     Invoked when the mouse wheel is moved.
        /// </summary>
        public void MouseWheel(MouseWheelEventArgs mouseWheelEventArgs)
        {
            _stateManager.MouseWheelMove(mouseWheelEventArgs);
        }
    }
}
