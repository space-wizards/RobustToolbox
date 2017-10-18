using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SS14.Client.Graphics.Utility;
using Color = SS14.Shared.Maths.Color;

namespace SS14.Client.Graphics.Render
{
    public class GraphicsContext
    {
        private readonly RenderWindow _window;

        internal GraphicsContext(RenderWindow window)
        {
            _window = window;
        }

        /// <summary>
        /// Clear color
        /// </summary>
        public Color BackgroundColor { get; set; }

        public void SetVerticalSyncEnabled(bool enabled)
        {
            _window.SetVerticalSyncEnabled(enabled);
        }

        public void SetFramerateLimit(uint limit)
        {
            _window.SetFramerateLimit(limit);
        }

        public void Draw(IDrawable drawable)
        {
            _window.Draw(drawable.SFMLDrawable);
        }

        internal void Draw(Drawable drawable)
        {
            _window.Draw(drawable);
        }

        public void Display()
        {
            _window.Display();
        }

        public void Clear(Color color)
        {
            _window.Clear(color.Convert());
        }
    }
}
