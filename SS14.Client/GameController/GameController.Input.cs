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
            if (!OnGodot)
            {
                _userInterfaceManager.KeyDown(keyEvent);

                if (keyEvent.Handled)
                {
                    return;
                }
            }
            _inputManager.KeyDown(keyEvent);
        }

        /// <summary>
        ///     Invoked when a key on the keyboard is released.
        /// </summary>
        public void KeyUp(KeyEventArgs keyEvent)
        {
            // Unlike KeyDown, InputManager still gets key ups.
            // My logic is that it should be fine dealing with redundant key ups and this *might* prevent edge cases.
            if (!OnGodot)
            {
                _userInterfaceManager.KeyUp(keyEvent);
            }
            _inputManager.KeyUp(keyEvent);
        }

        public void TextEntered(TextEventArgs textEvent)
        {
            _userInterfaceManager.TextEntered(textEvent);
        }

        /// <summary>
        ///     Invoked when a button on the mouse is pressed down.
        /// </summary>
        public void MouseDown(MouseButtonEventArgs mouseEvent)
        {
            if (GameController.OnGodot)
            {
                _userInterfaceManager.GDUnhandledMouseDown(mouseEvent);
            }
            else
            {
                _userInterfaceManager.MouseDown(mouseEvent);
            }
            _stateManager.MouseDown(mouseEvent);
        }

        /// <summary>
        ///     Invoked when a button on the mouse is released.
        /// </summary>
        public void MouseUp(MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (GameController.OnGodot)
            {
                _userInterfaceManager.GDUnhandledMouseUp(mouseButtonEventArgs);
            }
            else
            {
                _userInterfaceManager.MouseUp(mouseButtonEventArgs);
            }
            _stateManager.MouseUp(mouseButtonEventArgs);
        }

        /// <summary>
        ///     Invoked when the mouse is moved inside the game window.
        /// </summary>
        public void MouseMove(MouseMoveEventArgs mouseMoveEventArgs)
        {
            if (!GameController.OnGodot)
            {
                _userInterfaceManager.MouseMove(mouseMoveEventArgs);
            }
            _stateManager.MouseMove(mouseMoveEventArgs);
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
            _stateManager.MouseWheelMove(mouseWheelEventArgs);
        }
    }
}
