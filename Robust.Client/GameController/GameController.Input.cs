using Robust.Client.Input;

namespace Robust.Client
{
    internal sealed partial class GameController
    {
        /// <summary>
        ///     Invoked when a key on the keyboard or a mouse button is pressed down.
        /// </summary>
        public void KeyDown(KeyEventArgs keyEvent)
        {
            _inputManager.KeyDown(keyEvent);
        }

        /// <summary>
        ///     Invoked when a key on the keyboard or a mouse button is released.
        /// </summary>
        public void KeyUp(KeyEventArgs keyEvent)
        {
            _inputManager.KeyUp(keyEvent);
        }

        public void TextEntered(TextEventArgs textEvent)
        {
            _userInterfaceManager.TextEntered(textEvent);
        }

        /// <summary>
        ///     Invoked when the mouse is moved inside the game window.
        /// </summary>
        public void MouseMove(MouseMoveEventArgs mouseMoveEventArgs)
        {
            _userInterfaceManager.MouseMove(mouseMoveEventArgs);
        }

        /// <summary>
        ///     Invoked when the mouse wheel is moved.
        /// </summary>
        public void MouseWheel(MouseWheelEventArgs mouseWheelEventArgs)
        {
            _userInterfaceManager.MouseWheel(mouseWheelEventArgs);
            if (mouseWheelEventArgs.Handled)
            {
                return;
            }
        }
    }
}
