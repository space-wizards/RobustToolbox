using SS14.Client.Graphics.Render;
using System;

namespace SS14.Client.Graphics.Input
{
    public class InputEvents
    {
        public InputEvents(CluwneWindow window)
        {
            // if dummy don't attach events
            if (window == null)
                return;

            SFML.Graphics.RenderWindow SWindow = window.SFMLWindow;

            SWindow.KeyPressed += (sender, args) => KeyPressed?.Invoke(sender, (KeyEventArgs)args);
            SWindow.KeyReleased += (sender, args) => KeyReleased?.Invoke(sender, (KeyEventArgs)args);
            SWindow.MouseButtonPressed += (sender, args) => MouseButtonPressed?.Invoke(sender, (MouseButtonEventArgs)args);
            SWindow.MouseButtonReleased += (sender, args) => MouseButtonReleased?.Invoke(sender, (MouseButtonEventArgs)args);
            SWindow.MouseMoved += (sender, args) => MouseMoved?.Invoke(sender, (MouseMoveEventArgs)args);
            SWindow.MouseWheelScrolled += (sender, args) => MouseWheelMoved?.Invoke(sender, (MouseWheelScrollEventArgs)args);
            SWindow.MouseEntered += (sender, args) => MouseEntered?.Invoke(sender, args);
            SWindow.MouseLeft += (sender, args) => MouseLeft?.Invoke(sender, args);
            SWindow.TextEntered += (sender, args) => TextEntered?.Invoke(sender, (TextEventArgs)args);
        }

        public event EventHandler<KeyEventArgs> KeyPressed;
        public event EventHandler<KeyEventArgs> KeyReleased;
        public event EventHandler<MouseButtonEventArgs> MouseButtonPressed;
        public event EventHandler<MouseButtonEventArgs> MouseButtonReleased;
        public event EventHandler<MouseMoveEventArgs> MouseMoved;
        public event EventHandler<MouseWheelScrollEventArgs> MouseWheelMoved;
        public event EventHandler<EventArgs> MouseEntered;
        public event EventHandler<EventArgs> MouseLeft;
        public event EventHandler<TextEventArgs> TextEntered;
    }
}
