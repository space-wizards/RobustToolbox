using SFML.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Settings;
using SS14.Client.Graphics.Utility;
using SS14.Client.Graphics.Views;
using SS14.Shared.Maths;
using System;
using Color = SS14.Shared.Maths.Color;
using View = SS14.Client.Graphics.Views.View;

namespace SS14.Client.Graphics.Render
{
    public class CluwneWindow : IRenderTarget, IDisposable
    {
        public readonly RenderWindow SFMLWindow;
        public RenderTarget SFMLTarget => SFMLWindow;
        private readonly VideoSettings _settings;
        public Viewport Viewport { get; }
        public GraphicsContext Graphics { get; }
        public View View
        {
            // SFML makes a new view on fetch so this isn't managed by anything else.
            get => new View(SFMLTarget.GetView());
            set => SFMLTarget.SetView(value.SFMLView);
        }

        public event EventHandler<EventArgs> Closed;
        public event EventHandler<SizeEventArgs> Resized;

        public Vector2u Size => SFMLWindow.Size.Convert();
        public uint Width => Size.X;
        public uint Height => Size.Y;

        internal CluwneWindow(RenderWindow window, VideoSettings settings)
        {
            SFMLWindow = window;
            _settings = settings;
            Graphics = new GraphicsContext(SFMLWindow);
            View = new View(SFMLTarget.GetView());
            Viewport = new Viewport(0, 0, SFMLWindow.Size.X, SFMLWindow.Size.Y);

            CluwneLib.Input = new InputEvents(this);

            SFMLWindow.Closed += (sender, args) => Closed?.Invoke(sender, args);
            SFMLWindow.Resized += (sender, args) =>
            {
                Viewport.Width = SFMLWindow.Size.X;
                Viewport.Height = SFMLWindow.Size.Y;
                Resized?.Invoke(sender, new SizeEventArgs(args));
            };
        }

        public void Dispose() => SFMLWindow.Dispose();

        public void SetMouseCursorVisible(bool visible)
        {
            SFMLWindow.SetMouseCursorVisible(visible);
        }

        public void DispatchEvents()
        {
            SFMLWindow.DispatchEvents();
        }

        // close the window
        public void Close()
        {
            // prevents null issues
            CluwneLib.Input = new InputEvents(null);

            SFMLWindow.Close();
        }

        /// <summary>
        /// Gets the position of the mouse relative to this window.
        /// </summary>
        public Vector2i MousePosition => SFML.Window.Mouse.GetPosition(SFMLWindow).Convert();

        public void SetMousePosition(Vector2i newPosition)
        {
            SFML.Window.Mouse.SetPosition(newPosition.Convert());
        }

        public void Clear(Color color)
        {
            SFMLWindow.Clear(color.Convert());
        }

        public void Draw(IDrawable drawable)
        {
            DrawSFML(drawable.SFMLDrawable);
        }

        public void DrawSFML(Drawable drawable)
        {
            SFMLWindow.Draw(drawable);
        }
    }
}
