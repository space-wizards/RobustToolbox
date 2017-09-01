using SFML.Graphics;
using System;
using SFML.Window;
using SS14.Client.Graphics.Settings;
using SS14.Client.Graphics.View;

namespace SS14.Client.Graphics.Render
{
    public class CluwneWindow
    {
        private readonly RenderWindow _window;
        private readonly VideoSettings _settings;

        internal CluwneWindow(RenderWindow window, VideoSettings settings)
        {
            _window = window;
            _settings = settings;
            Graphics = new GraphicsContext(_window);
            Camera = new Camera(_window);
            Viewport = new Viewport(0,0,_window.Size.X, _window.Size.Y);
            
            CluwneLib.Input = new InputEvents(_window);

            _window.Closed += (sender, args) => Closed?.Invoke(sender, args);
            _window.Resized += (sender, args) =>
            {
                Viewport.Width = _window.Size.X;
                Viewport.Height = _window.Size.Y;
                Resized?.Invoke(sender, args);
            };
        }

        public Viewport Viewport { get; set; }

        public RenderTarget Screen => _window;

        /// <summary>
        /// Graphics context of the window.
        /// </summary>
        public GraphicsContext Graphics { get; }

        public Camera Camera { get; }

        public void SetMouseCursorVisible(bool visible)
        {
            _window.SetMouseCursorVisible(visible);
        }

        public void DispatchEvents()
        {
            _window.DispatchEvents();
        }

        // close the window
        public void Close()
        {
            // prevents null issues
            CluwneLib.Input = new InputEvents(null);

            _window.Close();
        }

        [Obsolete("Use the new API.")]
        public static implicit operator RenderWindow(CluwneWindow window)
        {
            return window._window;
        }
        

        public event EventHandler<EventArgs> Closed;
        public event EventHandler<SizeEventArgs> Resized;
    }
}
